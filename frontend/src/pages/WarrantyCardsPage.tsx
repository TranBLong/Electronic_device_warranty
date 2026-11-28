import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { WarrantyCard, Product, User } from '../types';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import { format } from 'date-fns';
import { Button, Alert, PageHeader, EmptyState, Select, Input } from '../components/ui';
import { SortHeader } from '../components/SortHeader';
import { Pagination, parsePaged } from '../components/Pagination';

function mapCard(d: any): WarrantyCard {
  return {
    id: d.id ?? d.Id,
    productId: d.productId ?? d.ProductId,
    customerId: d.customerId ?? d.CustomerId,
    customerName: d.customerName ?? d.CustomerName,
    productName: d.productName ?? d.ProductName,
    productSerial: d.serialNumber ?? d.SerialNumber ?? d.productSerial,
    startDate: d.startDate ?? d.StartDate,
    endDate: d.endDate ?? d.EndDate,
    status: d.status ?? d.Status,
  };
}

const statusColor: Record<string, string> = {
  Active: 'badge-success',
  Expired: 'badge-neutral',
  Voided: 'badge-danger',
};

export default function WarrantyCardsPage() {
  const { hasRole } = useAuthStore();
  const canWrite = hasRole('Admin', 'Manager', 'Receptionist');
  const [items, setItems] = useState<WarrantyCard[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [customers, setCustomers] = useState<User[]>([]);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ productId: 0, customerId: 0, startDate: '' });
  const [editId, setEditId] = useState<number | null>(null);
  const [editStatus, setEditStatus] = useState('Active');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [sortBy, setSortBy] = useState('id');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc');
  const [keyword, setKeyword] = useState('');
  const [appliedKeyword, setAppliedKeyword] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  const load = async (p = page, size = pageSize) => {
    try {
      const { data } = await api.get('/api/warranty-cards', {
        params: {
          pageNumber: p,
          pageSize: size,
          keyword: appliedKeyword || undefined,
          status: statusFilter || undefined,
          sortBy,
          sortDir,
        },
      });
      const parsed = parsePaged(data, mapCard);
      setItems(parsed.items);
      setPage(parsed.page);
      setPageSize(parsed.pageSize);
      setTotalPages(parsed.totalPages);
      setTotalCount(parsed.totalCount);
      setError('');
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  useEffect(() => {
    load(page, pageSize);
  }, [page, pageSize, sortBy, sortDir, appliedKeyword, statusFilter]);

  useEffect(() => {
    const loadOptions = async () => {
      try {
        const { data } = await api.get('/api/products', {
          params: { pageNumber: 1, pageSize: 100 },
        });
        setProducts(parsePaged(data, (d: any) => ({
          id: d.id ?? d.Id,
          name: d.name ?? d.Name,
          brand: d.brand ?? d.Brand,
          model: d.model ?? d.Model,
          serialNumber: d.serialNumber ?? d.SerialNumber,
          warrantyMonths: d.warrantyMonths ?? d.WarrantyMonths,
          createdAt: d.createdAt ?? d.CreatedAt,
        })).items);

        if (canWrite) {
          try {
            const custRes = await api.get('/api/users/customers');
            setCustomers(parsePaged(custRes.data, (d: any) => ({
              id: d.id ?? d.Id,
              fullName: d.fullName ?? d.FullName,
              email: d.email ?? d.Email,
              role: d.role ?? d.Role,
              isActive: d.isActive ?? d.IsActive,
              createdAt: d.createdAt ?? d.CreatedAt,
            })).items);
          } catch { /* */ }
        }
      } catch (err) {
        setError(getErrorMessage(err));
      }
    };
    loadOptions();
  }, [canWrite]);

  const toggleSort = (column: string) => {
    setSortDir((direction) => sortBy === column ? (direction === 'asc' ? 'desc' : 'asc') : 'asc');
    setSortBy(column);
    setPage(1);
  };

  const onCreate = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await api.post('/api/warranty-cards', {
        productId: form.productId,
        customerId: form.customerId,
        startDate: form.startDate || new Date().toISOString().slice(0, 10),
      });
      setShowForm(false);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const onUpdateStatus = async () => {
    if (!editId) return;
    setError('');
    try {
      await api.put(`/api/warranty-cards/${editId}`, { status: editStatus });
      setEditId(null);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const onDelete = async (id: number) => {
    if (!confirm('Xóa / vô hiệu phiếu bảo hành này?')) return;
    setError('');
    try {
      await api.delete(`/api/warranty-cards/${id}`);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div className="space-y-4">
      <PageHeader title="Phiếu bảo hành" subtitle="Quản lý thời hạn bảo hành thiết bị"
        actions={canWrite ? <Button icon={<Plus size={16} />} onClick={() => setShowForm(!showForm)}>Tạo phiếu BH</Button> : undefined} />

      {error && <Alert type="error">{error}</Alert>}

      {showForm && (
        <form onSubmit={onCreate} className="card p-4 grid grid-cols-1 sm:grid-cols-2 gap-3">
          <Select label="Sản phẩm" value={form.productId} required placeholder="-- Chọn --"
            onChange={(e) => setForm({ ...form, productId: +e.target.value })}
            options={products.map((p) => ({ value: p.id, label: `${p.name} (${p.serialNumber})` }))} />
          <Select label="Khách hàng" value={form.customerId} required placeholder="-- Chọn --"
            onChange={(e) => setForm({ ...form, customerId: +e.target.value })}
            options={customers.map((c) => ({ value: c.id, label: `${c.fullName} (${c.email})` }))} />
          <Input label="Ngày bắt đầu" type="date" value={form.startDate}
            onChange={(e) => setForm({ ...form, startDate: e.target.value })} />
          <div className="flex items-end gap-2">
            <Button type="submit">Lưu</Button>
            <Button type="button" variant="secondary" onClick={() => setShowForm(false)}>Hủy</Button>
          </div>
        </form>
      )}

      {editId && canWrite && (
        <div className="card p-4 flex flex-wrap items-end gap-3">
          <Select label={`Cập nhật trạng thái #${editId}`} value={editStatus}
            onChange={(e) => setEditStatus(e.target.value)}
            options={[
              { value: 'Active', label: 'Active' },
              { value: 'Expired', label: 'Expired' },
              { value: 'Voided', label: 'Voided' },
            ]} />
          <Button onClick={onUpdateStatus}>Lưu</Button>
          <Button variant="secondary" onClick={() => setEditId(null)}>Hủy</Button>
        </div>
      )}

      <div className="card p-4">
        <div className="flex flex-col sm:flex-row sm:items-end gap-3">
          <div className="flex-1 min-w-0">
            <Input
              label="Tìm kiếm"
              placeholder="Sản phẩm, số serial hoặc khách hàng"
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
                { value: 'Active', label: 'Active' },
                { value: 'Expired', label: 'Expired' },
                { value: 'Voided', label: 'Voided' },
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
              <SortHeader label="Sản phẩm" column="productName" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              <SortHeader label="Khách hàng" column="customerName" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              <SortHeader label="Bắt đầu" column="startDate" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              <SortHeader label="Kết thúc" column="endDate" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              <SortHeader label="Trạng thái" column="status" sortKey={sortBy} sortDir={sortDir} onToggle={toggleSort} />
              {canWrite && <th className="px-4 py-3 text-right text-xs font-semibold uppercase">Thao tác</th>}
            </tr>
          </thead>
          <tbody>
            {items.map((c) => (
              <tr key={c.id}>
                <td>#{c.id}</td>
                <td>{c.productName || `SP #${c.productId}`}</td>
                <td>{c.customerName || `KH #${c.customerId}`}</td>
                <td>{c.startDate ? format(new Date(c.startDate), 'dd/MM/yyyy') : '-'}</td>
                <td>{c.endDate ? format(new Date(c.endDate), 'dd/MM/yyyy') : '-'}</td>
                <td><span className={statusColor[c.status] || 'badge-neutral'}>{c.status}</span></td>
                {canWrite && (
                  <td className="text-right space-x-1">
                    <button className="btn-ghost btn-sm" onClick={() => { setEditId(c.id); setEditStatus(c.status); }}><Pencil size={14} /></button>
                    <button className="btn-ghost btn-sm text-red-600" onClick={() => onDelete(c.id)}><Trash2 size={14} /></button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
        {items.length === 0 && <EmptyState title="Không có phiếu bảo hành" />}
        <Pagination
          page={page}
          totalPages={totalPages}
          totalCount={totalCount}
          pageSize={pageSize}
          onPageChange={setPage}
          onPageSizeChange={(size) => { setPageSize(size); setPage(1); }}
        />
      </div>
    </div>
  );
}
