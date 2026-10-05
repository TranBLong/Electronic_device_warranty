import { FormEvent, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';

export default function ProfilePage() {
  const { user, fetchMe } = useAuthStore();
  const [form, setForm] = useState({
    fullName: user?.fullName || '',
    email: user?.email || '',
    phone: user?.phone || '',
  });
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setMessage('');
    setLoading(true);
    try {
      await api.put('/api/users/me', form);
      await fetchMe();
      setMessage('Cập nhật hồ sơ thành công.');
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="max-w-lg space-y-4">
      <h2 className="text-xl font-bold text-slate-800">Hồ sơ cá nhân</h2>

      {message && <div className="rounded-lg bg-emerald-50 text-emerald-700 text-sm px-3 py-2">{message}</div>}
      {error && <div className="rounded-lg bg-red-50 text-red-700 text-sm px-3 py-2">{error}</div>}

      <form onSubmit={onSubmit} className="card p-5 space-y-4">
        <div>
          <label className="label">Vai trò</label>
          <input className="input bg-slate-50" value={user?.role || ''} disabled />
        </div>
        <div>
          <label className="label">Họ và tên</label>
          <input className="input" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} required />
        </div>
        <div>
          <label className="label">Email</label>
          <input className="input" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
        </div>
        <div>
          <label className="label">Số điện thoại</label>
          <input className="input" value={form.phone || ''} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
        </div>
        <button type="submit" className="btn-primary" disabled={loading}>
          {loading ? 'Đang lưu...' : 'Lưu thay đổi'}
        </button>
      </form>
    </div>
  );
}
