import { FormEvent, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { Gender } from '../types';
import { UserCircle, Lock, Shield, Camera, Trash2 } from 'lucide-react';

export default function ProfilePage() {
  const { user, fetchMe } = useAuthStore();
  const [form, setForm] = useState({
    fullName: user?.fullName || '',
    email: user?.email || '',
    phone: user?.phone || '',
    address: user?.address || '',
    dateOfBirth: user?.dateOfBirth ? user.dateOfBirth.slice(0, 10) : '',
    gender: (user?.gender || '') as Gender | '',
  });
  const [pwd, setPwd] = useState({ currentPassword: '', newPassword: '', confirm: '' });
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [pwdLoading, setPwdLoading] = useState(false);
  const [avatarLoading, setAvatarLoading] = useState(false);

  const onAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    setError('');
    setMessage('');
    setAvatarLoading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      await api.post('/api/users/me/avatar', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      await fetchMe();
      setMessage('Cập nhật ảnh đại diện thành công.');
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setAvatarLoading(false);
      e.target.value = '';
    }
  };

  const onRemoveAvatar = async () => {
    setError('');
    setMessage('');
    setAvatarLoading(true);
    try {
      await api.delete('/api/users/me/avatar');
      await fetchMe();
      setMessage('Đã xóa ảnh đại diện.');
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setAvatarLoading(false);
    }
  };

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setMessage('');
    setLoading(true);
    try {
      await api.put('/api/users/me', {
        ...form,
        phone: form.phone.trim() || null,
        address: form.address.trim() || null,
        dateOfBirth: form.dateOfBirth || null,
        gender: form.gender || null,
      });
      await fetchMe();
      setMessage('Cập nhật hồ sơ thành công.');
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const onChangePassword = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setMessage('');
    if (pwd.newPassword !== pwd.confirm) {
      setError('Mật khẩu mới và xác nhận không khớp.');
      return;
    }
    if (pwd.newPassword.length < 6) {
      setError('Mật khẩu mới tối thiểu 6 ký tự.');
      return;
    }
    setPwdLoading(true);
    try {
      await api.post('/api/users/me/change-password', {
        currentPassword: pwd.currentPassword,
        newPassword: pwd.newPassword,
      });
      setPwd({ currentPassword: '', newPassword: '', confirm: '' });
      setMessage('Đổi mật khẩu thành công.');
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setPwdLoading(false);
    }
  };

  return (
    <div className="space-y-6 max-w-2xl">
      <div className="rounded-2xl bg-gradient-to-r from-indigo-600 via-violet-600 to-purple-600 p-6 text-white shadow-lg">
        <div className="flex items-center gap-4">
          <div className="relative shrink-0">
            {user?.avatarUrl ? (
              <img
                src={user.avatarUrl}
                alt="Ảnh đại diện"
                className="h-20 w-20 rounded-full object-cover border-2 border-white/40 bg-slate-200"
              />
            ) : (
              <div className="rounded-full bg-white/20 p-4">
                <UserCircle size={40} />
              </div>
            )}
            <label
              className="absolute bottom-0 right-0 cursor-pointer rounded-full bg-white p-2 text-indigo-700 shadow-lg"
              title="Thay ảnh đại diện"
            >
              <Camera size={14} />
              <input
                type="file"
                accept="image/jpeg,image/png,image/webp,image/gif"
                className="hidden"
                onChange={onAvatarChange}
                disabled={avatarLoading}
              />
            </label>
          </div>
          <div className="flex-1">
            <h2 className="text-2xl font-bold">{user?.fullName || 'Hồ sơ'}</h2>
            <p className="text-indigo-100 text-sm mt-0.5">
              {user?.email} · <span className="font-medium">{user?.role}</span>
            </p>
          </div>
          {user?.avatarUrl && !user.avatarUrl.endsWith('/default-avatar.svg') && (
            <button
              type="button"
              onClick={onRemoveAvatar}
              disabled={avatarLoading}
              className="flex items-center gap-1 rounded-lg bg-white/15 px-3 py-2 text-xs font-medium hover:bg-white/25 disabled:opacity-50"
            >
              <Trash2 size={14} /> Xóa ảnh
            </button>
          )}
        </div>
      </div>

      {message && (
        <div className="rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-sm px-4 py-3">
          {message}
        </div>
      )}
      {error && (
        <div className="rounded-xl bg-red-50 border border-red-200 text-red-700 text-sm px-4 py-3">
          {error}
        </div>
      )}

      <form onSubmit={onSubmit} className="rounded-2xl border border-indigo-100 bg-white p-6 shadow-sm space-y-4">
        <h3 className="font-semibold text-slate-800 flex items-center gap-2">
          <Shield size={18} className="text-indigo-600" /> Thông tin cá nhân
        </h3>
        <div className="grid sm:grid-cols-2 gap-4">
          <div className="sm:col-span-2">
            <label className="label">Vai trò</label>
            <input className="input bg-slate-50" value={user?.role || ''} disabled />
          </div>
          <div className="sm:col-span-2">
            <label className="label">Họ và tên</label>
            <input
              className="input"
              value={form.fullName}
              onChange={(e) => setForm({ ...form, fullName: e.target.value })}
              required
            />
          </div>
          <div>
            <label className="label">Email</label>
            <input
              className="input"
              type="email"
              value={form.email}
              onChange={(e) => setForm({ ...form, email: e.target.value })}
              required
            />
          </div>
          <div>
            <label className="label">Số điện thoại</label>
            <input
              className="input"
              value={form.phone || ''}
              onChange={(e) => setForm({ ...form, phone: e.target.value })}
            />
          </div>
          <div className="sm:col-span-2">
            <label className="label">Địa chỉ</label>
            <input
              className="input"
              value={form.address}
              onChange={(e) => setForm({ ...form, address: e.target.value })}
              placeholder="Số nhà, đường, quận/huyện, tỉnh/thành"
            />
          </div>
          <div>
            <label className="label">Ngày sinh</label>
            <input
              className="input"
              type="date"
              value={form.dateOfBirth}
              onChange={(e) => setForm({ ...form, dateOfBirth: e.target.value })}
            />
          </div>
          <div>
            <label className="label">Giới tính</label>
            <select
              className="input"
              value={form.gender}
              onChange={(e) => setForm({ ...form, gender: e.target.value as Gender | '' })}
            >
              <option value="">-- Chọn --</option>
              <option value="Male">Nam</option>
              <option value="Female">Nữ</option>
              <option value="Other">Khác</option>
            </select>
          </div>
        </div>
        <button type="submit" className="btn-primary" disabled={loading}>
          {loading ? 'Đang lưu…' : 'Lưu hồ sơ'}
        </button>
      </form>

      <form
        onSubmit={onChangePassword}
        className="rounded-2xl border border-slate-200 bg-slate-50 p-6 shadow-sm space-y-4"
      >
        <h3 className="font-semibold text-slate-800 flex items-center gap-2">
          <Lock size={18} className="text-slate-600" /> Đổi mật khẩu
        </h3>
        <div>
          <label className="label">Mật khẩu hiện tại</label>
          <input
            className="input"
            type="password"
            value={pwd.currentPassword}
            onChange={(e) => setPwd({ ...pwd, currentPassword: e.target.value })}
            required
            autoComplete="current-password"
          />
        </div>
        <div className="grid sm:grid-cols-2 gap-4">
          <div>
            <label className="label">Mật khẩu mới</label>
            <input
              className="input"
              type="password"
              value={pwd.newPassword}
              onChange={(e) => setPwd({ ...pwd, newPassword: e.target.value })}
              required
              minLength={6}
              autoComplete="new-password"
            />
          </div>
          <div>
            <label className="label">Xác nhận mật khẩu mới</label>
            <input
              className="input"
              type="password"
              value={pwd.confirm}
              onChange={(e) => setPwd({ ...pwd, confirm: e.target.value })}
              required
              minLength={6}
              autoComplete="new-password"
            />
          </div>
        </div>
        <button type="submit" className="btn-secondary" disabled={pwdLoading}>
          {pwdLoading ? 'Đang đổi…' : 'Đổi mật khẩu'}
        </button>
      </form>
    </div>
  );
}
