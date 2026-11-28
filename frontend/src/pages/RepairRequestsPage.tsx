import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { RepairRequest, WarrantyCard, User, RepairRequestStatus, RepairPartLine, Part } from '../types';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import { format } from 'date-fns';
import { Button, Alert, PageHeader, EmptyState, Select, TextArea, Input } from '../components/ui';
import { SortHeader } from '../components/SortHeader';
import { Pagination, parsePaged } from '../components/Pagination';

function mapReq(d: any): RepairRequest {
  return {
    id: d.id ?? d.Id,
    warrantyCardId: d.warrantyCardId ?? d.WarrantyCardId,
    customerId: d.customerId ?? d.CustomerId,
    customerName: d.customerName ?? d.CustomerName ?? '',
    receptionistId: d.receptionistId ?? d.ReceptionistId,
    technicianId: d.technicianId ?? d.TechnicianId,
    technicianName: d.technicianName ?? d.TechnicianName,
    description: d.description ?? d.Description,
    status: d.status ?? d.Status,
    createdAt: d.createdAt ?? d.CreatedAt,
    updatedAt: d.updatedAt ?? d.UpdatedAt,
    statusHistory: (d.statusHistory ?? d.StatusHistory ?? []).map((h: any) => ({
      id: h.id ?? h.Id,
      oldStatus: h.oldStatus ?? h.OldStatus,
      newStatus: h.newStatus ?? h.NewStatus,
      changedBy: h.changedBy ?? h.ChangedBy,
      changedAt: h.changedAt ?? h.ChangedAt,
    })),
  };
}

const statusColor: Record<string, string> = {
  Received: 'badge-warning',
  InProgress: 'badge-info',
  Completed: 'badge-success',
  Returned: 'badge-neutral',
  Cancelled: 'badge-danger',
};

const statuses: RepairRequestStatus[] = ['Received', 'InProgress', 'Completed', 'Returned', 'Cancelled'];

