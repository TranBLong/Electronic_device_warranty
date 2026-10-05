import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { RepairRequest, WarrantyCard, User, RepairRequestStatus } from '../types';
import { Plus } from 'lucide-react';
import { format } from 'date-fns';

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
  Received: 'bg-amber-100 text-amber-800',
  InProgress: 'bg-blue-100 text-blue-800',
  Completed: 'bg-emerald-100 text-emerald-800',
  Returned: 'bg-slate-100 text-slate-700',
  Cancelled: 'bg-red-100 text-red-800',
};

const statuses: RepairRequestStatus[] = ['Received', 'InProgress', 'Completed', 'Returned', 'Cancelled'];

export default function RepairRequestsPage() {
  const { hasRole, user } = useAuthStore();
  const canCreate = hasRole('Admin', 'Receptionist', 'Customer');
  const canAssign = hasRole('Admin', 'Manager', 'Receptionist');
  const canUpdateStatus = hasRole('Technician');
  const [items, setItems] = useState<RepairRequest[]>([]);
  const [cards, setCards] = useState<WarrantyCard[]>([]);
  const [technicians, setTechnicians] = useState<User[]>([]);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ warrantyCardId: 0, description: '' });
  const [assignId, setAssignId] = useState<number | null>(null);
  const [techId, setTechId] = useState(0);

  const load = async () => {
    try {
      const [reqRes, cardRes] = await Promise.all([
        api.get('/api/repair-requests'),
        api.get('/api/warranty-cards'),
      ]);
      setItems((Array.isArray(reqRes.data) ? reqRes.data : []).map(mapReq));
      setCards(
        (Array.isArray(cardRes.data) ? cardRes.data : []).map((d: any) => ({
          id: d.id ?? d.Id,
          productId: d.productId ?? d.ProductId,
          customerId: d.customerId ?? d.CustomerId,
          status: d.status ?? d.Status,
          startDate: d.startDate ?? d.StartDate,
          endDate: d.endDate ?? d.EndDate,
          productName: d.productName ?? d.ProductName,
        }))
      );
      if (canAssign) {
        try {
          const techRes = await api.get('/api/users/technicians');
          setTechnicians(
            (Array.isArray(techRes.data) ? techRes.data : []).map((d: any) => ({
              id: d.id ?? d.Id,
              fullName: d.fullName ?? d.FullName,
              email: d.email ?? d.Email,
              role: d.role ?? d.Role,
              isActive: d.isActive ?? d.IsActive,
              createdAt: d.createdAt ?? d.CreatedAt,
            }))
          );
        } catch { /* ignore */ }
      }
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  useEffect(() => {
    load();
  }, []);

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

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <h2 className="text-xl font-bold text-slate-800">Yêu cầu sửa chữa</h2>
        {canCreate && (
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
            <Plus size={16} /> Tạo yêu cầu
          </button>
        )}
      </div>

      {error && <div className="rounded-lg bg-red-50 text-red-700 text-sm px-3 py-2">{error}</div>}

      {showForm && (
        <form onSubmit={onCreate} className="card p-4 space-y-3">
          <div>
            <label className="label">Phiếu bảo hành</label>
            <select className="input" value={form.warrantyCardId} onChange={(e) => setForm({ ...form, warrantyCardId: +e.target.value })} required>
              <option value={0}>-- Chọn --</option>
              {cards.filter((c) => c.status === 'Active').map((c) => (
                <option key={c.id} value={c.id}>
                  #{c.id} — {c.productName || `SP #${c.productId}`}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="label">Mô tả sự cố</label>
            <textarea className="input min-h-[80px]" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} required />
          </div>
          <div className="flex gap-2">
            <button type="submit" className="btn-primary">Gửi</button>
            <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Hủy</button>
          </div>
        </form>
      )}

      {assignId && (
        <div className="card p-4 flex flex-wrap items-end gap-3">
          <div className="flex-1 min-w-[200px]">
            <label className="label">Gán kỹ thuật viên cho #{assignId}</label>
            <select className="input" value={techId} onChange={(e) => setTechId(+e.target.value)}>
              <option value={0}>-- Chọn --</option>
              {technicians.map((t) => (
                <option key={t.id} value={t.id}>{t.fullName}</option>
              ))}
            </select>
          </div>
          <button className="btn-primary" onClick={onAssign}>Gán</button>
          <button className="btn-secondary" onClick={() => setAssignId(null)}>Hủy</button>
        </div>
      )}

      <div className="space-y-3">
        {items.map((r) => (
          <div key={r.id} className="card p-4">
            <div className="flex flex-wrap items-start justify-between gap-2">
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-slate-800">#{r.id}</span>
                  <span className={`badge ${statusColor[r.status] || ''}`}>{r.status}</span>
                </div>
                <p className="text-sm text-slate-600 mt-1">{r.description}</p>
                <p className="text-xs text-slate-400 mt-2">
                  KH: {r.customerName || `#${r.customerId}`} · Phiếu BH: #{r.warrantyCardId}
                  {r.technicianName && ` · KT: ${r.technicianName}`}
                  {' · '}
                  {r.createdAt ? format(new Date(r.createdAt), 'dd/MM/yyyy HH:mm') : ''}
                </p>
              </div>
              <div className="flex flex-wrap gap-2">
                {canAssign && !r.technicianId && r.status === 'Received' && (
                  <button className="btn-secondary text-xs" onClick={() => setAssignId(r.id)}>Gán KT</button>
                )}
                {canUpdateStatus && r.technicianId === user?.id && (
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
              </div>
            </div>
            {r.statusHistory?.length > 0 && (
              <div className="mt-3 pt-3 border-t border-slate-100">
                <p className="text-xs font-medium text-slate-500 mb-1">Lịch sử trạng thái</p>
                <ul className="text-xs text-slate-500 space-y-0.5">
                  {r.statusHistory.map((h) => (
                    <li key={h.id}>
                      {h.oldStatus || '—'} → <strong>{h.newStatus}</strong> · {h.changedAt ? format(new Date(h.changedAt), 'dd/MM/yyyy HH:mm') : ''}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        ))}
        {items.length === 0 && <div className="card p-8 text-center text-slate-400">Không có yêu cầu nào</div>}
      </div>
    </div>
  );
}
