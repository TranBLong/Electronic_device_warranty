import { FormEvent, useCallback, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { User, UserRole } from '../types';
import { Plus, Pencil, Trash2, Search, Users } from 'lucide-react';
import { format } from 'date-fns';
import { Button, Alert, Modal } from '../components/ui';
import { Pagination, parsePaged } from '../components/Pagination';

function mapUser(d: any): User {
  return {
    id: d.id ?? d.Id,
    fullName: d.fullName ?? d.FullName,
    email: d.email ?? d.Email,
    phone: d.phone ?? d.Phone,
    address: d.address ?? d.Address ?? null,
    dateOfBirth: d.dateOfBirth ?? d.DateOfBirth ?? null,
    gender: d.gender ?? d.Gender ?? null,
    role: d.role ?? d.Role,
    isActive: d.isActive ?? d.IsActive,
    createdAt: d.createdAt ?? d.CreatedAt,
  };
}

const roles: UserRole[] = ['Admin', 'Manager', 'Receptionist', 'Technician', 'Customer'];

const roleBadge: Record<string, string> = {
  Admin: 'badge-danger',
  Manager: 'badge-purple',
  Receptionist: 'badge-info',
  Technician: 'badge-warning',
  Customer: 'badge-success',
};

const emptyForm = {
  fullName: '',
  email: '',
  password: '',
  phone: '',
  address: '',
  dateOfBirth: '',
  gender: '',
  role: 'Customer' as UserRole,
  isActive: true,
};

export default function UsersPage() {
  const { hasRole, user: me } = useAuthStore();
  const isAdmin = hasRole('Admin');
  const canManage = hasRole('Admin', 'Manager');

  const [items, setItems] = useState<User[]>([]);
  const [error, setError] = useState('');
  const [keyword, setKeyword] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<User | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);

  const load = useCallback(
    async (p = page, size = pageSize) => {
      if (!canManage) return;
      setLoading(true);
      try {
        const { data } = await api.get('/api/users', {
          params: {
            pageNumber: p,
            pageSize: size,
            keyword: keyword.trim() || undefined,
            role: roleFilter || undefined,
            sortBy: 'fullname',
            isDescending: false,
          },
        });
        const parsed = parsePaged(data, mapUser);
        setItems(parsed.items);
        setPage(parsed.page);
        setPageSize(parsed.pageSize);
        setTotalPages(parsed.totalPages);
        setTotalCount(parsed.totalCount);
        setError('');
      } catch (err) {
        setError(getErrorMessage(err));
      } finally {
        setLoading(false);
      }
    },
    [canManage, keyword, roleFilter, page, pageSize]
  );

  useEffect(() => {
    load(1, pageSize);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [roleFilter]);

  useEffect(() => {
    load(page, pageSize);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize]);

  const searchNow = () => {
    setPage(1);
    load(1, pageSize);
  };

  const openCreate = () => {
    if (!isAdmin) return;
    setEditing(null);
    setForm(emptyForm);
    setModal(true);
  };

  const openEdit = (u: User) => {
    if (!isAdmin) return;
    setEditing(u);
    setForm({
      fullName: u.fullName,
      email: u.email,
      password: '',
      phone: u.phone || '',
      address: u.address || '',
      dateOfBirth: u.dateOfBirth ? String(u.dateOfBirth).slice(0, 10) : '',
      gender: u.gender || '',
      role: u.role,
      isActive: u.isActive,
    });
    setModal(true);
  };

  const onSave = async (e?: FormEvent) => {
    e?.preventDefault();
    if (!isAdmin) return;
    setError('');
    setSaving(true);
    try {
      if (editing) {
        await api.put(`/api/users/${editing.id}`, {
          fullName: form.fullName.trim(),
          email: form.email.trim(),
          phone: form.phone.trim() || null,
          address: form.address.trim() || null,
          dateOfBirth: form.dateOfBirth || null,
          gender: form.gender || null,
          role: form.role,
          isActive: form.isActive,
          newPassword: form.password.trim() || null,
        });
      } else {
        if (form.password.length < 8) {
          setError('Mật khẩu tối thiểu 8 ký tự');
          setSaving(false);
          return;
        }
        await api.post('/api/users', {
          fullName: form.fullName.trim(),
          email: form.email.trim(),
          password: form.password,
          phone: form.phone.trim() || null,
          address: form.address.trim() || null,
          dateOfBirth: form.dateOfBirth || null,
          gender: form.gender || null,
          role: form.role,
          isActive: form.isActive,
        });
      }
      setModal(false);
      await load(page, pageSize);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const onDelete = async (u: User) => {
    if (!isAdmin) return;
    if (u.id === me?.id) {
      setError('Không thể vô hiệu hóa chính tài khoản đang đăng nhập.');
      return;
    }
    if (!confirm(`Vô hiệu hóa tài khoản "${u.fullName}" (${u.email})?`)) return;
    try {
      await api.delete(`/api/users/${u.id}`);
      await load(page, pageSize);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div className="space-y-5">
      <div className="rounded-2xl bg-gradient-to-r from-slate-800 via-slate-700 to-cyan-800 p-6 text-white shadow-lg">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-white/15 p-3">
              <Users size={28} />
            </div>
            <div>
              <h2 className="text-2xl font-bold">Người dùng</h2>
              <p className="text-slate-200 text-sm mt-0.5">
                {totalCount > 0
                  ? `${totalCount.toLocaleString()} tài khoản · phân trang server`
                  : 'Quản lý tài khoản hệ thống'}
              </p>
            </div>
          </div>
          {isAdmin && (
            <Button
              className="!bg-white !text-slate-800 hover:!bg-slate-100"
              icon={<Plus size={16} />}
              onClick={openCreate}
            >
              Thêm người dùng
            </Button>
          )}
        </div>
      </div>

      {error && <Alert type="error">{error}</Alert>}

      <div className="card p-4">
        <div className="flex flex-col sm:flex-row sm:items-end gap-3">
          <div className="relative flex-1 min-w-0">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              className="input pl-9"
              placeholder="Tìm tên, email, SĐT…"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && searchNow()}
            />
          </div>
          <div className="w-full sm:w-44 shrink-0">
            <select
              className="input"
              value={roleFilter}
              onChange={(e) => {
                setRoleFilter(e.target.value);
                setPage(1);
              }}
            >
              <option value="">Tất cả vai trò</option>
              {roles.map((r) => (
                <option key={r} value={r}>
                  {r}
                </option>
              ))}
            </select>
          </div>
          <div className="shrink-0">
            <Button variant="secondary" className="w-full sm:w-auto" onClick={searchNow} disabled={loading}>
              {loading ? 'Đang tải…' : 'Tìm kiếm'}
            </Button>
          </div>
        </div>
      </div>

      <div className="card overflow-x-auto">
        <table className="data-table">
          <thead>
            <tr>
              <th>Họ tên</th>
              <th>Email</th>
              <th>SĐT</th>
              <th>Vai trò</th>
              <th>Trạng thái</th>
              <th>Ngày tạo</th>
              {isAdmin && <th className="text-right">Thao tác</th>}
            </tr>
          </thead>
          <tbody>
            {items.map((u) => (
              <tr key={u.id}>
                <td className="font-medium">{u.fullName}</td>
                <td className="text-slate-600">{u.email}</td>
                <td>{u.phone || '—'}</td>
                <td>
                  <span className={roleBadge[u.role] || 'badge-neutral'}>{u.role}</span>
                </td>
                <td>
                  <span className={u.isActive ? 'badge-success' : 'badge-neutral'}>
                    {u.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="text-xs text-slate-500">
                  {u.createdAt ? format(new Date(u.createdAt), 'dd/MM/yyyy') : '—'}
                </td>
                {isAdmin && (
                  <td className="text-right whitespace-nowrap space-x-1">
                    <button className="btn-ghost btn-sm" title="Sửa" onClick={() => openEdit(u)}>
                      <Pencil size={14} />
                    </button>
                    <button
                      className="btn-ghost btn-sm text-red-600"
                      title="Vô hiệu hóa"
                      onClick={() => onDelete(u)}
                      disabled={u.id === me?.id}
                    >
                      <Trash2 size={14} />
                    </button>
                  </td>
                )}
              </tr>
            ))}
            {items.length === 0 && !loading && (
              <tr>
                <td colSpan={isAdmin ? 7 : 6} className="text-center text-slate-400 py-10">
                  Không có dữ liệu (kiểm tra seed Development hoặc quyền Admin/Manager)
                </td>
              </tr>
            )}
          </tbody>
        </table>

        <div className="p-4">
          <Pagination
            page={page}
            totalPages={totalPages}
            totalCount={totalCount}
            pageSize={pageSize}
            onPageChange={setPage}
            onPageSizeChange={(s) => {
              setPageSize(s);
              setPage(1);
            }}
          />
        </div>
      </div>

      <Modal
        open={modal}
        title={editing ? 'Sửa người dùng' : 'Thêm người dùng'}
        onClose={() => setModal(false)}
      >
          <form onSubmit={onSave} className="space-y-3">
            <div>
              <label className="label">Họ tên</label>
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
              <label className="label">
                {editing ? 'Mật khẩu mới (để trống nếu giữ)' : 'Mật khẩu'}
              </label>
              <input
                className="input"
                type="password"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                required={!editing}
                minLength={editing ? undefined : 8}
              />
            </div>
            <div>
              <label className="label">SĐT</label>
              <input
                className="input"
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
              />
            </div>
            <div>
              <label className="label">Địa chỉ</label>
              <input
                className="input"
                value={form.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
              />
            </div>
            <div className="grid grid-cols-2 gap-3">
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
                  onChange={(e) => setForm({ ...form, gender: e.target.value })}
                >
                  <option value="">-- Chọn --</option>
                  <option value="Male">Nam</option>
                  <option value="Female">Nữ</option>
                  <option value="Other">Khác</option>
                </select>
              </div>
            </div>
            <div>
              <label className="label">Vai trò</label>
              <select
                className="input"
                value={form.role}
                onChange={(e) => setForm({ ...form, role: e.target.value as UserRole })}
              >
                {roles.map((r) => (
                  <option key={r} value={r}>
                    {r}
                  </option>
                ))}
              </select>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
              />
              Đang hoạt động
            </label>
            <div className="flex justify-end gap-2 pt-2">
              <Button type="button" variant="secondary" onClick={() => setModal(false)}>
                Hủy
              </Button>
              <Button type="submit" loading={saving}>
                Lưu
              </Button>
            </div>
          </form>
      </Modal>
    </div>
  );
}
