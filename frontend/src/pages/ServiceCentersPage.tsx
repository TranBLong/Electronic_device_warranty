import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { ServiceCenter } from '../types';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import { Button, Input, Alert, Modal, PageHeader, EmptyState } from '../components/ui';
import { useSort } from '../hooks/useSort';
import { SortHeader } from '../components/SortHeader';

function map(d: any): ServiceCenter {
  return {
    id: d.id ?? d.Id,
    name: d.name ?? d.Name,
    address: d.address ?? d.Address,
    phone: d.phone ?? d.Phone,
    isActive: d.isActive ?? d.IsActive,
    createdAt: d.createdAt ?? d.CreatedAt,
  };
}

export default function ServiceCentersPage() {
  const { hasRole } = useAuthStore();
  const canWrite = hasRole('Admin', 'Manager');
  const [items, setItems] = useState<ServiceCenter[]>([]);
  const [error, setError] = useState('');
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<ServiceCenter | null>(null);
  const [form, setForm] = useState({ name: '', address: '', phone: '' });
  const [saving, setSaving] = useState(false);

  const load = async () => {
    try {
      const { data } = await api.get('/api/service-centers');
      setItems((Array.isArray(data) ? data : []).map(map));
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  useEffect(() => { load(); }, []);
  const { sorted, sortKey, sortDir, toggle } = useSort(items, 'name', 'asc');

  const openCreate = () => {
    setEditing(null);
    setForm({ name: '', address: '', phone: '' });
    setModal(true);
  };
  const openEdit = (c: ServiceCenter) => {
    setEditing(c);
    setForm({ name: c.name, address: c.address, phone: c.phone || '' });
    setModal(true);
  };

  const onSave = async (e?: FormEvent) => {
    e?.preventDefault();
    setSaving(true);
    setError('');
    try {
      if (editing) await api.put(`/api/service-centers/${editing.id}`, form);
      else await api.post('/api/service-centers', form);
      setModal(false);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const onDelete = async (c: ServiceCenter) => {
    if (!confirm(`Vô hiệu hóa trung tâm "${c.name}"?`)) return;
    try {
      await api.delete(`/api/service-centers/${c.id}`);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div>
      <PageHeader title="Trung tâm bảo hành" subtitle="Danh sách chi nhánh / service center"
        actions={canWrite ? <Button icon={<Plus size={16} />} onClick={openCreate}>Thêm</Button> : undefined} />
      {error && <div className="mb-4"><Alert type="error">{error}</Alert></div>}
      <div className="card overflow-x-auto">
        <table className="data-table">
          <thead>
            <tr>
              <SortHeader label="Tên" column="name" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              <SortHeader label="Địa chỉ" column="address" sortKey={sortKey} sortDir={sortDir} onToggle={toggle} />
              <th className="px-4 py-3 text-xs font-semibold uppercase">SĐT</th>
              <th className="px-4 py-3 text-xs font-semibold uppercase">Trạng thái</th>
              {canWrite && <th className="px-4 py-3 text-right text-xs font-semibold uppercase">Thao tác</th>}
            </tr>
          </thead>
          <tbody>
            {sorted.map((c) => (
              <tr key={c.id}>
                <td className="font-medium">{c.name}</td>
                <td>{c.address}</td>
                <td>{c.phone || '—'}</td>
                <td><span className={c.isActive ? 'badge-success' : 'badge-neutral'}>{c.isActive ? 'Active' : 'Inactive'}</span></td>
                {canWrite && (
                  <td className="text-right space-x-1">
                    <button className="btn-ghost btn-sm" onClick={() => openEdit(c)}><Pencil size={14} /></button>
                    <button className="btn-ghost btn-sm text-red-600" onClick={() => onDelete(c)}><Trash2 size={14} /></button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
        {sorted.length === 0 && <EmptyState title="Chưa có trung tâm" />}
      </div>
      <Modal open={modal} onClose={() => setModal(false)} title={editing ? 'Sửa trung tâm' : 'Thêm trung tâm'}
        footer={<><Button variant="secondary" onClick={() => setModal(false)}>Hủy</Button><Button loading={saving} onClick={() => onSave()}>Lưu</Button></>}>
        <form className="space-y-3" onSubmit={onSave}>
          <Input label="Tên" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          <Input label="Địa chỉ" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} required />
          <Input label="SĐT" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
        </form>
      </Modal>
    </div>
  );
}
