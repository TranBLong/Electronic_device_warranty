import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import type { User, UserRole } from '../types';
import { Plus } from 'lucide-react';
import { format } from 'date-fns';

function mapUser(d: any): User {
  return {
    id: d.id ?? d.Id,
    fullName: d.fullName ?? d.FullName,
    email: d.email ?? d.Email,
    phone: d.phone ?? d.Phone,
    role: d.role ?? d.Role,
    isActive: d.isActive ?? d.IsActive,
    createdAt: d.createdAt ?? d.CreatedAt,
  };
}

const roles: UserRole[] = ['Admin', 'Manager', 'Receptionist', 'Technician', 'Customer'];

export default function UsersPage() {
  const [items, setItems] = useState<User[]>([]);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({
    fullName: '',
    email: '',
    password: '',
    phone: '',
    role: 'Customer' as UserRole,
    isActive: true,
  });

  const load = async () => {
    try {
      const { data } = await api.get('/api/users');
      setItems((Array.isArray(data) ? data : []).map(mapUser));
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
      await api.post('/api/users', form);
      setShowForm(false);
      setForm({ fullName: '', email: '', password: '', phone: '', role: 'Customer', isActive: true });
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const toggleActive = async (u: User) => {
    try {
      await api.put(`/api/users/${u.id}`, {
        fullName: u.fullName,
        email: u.email,
        phone: u.phone,
        role: u.role,
        isActive: !u.isActive,
      });
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <h2 className="text-xl font-bold text-slate-800">Người dùng</h2>
        <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
          <Plus size={16} /> Thêm user
        </button>
      </div>

      {error && <div className="rounded-lg bg-red-50 text-red-700 text-sm px-3 py-2">{error}</div>}

      {showForm && (
        <form onSubmit={onCreate} className="card p-4 grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="label">Họ tên</label>
            <input className="input" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} required />
          </div>
          <div>
            <label className="label">Email</label>
            <input className="input" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
          </div>
          <div>
            <label className="label">Mật khẩu</label>
            <input className="input" type="password" minLength={8} value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} required />
          </div>
          <div>
            <label className="label">SĐT</label>
            <input className="input" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
          </div>
          <div>
            <label className="label">Vai trò</label>
            <select className="input" value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as UserRole })}>
              {roles.map((r) => (
                <option key={r} value={r}>{r}</option>
              ))}
            </select>
          </div>
          <div className="flex items-end gap-2">
            <button type="submit" className="btn-primary">Lưu</button>
            <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Hủy</button>
          </div>
        </form>
      )}

      <div className="card overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-slate-600 border-b">
            <tr>
              <th className="text-left px-4 py-3 font-medium">Họ tên</th>
              <th className="text-left px-4 py-3 font-medium">Email</th>
              <th className="text-left px-4 py-3 font-medium">SĐT</th>
              <th className="text-left px-4 py-3 font-medium">Vai trò</th>
              <th className="text-left px-4 py-3 font-medium">Trạng thái</th>
              <th className="text-left px-4 py-3 font-medium">Ngày tạo</th>
              <th className="text-left px-4 py-3 font-medium"></th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {items.map((u) => (
              <tr key={u.id} className="hover:bg-slate-50">
                <td className="px-4 py-3 font-medium">{u.fullName}</td>
                <td className="px-4 py-3">{u.email}</td>
                <td className="px-4 py-3">{u.phone || '—'}</td>
                <td className="px-4 py-3">
                  <span className="badge bg-brand-100 text-brand-800">{u.role}</span>
                </td>
                <td className="px-4 py-3">
                  <span className={`badge ${u.isActive ? 'bg-emerald-100 text-emerald-800' : 'bg-slate-100 text-slate-600'}`}>
                    {u.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-4 py-3 text-xs">{u.createdAt ? format(new Date(u.createdAt), 'dd/MM/yyyy') : '—'}</td>
                <td className="px-4 py-3">
                  <button className="text-xs text-brand-600 hover:underline" onClick={() => toggleActive(u)}>
                    {u.isActive ? 'Vô hiệu' : 'Kích hoạt'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