export default function RepairRequestsPage() {
  const { hasRole, user } = useAuthStore();
  const canCreate = hasRole('Admin', 'Manager', 'Receptionist', 'Customer');
  const canAssign = hasRole('Admin', 'Manager', 'Receptionist');
  const canEdit = hasRole('Admin', 'Manager', 'Receptionist');
  const canStatus = hasRole('Technician', 'Admin', 'Manager');
  const [items, setItems] = useState<RepairRequest[]>([]);
  const [cards, setCards] = useState<WarrantyCard[]>([]);
  const [technicians, setTechnicians] = useState<User[]>([]);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ warrantyCardId: 0, description: '' });
  const [assignId, setAssignId] = useState<number | null>(null);
  const [techId, setTechId] = useState(0);
  const [editId, setEditId] = useState<number | null>(null);
  const [editDesc, setEditDesc] = useState('');
  const [partsFor, setPartsFor] = useState<number | null>(null);
  const [partLines, setPartLines] = useState<RepairPartLine[]>([]);
  const [allParts, setAllParts] = useState<Part[]>([]);
  const [partForm, setPartForm] = useState({ partId: 0, quantity: 1 });
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [sortBy, setSortBy] = useState('createdAt');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [keyword, setKeyword] = useState('');
  const [appliedKeyword, setAppliedKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  const load = async (p = page, size = pageSize) => {
    try {
      const [reqRes, cardRes] = await Promise.all([
        api.get('/api/repair-requests', {
          params: {
            pageNumber: p,
            pageSize: size,
            keyword: appliedKeyword || undefined,
            status: statusFilter || undefined,
            sortBy,
            sortDir,
          },
        }),
        api.get('/api/warranty-cards', { params: { pageNumber: 1, pageSize: 100 } }),
      ]);
      const parsed = parsePaged(reqRes.data, mapReq);
      setItems(parsed.items);
      setPage(parsed.page);
      setPageSize(parsed.pageSize);
      setTotalPages(parsed.totalPages);
      setTotalCount(parsed.totalCount);
      setCards(parsePaged(cardRes.data, (d: any) => ({
          id: d.id ?? d.Id,
          productId: d.productId ?? d.ProductId,
          customerId: d.customerId ?? d.CustomerId,
          status: d.status ?? d.Status,
          startDate: d.startDate ?? d.StartDate,
          endDate: d.endDate ?? d.EndDate,
          productName: d.productName ?? d.ProductName,
        })).items);
      if (canAssign) {
        try {
          const techRes = await api.get('/api/users/technicians');
          setTechnicians(parsePaged(techRes.data, (d: any) => ({
              id: d.id ?? d.Id,
              fullName: d.fullName ?? d.FullName,
              email: d.email ?? d.Email,
              role: d.role ?? d.Role,
              isActive: d.isActive ?? d.IsActive,
              createdAt: d.createdAt ?? d.CreatedAt,
            })).items);
        } catch { /* ignore */ }
      }
      setError('');
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  useEffect(() => {
    load(page, pageSize);
  }, [page, pageSize, sortBy, sortDir, appliedKeyword, statusFilter]);

  const toggleSort = (column: string) => {
    setSortDir((direction) => sortBy === column ? (direction === 'asc' ? 'desc' : 'asc') : 'asc');
    setSortBy(column);
    setPage(1);
  };

  const onCreate = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await api.post('/api/repair-requests', form);
      setShowForm(false);
      setForm({ warrantyCardId: 0, description: '' });
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const onAssign = async () => {
    if (!assignId || !techId) return;
    setError('');
    try {
      await api.put(`/api/repair-requests/${assignId}/technician`, { technicianId: techId });
      setAssignId(null);
      setTechId(0);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const onStatus = async (id: number, status: RepairRequestStatus) => {
    setError('');
    try {
      await api.put(`/api/repair-requests/${id}/status`, { status });
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const onSaveEdit = async () => {
    if (!editId) return;
    setError('');
    try {
      await api.put(`/api/repair-requests/${editId}`, { description: editDesc });
      setEditId(null);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const onCancel = async (id: number) => {
    if (!confirm('Hủy yêu cầu sửa chữa này?')) return;
    setError('');
    try {
      await api.delete(`/api/repair-requests/${id}`);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };


  const openParts = async (id: number) => {
    setPartsFor(id);
    setError('');
    try {
      const [lines, parts] = await Promise.all([
        api.get(`/api/repair-requests/${id}/parts`),
        api.get('/api/parts', { params: { pageNumber: 1, pageSize: 100 } }),
      ]);
      setPartLines((Array.isArray(lines.data) ? lines.data : []).map((d: any) => ({
        id: d.id ?? d.Id,
        partId: d.partId ?? d.PartId,
        partCode: d.partCode ?? d.PartCode,
        partName: d.partName ?? d.PartName,
        quantity: d.quantity ?? d.Quantity,
        unitPrice: d.unitPrice ?? d.UnitPrice,
        lineTotal: d.lineTotal ?? d.LineTotal,
        addedAt: d.addedAt ?? d.AddedAt,
      })));
      setAllParts(parsePaged(parts.data, (d: any) => ({
        id: d.id ?? d.Id,
        code: d.code ?? d.Code,
        name: d.name ?? d.Name,
        unitPrice: d.unitPrice ?? d.UnitPrice,
        stockQuantity: d.stockQuantity ?? d.StockQuantity,
        isActive: d.isActive ?? d.IsActive,
        createdAt: d.createdAt ?? d.CreatedAt,
      })).items);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const addPartLine = async () => {
    if (!partsFor || !partForm.partId) return;
    try {
      await api.post(`/api/repair-requests/${partsFor}/parts`, partForm);
      await openParts(partsFor);
      setPartForm({ partId: 0, quantity: 1 });
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const removePartLine = async (lineId: number) => {
    if (!partsFor) return;
    try {
      await api.delete(`/api/repair-requests/${partsFor}/parts/${lineId}`);
      await openParts(partsFor);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const canChangeStatus = (r: RepairRequest) => {
    if (hasRole('Admin', 'Manager')) return true;
    if (hasRole('Technician') && Number(r.technicianId) === Number(user?.id)) return true;
    return false;
  };

  return (
    <div className="space-y-4">
      <PageHeader
        title="Yêu cầu sửa chữa"
        subtitle="Theo dõi và cập nhật tiến độ sửa chữa"
        actions={canCreate ? <Button icon={<Plus size={16} />} onClick={() => setShowForm(!showForm)}>Tạo yêu cầu</Button> : undefined}
      />

      {error && <Alert type="error">{error}</Alert>}

      {showForm && (
        <form onSubmit={onCreate} className="card p-4 space-y-3">
          <Select
            label="Phiếu bảo hành"
            value={form.warrantyCardId}
            onChange={(e) => setForm({ ...form, warrantyCardId: +e.target.value })}
            placeholder="-- Chọn --"
            options={cards.filter((c) => c.status === 'Active').map((c) => ({
              value: c.id,
              label: `#${c.id} — ${c.productName || `SP #${c.productId}`}`,
            }))}
            required
          />
          <TextArea label="Mô tả sự cố" value={form.description}
            onChange={(e) => setForm({ ...form, description: e.target.value })} required />
          <div className="flex gap-2">
            <Button type="submit">Gửi</Button>
            <Button type="button" variant="secondary" onClick={() => setShowForm(false)}>Hủy</Button>
          </div>
        </form>
      )}

      {assignId && (
        <div className="card p-4 flex flex-wrap items-end gap-3">
          <div className="flex-1 min-w-[200px]">
            <Select
              label={`Gán kỹ thuật viên cho #${assignId}`}
              value={techId}
              onChange={(e) => setTechId(+e.target.value)}
              placeholder="-- Chọn --"
              options={technicians.map((t) => ({ value: t.id, label: t.fullName }))}
            />
          </div>
          <Button onClick={onAssign}>Gán</Button>
          <Button variant="secondary" onClick={() => setAssignId(null)}>Hủy</Button>
        </div>
      )}

      {editId && (
        <div className="card p-4 space-y-3">
          <TextArea label={`Sửa mô tả #${editId}`} value={editDesc} onChange={(e) => setEditDesc(e.target.value)} />
          <div className="flex gap-2">
            <Button onClick={onSaveEdit}>Lưu</Button>
            <Button variant="secondary" onClick={() => setEditId(null)}>Hủy</Button>
          </div>
        </div>
      )}

      <div className="card p-4">
        <div className="flex flex-col sm:flex-row sm:items-end gap-3">
          <div className="flex-1 min-w-0">
            <Input
              label="Tìm kiếm"
              placeholder="Mô tả, khách hàng hoặc kỹ thuật viên"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === 'Enter') {
                  setAppliedKeyword(keyword.trim());
                  setPage(1);
                }
              }}
            />
          </div>
          <div className="w-full sm:w-44 shrink-0">
            <Select
              label="Trạng thái"
              value={statusFilter}
              onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
              options={[
                { value: '', label: 'Tất cả' },
                ...statuses.map((status) => ({ value: status, label: status })),
              ]}
            />
          </div>
          <div className="shrink-0">
            <Button className="w-full sm:w-auto" onClick={() => { setAppliedKeyword(keyword.trim()); setPage(1); }}>Tìm</Button>
          </div>
        </div>
      </div>

      <div className="card overflow-x-auto">
        <table className="data-table">
          <thead>
            <tr>
              <SortHeader label="ID" column="id" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              <SortHeader label="Khách hàng" column="customer" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              <SortHeader label="Mô tả" column="description" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              <SortHeader label="Trạng thái" column="status" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              <SortHeader label="Kỹ thuật viên" column="technician" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              <SortHeader label="Ngày tạo" column="createdAt" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              <th className="px-4 py-3 text-right text-xs font-semibold uppercase">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            {items.map((r) => (
              <tr key={r.id}>
                <td className="font-medium">#{r.id}</td>
                <td>{r.customerName || `#${r.customerId}`}</td>
                <td className="max-w-xs truncate" title={r.description}>{r.description}</td>
                <td><span className={statusColor[r.status] || 'badge-neutral'}>{r.status}</span></td>
                <td>{r.technicianName || <span className="text-slate-400">Chưa gán</span>}</td>
                <td className="text-xs text-slate-500">{r.createdAt ? format(new Date(r.createdAt), 'dd/MM/yyyy HH:mm') : ''}</td>
                <td className="text-right">
                  <div className="inline-flex flex-wrap gap-1 justify-end">
                    {canAssign && !r.technicianId && r.status === 'Received' && (
                      <Button size="sm" variant="secondary" onClick={() => setAssignId(r.id)}>Gán KT</Button>
                    )}
                    {canChangeStatus(r) && (
                      <select
                        className="input text-xs py-1 w-auto"
                        value={r.status}
                        onChange={(e) => onStatus(r.id, e.target.value as RepairRequestStatus)}
                      >
                        {statuses.map((s) => (
                          <option key={s} value={s}>{s}</option>
                        ))}
                      </select>
                    )}
                    <Button size="sm" variant="ghost" onClick={() => openParts(r.id)}>Linh kiện</Button>
                    {canEdit && (
                      <>
                        <button className="btn-ghost btn-sm" onClick={() => { setEditId(r.id); setEditDesc(r.description); }} title="Sửa">
                          <Pencil size={14} />
                        </button>
                        <button className="btn-ghost btn-sm text-red-600" onClick={() => onCancel(r.id)} title="Hủy">
                          <Trash2 size={14} />
                        </button>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {items.length === 0 && <EmptyState title="Không có yêu cầu" description="Chưa có yêu cầu sửa chữa nào." />}
        <Pagination
          page={page}
          totalPages={totalPages}
          totalCount={totalCount}
          pageSize={pageSize}
          onPageChange={setPage}
          onPageSizeChange={(size) => { setPageSize(size); setPage(1); }}
        />
      </div>

      {partsFor && (
        <div className="card p-4 space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="font-semibold">Linh kiện — yêu cầu #{partsFor}</h3>
            <Button variant="secondary" size="sm" onClick={() => setPartsFor(null)}>Đóng</Button>
          </div>
          <ul className="text-sm space-y-1">
            {partLines.map((l) => (
              <li key={l.id} className="flex justify-between gap-2 border-b border-slate-100 py-1">
                <span>{l.partCode} — {l.partName} × {l.quantity}</span>
                <span className="flex items-center gap-2">
                  {Number(l.lineTotal).toLocaleString('vi-VN')} ₫
                  {canStatus && (
                    <button className="text-red-600 text-xs" onClick={() => removePartLine(l.id)}>Xóa</button>
                  )}
                </span>
              </li>
            ))}
            {partLines.length === 0 && <li className="text-slate-400">Chưa dùng linh kiện</li>}
          </ul>
          {canStatus && (
            <div className="flex flex-wrap gap-2 items-end">
              <div className="flex-1 min-w-[180px]">
                <label className="label">Linh kiện</label>
                <select className="input" value={partForm.partId} onChange={(e) => setPartForm({ ...partForm, partId: +e.target.value })}>
                  <option value={0}>-- Chọn --</option>
                  {allParts.map((p) => (
                    <option key={p.id} value={p.id}>{p.code} — {p.name} (tồn {p.stockQuantity})</option>
                  ))}
                </select>
              </div>
              <div className="w-24">
                <label className="label">SL</label>
                <input className="input" type="number" min={1} value={partForm.quantity}
                  onChange={(e) => setPartForm({ ...partForm, quantity: +e.target.value })} />
              </div>
              <Button onClick={addPartLine}>Thêm</Button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
