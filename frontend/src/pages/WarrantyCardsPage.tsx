import { FormEvent, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { WarrantyCard, Product, User } from '../types';
import { Plus } from 'lucide-react';
import { format } from 'date-fns';

function mapCard(d: any): WarrantyCard {
  return {
    id: d.id ?? d.Id,
    productId: d.productId ?? d.ProductId,
    customerId: d.customerId ?? d.CustomerId,
    customerName: d.customerName ?? d.CustomerName,
    productName: d.productName ?? d.ProductName,
    productBrand: d.productBrand ?? d.ProductBrand,
    productSerial: d.productSerial ?? d.ProductSerial ?? d.serialNumber,
    startDate: d.startDate ?? d.StartDate,
    endDate: d.endDate ?? d.EndDate,
    status: d.status ?? d.Status,
  };
}

const statusColor: Record<string, string> = {
  Active: 'bg-emerald-100 text-emerald-800',
  Expired: 'bg-slate-100 text-slate-600',
  Voided: 'bg-red-100 text-red-800',
};

export default function WarrantyCardsPage() {
  const { hasRole } = useAuthStore();
  const canCreate = hasRole('Admin', 'Receptionist');
  const [items, setItems] = useState<WarrantyCard[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [customers, setCustomers] = useState<User[]>([]);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState({ productId: 0, customerId: 0, startDate: '', warrantyMonths: 12 });

  const load = async () => {
    try {
      const [cardsRes, prodRes] = await Promise.all([
        api.get('/api/warranty-cards'),
        api.get('/api/products'),
      ]);
      setItems((Array.isArray(cardsRes.data) ? cardsRes.data : []).map(mapCard));
      setProducts(
        (Array.isArray(prodRes.data) ? prodRes.data : []).map((d: any) => ({
          id: d.id ?? d.Id,
          name: d.name ?? d.Name,
          brand: d.brand ?? d.Brand,
          model: d.model ?? d.Model,
          serialNumber: d.serialNumber ?? d.SerialNumber,
          warrantyMonths: d.warrantyMonths ?? d.WarrantyMonths,
          createdAt: d.createdAt ?? d.CreatedAt,
        }))
      );
      if (canCreate) {
        try {
          const custRes = await api.get('/api/users/customers');
          setCustomers(
            (Array.isArray(custRes.data) ? custRes.data : []).map((d: any) => ({
              id: d.id ?? d.Id,
              fullName: d.fullName ?? d.FullName,
              email: d.email ?? d.Email,
              phone: d.phone ?? d.Phone,
              role: d.role ?? d.Role,
              isActive: d.isActive ?? d.IsActive,
              createdAt: d.createdAt ?? d.CreatedAt,
            }))
          );
        } catch {
          /* ignore if no permission */
        }
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
      const start = form.startDate || new Date().toISOString().slice(0, 10);
      const end = new Date(start);
      end.setMonth(end.getMonth() + form.warrantyMonths);
      await api.post('/api/warranty-cards', {
        productId: form.productId,
        customerId: form.customerId,
        startDate: start,
        endDate: end.toISOString().slice(0, 10),
      });
      setShowForm(false);
      await load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <h2 className="text-xl font-bold text-slate-800">Phiếu bảo hành</h2>
        {canCreate && (
          <button className="btn-primary" onClick={() => setShowForm(!showForm)}>
            <Plus size={16} /> Tạo phiếu BH
          </button>
        )}
      </div>

      {error && <div className="rounded-lg bg-red-50 text-red-700 text-sm px-3 py-2">{error}</div>}

      {showForm && (
        <form onSubmit={onCreate} className="card p-4 grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label className="label">Sản phẩm</label>
            <select className="input" value={form.productId} onChange={(e) => setForm({ ...form, productId: +e.target.value })} required>
              <option value={0}>-- Chọn --</option>
              {products.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name} ({p.serialNumber})
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="label">Khách hàng</label>
            <select className="input" value={form.customerId} onChange={(e) => setForm({ ...form, customerId: +e.target.value })} required>
              <option value={0}>-- Chọn --</option>
              {customers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.fullName} ({c.email})
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="label">Ngày bắt đầu</label>
            <input className="input" type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} />
          </div>
          <div>
            <label className="label">Số tháng BH</label>
            <input className="input" type="number" min={1} value={form.warrantyMonths} onChange={(e) => setForm({ ...form, warrantyMonths: +e.target.value })} />
          </div>
          <div className="sm:col-span-2 flex gap-2">
            <button type="submit" className="btn-primary">Lưu</button>
            <button type="button" className="btn-secondary" onClick={() => setShowForm(false)}>Hủy</button>
          </div>
        </form>
      )}

      <div className="card overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-slate-50 text-slate-600 border-b">
            <tr>
              <th className="text-left px-4 py-3 font-medium">ID</th>
              <th className="text-left px-4 py-3 font-medium">Sản phẩm</th>
              <th className="text-left px-4 py-3 font-medium">Khách hàng</th>
              <th className="text-left px-4 py-3 font-medium">Bắt đầu</th>
              <th className="text-left px-4 py-3 font-medium">Kết thúc</th>
              <th className="text-left px-4 py-3 font-medium">Trạng thái</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {items.map((c) => (
              <tr key={c.id} className="hover:bg-slate-50">
                <td className="px-4 py-3">#{c.id}</td>
                <td className="px-4 py-3">{c.productName || `SP #${c.productId}`}</td>
                <td className="px-4 py-3">{c.customerName || `KH #${c.customerId}`}</td>
                <td className="px-4 py-3">{c.startDate ? format(new Date(c.startDate), 'dd/MM/yyyy') : '-'}</td>
                <td className="px-4 py-3">{c.endDate ? format(new Date(c.endDate), 'dd/MM/yyyy') : '-'}</td>
                <td className="px-4 py-3">
                  <span className={`badge ${statusColor[c.status] || 'bg-slate-100'}`}>{c.status}</span>
                </td>
              </tr>
            ))}
            {items.length === 0 && (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-slate-400">Không có dữ liệu</td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
