import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import type { Category } from '../types';
import { Plus } from 'lucide-react';

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
  const [items, setItems] = useState<Category[]>([]);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', description: '' });

  const load = async () => {
    try {
      const { data } = await api.get('/api/categories');
      setItems((Array.isArray(data) ? data : []).map(mapCat));
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
      await api.post('/api/categories', form);
      setShowForm(false);
      setForm({ name: '', description: '' });
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <h2 className="text-xl font-bold text-slate-800">Danh mục sản phẩm</h2>
        <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
          <Plus size={16} /> Thêm danh mục
        </button>
      </div>

      {error && <div className="rounded-lg bg-red-50 text-red-700 text-sm px-3 py-2">{error}</div>}

      {showForm && (
        <form onSubmit={onCreate} className="card p-4 space-y-3 max-w-md">
          <div>
            <label className="label">Tên</label>
            <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          </div>
          <div>
            <label className="label">Mô tả</label>
            <input className="input" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          </div>
          <div className="flex gap-2">
            <button type="submit" className="btn-primary">Lưu</button>
            <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Hủy</button>
          </div>
        </form>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
        {items.map((c) => (
          <div key={c.id} className="card p-4">
            <h3 className="font-semibold text-slate-800">{c.name}</h3>
            <p className="text-sm text-slate-500 mt-1">{c.description || '—'}</p>
          </div>
        ))}
        {items.length === 0 && <div className="card p-8 text-center text-slate-400 sm:col-span-2 lg:col-span-3">Không có danh mục</div>}
      </div>
    </div>
  );
}
