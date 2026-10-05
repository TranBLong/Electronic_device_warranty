import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { Product } from '../types';
import { Plus, Search } from 'lucide-react';

function mapProduct(d: any): Product {
  return {
    id: d.id ?? d.Id,
    name: d.name ?? d.Name,
    brand: d.brand ?? d.Brand,
    model: d.model ?? d.Model,
    serialNumber: d.serialNumber ?? d.SerialNumber,
    warrantyMonths: d.warrantyMonths ?? d.WarrantyMonths,
    createdAt: d.createdAt ?? d.CreatedAt,
  };
}

export default function ProductsPage() {
  const { hasRole } = useAuthStore();
  const canWrite = hasRole('Admin');
  const [items, setItems] = useState<Product[]>([]);
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ name: '', brand: '', model: '', serialNumber: '', warrantyMonths: 12 });

  const load = async () => {
    try {
      const { data } = await api.get('/api/products');
      setItems((Array.isArray(data) ? data : []).map(mapProduct));
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  useEffect(() => {
    load();
  }, []);

  const filtered = items.filter(
    (p) =>
      !search ||
      p.name.toLowerCase().includes(search.toLowerCase()) ||
      p.brand.toLowerCase().includes(search.toLowerCase()) ||
      p.serialNumber.toLowerCase().includes(search.toLowerCase())
  );

  const onCreate = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await api.post('/api/products', form);
      setShowForm(false);
      setForm({ name: '', brand: '', model: '', serialNumber: '', warrantyMonths: 12 });
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <h2 className="text-xl font-bold text-slate-800">Sản phẩm</h2>
        {canWrite && (
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
            <Plus size={16} /> Thêm sản phẩm
          </button>
        )}
      </div>

      {error && <div className="rounded-lg bg-red-50 text-red-700 text-sm px-3 py-2">{error}</div>}

      {showForm && (
        <form onSubmit={onCreate} className="card p-4 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
          <div>
            <label className="label">Tên</label>
            <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          </div>
          <div>
            <label className="label">Hãng</label>
            <input className="input" value={form.brand} onChange={(e) => setForm({ ...form, brand: e.target.value })} required />
          </div>
          <div>
            <label className="label">Model</label>
            <input className="input" value={form.model} onChange={(e) => setForm({ ...form, model: e.target.value })} required />
          </div>
          <div>
            <label className="label">Serial</label>
            <input className="input" value={form.serialNumber} onChange={(e) => setForm({ ...form, serialNumber: e.target.value })} required />
          </div>
          <div>
            <label className="label">Tháng BH</label>
            <input className="input" type="number" min={1} value={form.warrantyMonths} onChange={(e) => setForm({ ...form, warrantyMonths: +e.target.value })} required />
          </div>
          <div className="flex items-end gap-2">
            <button type="submit" className="btn-primary">Lưu</button>
            <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Hủy</button>
          </div>
        </form>
      )}

      <div className="relative">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
        <input className="input pl-9" placeholder="Tìm theo tên, hãng, serial..." value={search} onChange={(e) => setSearch(e.target.value)} />
      </div>

      <div className="card overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-slate-600 border-b">
            <tr>
              <th className="text-left px-4 py-3 font-medium">Tên</th>
              <th className="text-left px-4 py-3 font-medium">Hãng</th>
              <th className="text-left px-4 py-3 font-medium">Model</th>
              <th className="text-left px-4 py-3 font-medium">Serial</th>
              <th className="text-left px-4 py-3 font-medium">BH (tháng)</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {filtered.map((p) => (
              <tr key={p.id} className="hover:bg-slate-50">
                <td className="px-4 py-3 font-medium">{p.name}</td>
                <td className="px-4 py-3">{p.brand}</td>
                <td className="px-4 py-3">{p.model}</td>
                <td className="px-4 py-3 font-mono text-xs">{p.serialNumber}</td>
                <td className="px-4 py-3">{p.warrantyMonths}</td>
              </tr>
            ))}
            {filtered.length === 0 && (
              <tr>
                <td colSpan={5} className="px-4 py-8 text-center text-slate-400">Không có dữ liệu</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
