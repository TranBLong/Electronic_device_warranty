import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import type { Category } from '../types';
import { Plus, Pencil, Trash2, FolderOpen, Search } from 'lucide-react';
import { useAuthStore } from '../store/authStore';
import { Pagination, parsePaged } from '../components/Pagination';

function mapCat(d: any): Category {
  return {
    id: d.id ?? d.Id,
    name: d.name ?? d.Name,
    description: d.description ?? d.Description,
    isActive: d.isActive ?? d.IsActive,
    createdAt: d.createdAt ?? d.CreatedAt,
  };
}

export default function CategoriesPage() {
  const { hasRole } = useAuthStore();
  const canWrite = hasRole('Admin', 'Manager');
  const [items, setItems] = useState<Category[]>([]);
  const [error, setError] = useState('');
  const [keyword, setKeyword] = useState('');
  const [appliedKeyword, setAppliedKeyword] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [sortBy, setSortBy] = useState('name');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<Category | null>(null);
  const [form, setForm] = useState({ name: '', description: '' });
  const [saving, setSaving] = useState(false);

  const load = async (p = page, size = pageSize) => {
    try {
      const { data } = await api.get('/api/categories', {
        params: {
          keyword: appliedKeyword || undefined,
          pageNumber: p,
          pageSize: size,
          sortBy,
          sortDir,
        },
      });
      const parsed = parsePaged(data, mapCat);
      setItems(parsed.items);
      setTotalPages(parsed.totalPages);
      setTotalCount(parsed.totalCount);
      setPage(parsed.page);
      setPageSize(parsed.pageSize);
      setError('');
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  useEffect(() => {
    load(page, pageSize);
  }, [page, pageSize, appliedKeyword, sortBy, sortDir]);

  const toggleSortDirection = () => {
    setSortDir((direction) => direction === 'asc' ? 'desc' : 'asc');
    setPage(1);
  };

  const openCreate = () => {
    setEditing(null);
    setForm({ name: '', description: '' });
    setModal(true);
  };

  const openEdit = (c: Category) => {
    setEditing(c);
    setForm({ name: c.name, description: c.description || '' });
    setModal(true);
  };

  const onSave = async (e: FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError('');
    try {
      if (editing) await api.put(`/api/categories/${editing.id}`, form);
      else await api.post('/api/categories', form);
      setModal(false);
      await load(page);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const onDelete = async (c: Category) => {
    if (!confirm(`Ngừng dùng danh mục "${c.name}"?`)) return;
    try {
      await api.delete(`/api/categories/${c.id}`);
      await load(page);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div className="space-y-5">
      {/* Hero band – unique for categories */}
      <div className="rounded-2xl bg-gradient-to-br from-amber-500 via-orange-500 to-rose-500 p-6 text-white shadow-lg">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="flex items-start gap-3">
            <div className="rounded-xl bg-white/20 p-3 backdrop-blur">
              <FolderOpen size={28} />
            </div>
            <div>
              <h2 className="text-2xl font-bold tracking-tight">Danh mục sản phẩm</h2>
              <p className="text-amber-50/90 text-sm mt-1">
                Phân loại catalog — Laptop, Phone, Tablet…
              </p>
            </div>
          </div>
          {canWrite && (
            <button
              className="inline-flex items-center gap-2 rounded-xl bg-white text-orange-700 font-semibold px-4 py-2.5 shadow hover:bg-amber-50 transition"
              onClick={openCreate}
            >
              <Plus size={18} /> Thêm danh mục
            </button>
          )}
        </div>
      </div>

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 text-red-700 text-sm px-4 py-3">{error}</div>
      )}

      <div className="card p-4">
        <div className="flex flex-col sm:flex-row sm:items-end gap-3">
          <div className="relative flex-1 min-w-0">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <input
              className="input pl-9"
              placeholder="Tìm danh mục…"
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
          <button className="btn-secondary w-full sm:w-auto shrink-0" onClick={() => { setAppliedKeyword(keyword.trim()); setPage(1); }}>
            Tìm
          </button>
          <select
            className="input w-full sm:w-auto shrink-0"
            aria-label="Sắp xếp danh mục"
            value={sortBy}
            onChange={(e) => { setSortBy(e.target.value); setPage(1); }}
          >
            <option value="name">Tên</option>
            <option value="createdAt">Ngày tạo</option>
            <option value="id">ID</option>
          </select>
        </div>
        <button className="btn-secondary" onClick={toggleSortDirection}>
          {sortDir === 'asc' ? 'Tăng dần' : 'Giảm dần'}
        </button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        {items.map((c) => (
          <div
            key={c.id}
            className="group relative overflow-hidden rounded-2xl border border-orange-100 bg-white p-5 shadow-sm hover:shadow-md transition"
          >
            <div className="absolute top-0 right-0 h-16 w-16 rounded-bl-full bg-gradient-to-bl from-amber-100 to-transparent" />
            <h3 className="font-bold text-slate-800 text-lg">{c.name}</h3>
            <p className="text-sm text-slate-500 mt-2 line-clamp-2 min-h-[2.5rem]">
              {c.description || 'Chưa có mô tả'}
            </p>
            {canWrite && (
              <div className="mt-4 flex gap-2">
                <button
                  className="inline-flex items-center gap-1 text-sm text-orange-700 hover:underline"
                  onClick={() => openEdit(c)}
                >
                  <Pencil size={14} /> Sửa
                </button>
                <button
                  className="inline-flex items-center gap-1 text-sm text-red-600 hover:underline"
                  onClick={() => onDelete(c)}
                >
                  <Trash2 size={14} /> Xóa
                </button>
              </div>
            )}
          </div>
        ))}
        {items.length === 0 && (
          <div className="col-span-full rounded-2xl border border-dashed border-orange-200 bg-orange-50/50 p-12 text-center text-slate-500">
            Không có danh mục nào
          </div>
        )}
      </div>

      <Pagination
        page={page}
        totalPages={totalPages}
        totalCount={totalCount}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={(size) => { setPageSize(size); setPage(1); }}
      />

      {modal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <form
            onSubmit={onSave}
            className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl space-y-4"
          >
            <h3 className="text-lg font-bold text-slate-800">
              {editing ? 'Sửa danh mục' : 'Thêm danh mục'}
            </h3>
            <div>
              <label className="label">Tên</label>
              <input
                className="input"
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                required
              />
            </div>
            <div>
              <label className="label">Mô tả</label>
              <textarea
                className="input min-h-[80px]"
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
              />
            </div>
            <div className="flex gap-2 justify-end">
              <button type="button" className="btn-secondary" onClick={() => setModal(false)}>
                Hủy
              </button>
              <button type="submit" className="btn-primary" disabled={saving}>
                {saving ? 'Đang lưu…' : 'Lưu'}
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
