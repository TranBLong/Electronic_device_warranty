import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { Part } from '../types';
import { Plus } from 'lucide-react';

function mapPart(d: any): Part {
  return {
    id: d.id ?? d.Id,
    code: d.code ?? d.Code,
    name: d.name ?? d.Name,
    description: d.description ?? d.Description,
    unitPrice: d.unitPrice ?? d.UnitPrice,
    stockQuantity: d.stockQuantity ?? d.StockQuantity,
    isActive: d.isActive ?? d.IsActive,
    createdAt: d.createdAt ?? d.CreatedAt,
  };
}

export default function PartsPage() {
  const { hasRole } = useAuthStore();
  const canWrite = hasRole('Admin', 'Manager');
  const [items, setItems] = useState<Part[]>([]);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ code: '', name: '', description: '', unitPrice: 0, stockQuantity: 0 });

  const load = async () => {
    try {
      const { data } = await api.get('/api/parts');
      setItems((Array.isArray(data) ? data : []).map(mapPart));
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
      await api.post('/api/parts', form);
      setShowForm(false);
      setForm({ code: '', name: '', description: '', unitPrice: 0, stockQuantity: 0 });
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <h2 className="text-xl font-bold text-slate-800">Linh kiện</h2>
        {canWrite && (
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
            <Plus size={16} /> Thêm linh kiện
          </button>
        )}
      </div>

      {error && <div className="rounded-lg bg-red-50 text-red-700 text-sm px-3 py-2">{error}</div>}

      {showForm && (
        <form onSubmit={onCreate} className="card p-4 grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="label">Mã</label>
            <input className="input" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} required />
          </div>
          <div>
            <label className="label">Tên</label>
            <input className="input" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
          </div>
          <div>
            <label className="label">Đơn giá (VNĐ)</label>
            <input className="input" type="number" min={0} value={form.unitPrice} onChange={(e) => setForm({ ...form, unitPrice: +e.target.value })} />
          </div>
          <div>
            <label className="label">Tồn kho</label>
            <input className="input" type="number" min={0} value={form.stockQuantity} onChange={(e) => setForm({ ...form, stockQuantity: +e.target.value })} />
          </div>
          <div className="sm:col-span-2">
            <label className="label">Mô tả</label>
            <input className="input" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
          </div>
          <div className="flex gap-2">
            <button type="submit" className="btn-primary">Lưu</button>
            <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Hủy</button>
          </div>
        </form>
      )}

      <div className="card overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-slate-600 border-b">
            <tr>
              <th className="text-left px-4 py-3 font-medium">Mã</th>
              <th className="text-left px-4 py-3 font-medium">Tên</th>
              <th className="text-left px-4 py-3 font-medium">Đơn giá</th>
              <th className="text-left px-4 py-3 font-medium">Tồn kho</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {items.map((p) => (
              <tr key={p.id} className="hover:bg-slate-50">
                <td className="px-4 py-3 font-mono text-xs">{p.code}</td>
                <td className="px-4 py-3 font-medium">{p.name}</td>
                <td className="px-4 py-3">{Number(p.unitPrice).toLocaleString('vi-VN')} ₫</td>
                <td className="px-4 py-3">{p.stockQuantity}</td>
              </tr>
            ))}
            {items.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-slate-400">Không có dữ liệu</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
