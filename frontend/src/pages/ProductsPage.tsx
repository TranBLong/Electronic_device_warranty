import { FormEvent, useCallback, useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { Product } from '../types';
import { Plus, Pencil, Trash2, Search, Package } from 'lucide-react';
import { Button, Input, Alert, Modal, EmptyState } from '../components/ui';
import { Pagination, parsePaged } from '../components/Pagination';

function mapProduct(d: any): Product {
  return {
    id: d.id ?? d.Id,
    name: d.name ?? d.Name,
    brand: d.brand ?? d.Brand,
    model: d.model ?? d.Model,
    serialNumber: d.serialNumber ?? d.SerialNumber,
    warrantyMonths: d.warrantyMonths ?? d.WarrantyMonths,
    createdAt: d.createdAt ?? d.CreatedAt,
    categoryId: d.categoryId ?? d.CategoryId ?? null,
    categoryName: d.categoryName ?? d.CategoryName ?? null,
  };
}

const empty = {
  name: '',
  brand: '',
  model: '',
  serialNumber: '',
  warrantyMonths: 12,
  categoryId: 0 as number,
};

export default function ProductsPage() {
  const { hasRole } = useAuthStore();
  const canWrite = hasRole('Admin', 'Manager');

  const [items, setItems] = useState<Product[]>([]);
  const [keyword, setKeyword] = useState('');
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);

  const [modal, setModal] = useState(false);
  const [editing, setEditing] = useState<Product | null>(null);
  const [form, setForm] = useState(empty);
  const [categories, setCategories] = useState<{ id: number; name: string }[]>([]);
  const [saving, setSaving] = useState(false);

  const load = useCallback(
    async (p = page, size = pageSize) => {
      setLoading(true);
      try {
        const { data } = await api.get('/api/products', {
          params: {
            pageNumber: p,
            pageSize: size,
            keyword: keyword.trim() || undefined,
            sortBy: 'name',
            isDescending: false,
          },
        });
        const parsed = parsePaged(data, mapProduct);
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
    [keyword, page, pageSize]
  );

  const loadCategories = async () => {
    try {
      const cat = await api.get('/api/categories', {
        params: { pageNumber: 1, pageSize: 100 },
      });
      const craw = Array.isArray(cat.data)
        ? cat.data
        : cat.data?.items ?? cat.data?.Items ?? [];
      setCategories(
        craw.map((c: any) => ({ id: c.id ?? c.Id, name: c.name ?? c.Name }))
      );
    } catch {
      /* ignore */
    }
  };

  useEffect(() => {
    load(1, pageSize);
    loadCategories();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    load(page, pageSize);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize]);

  const searchNow = () => {
    setPage(1);
    load(1, pageSize);
  };

  const openCreate = () => {
    setEditing(null);
    setForm(empty);
    setModal(true);
  };

  const openEdit = (p: Product) => {
    setEditing(p);
    setForm({
      name: p.name,
      brand: p.brand,
      model: p.model,
      serialNumber: p.serialNumber,
      warrantyMonths: p.warrantyMonths,
      categoryId: p.categoryId || 0,
    });
    setModal(true);
  };

  const onSave = async (e?: FormEvent) => {
    e?.preventDefault();
    setError('');
    setSaving(true);
    try {
      const payload = {
        ...form,
        categoryId: form.categoryId ? form.categoryId : null,
      };
      if (editing) await api.put(`/api/products/${editing.id}`, payload);
      else await api.post('/api/products', payload);
      setModal(false);
      await load(page, pageSize);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  const onDelete = async (p: Product) => {
    if (!confirm(`Xóa sản phẩm "${p.name}" (${p.serialNumber})?`)) return;
    try {
      await api.delete(`/api/products/${p.id}`);
      await load(page, pageSize);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <div className="space-y-5">
      <div className="rounded-2xl bg-gradient-to-r from-emerald-600 via-teal-600 to-cyan-600 p-6 text-white shadow-lg">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <div className="rounded-xl bg-white/20 p-3">
              <Package size={28} />
            </div>
            <div>
              <h2 className="text-2xl font-bold">Sản phẩm</h2>
              <p className="text-emerald-50 text-sm mt-0.5">
                {totalCount > 0
                  ? `${totalCount.toLocaleString()} thiết bị · phân trang server`
                  : 'Catalog thiết bị trong hệ thống bảo hành'}
              </p>
            </div>
          </div>
          {canWrite && (
            <Button
              className="!bg-white !text-emerald-800 hover:!bg-emerald-50"
              icon={<Plus size={16} />}
              onClick={openCreate}
            >
              Thêm sản phẩm
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
              placeholder="Tìm tên, hãng, serial…"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && searchNow()}
            />
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
              <th>Tên</th>
              <th>Hãng</th>
              <th>Model</th>
              <th>Serial</th>
              <th>BH (tháng)</th>
              <th>Danh mục</th>
              {canWrite && <th className="text-right">Thao tác</th>}
            </tr>
          </thead>
          <tbody>
            {items.map((p) => (
              <tr key={p.id}>
                <td className="font-medium">{p.name}</td>
                <td>{p.brand}</td>
                <td>{p.model}</td>
                <td className="font-mono text-xs">{p.serialNumber}</td>
                <td>{p.warrantyMonths}</td>
                <td className="text-slate-500 text-sm">{p.categoryName || '—'}</td>
                {canWrite && (
                  <td className="text-right space-x-1">
                    <button className="btn-ghost btn-sm" onClick={() => openEdit(p)} title="Sửa">
                      <Pencil size={14} />
                    </button>
                    <button
                      className="btn-ghost btn-sm text-red-600"
                      onClick={() => onDelete(p)}
                      title="Xóa"
                    >
                      <Trash2 size={14} />
                    </button>
                  </td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
        {items.length === 0 && !loading && (
          <EmptyState title="Không có sản phẩm" description="Thêm sản phẩm hoặc đổi bộ lọc." />
        )}
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
        onClose={() => setModal(false)}
        title={editing ? 'Sửa sản phẩm' : 'Thêm sản phẩm'}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModal(false)}>
              Hủy
            </Button>
            <Button loading={saving} onClick={() => onSave()}>
              Lưu
            </Button>
          </>
        }
      >
        <form onSubmit={onSave} className="space-y-3">
          <Input
            label="Tên"
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
            required
          />
          <Input
            label="Hãng"
            value={form.brand}
            onChange={(e) => setForm({ ...form, brand: e.target.value })}
            required
          />
          <Input
            label="Model"
            value={form.model}
            onChange={(e) => setForm({ ...form, model: e.target.value })}
            required
          />
          <Input
            label="Serial"
            value={form.serialNumber}
            onChange={(e) => setForm({ ...form, serialNumber: e.target.value })}
            required
          />
          <Input
            label="Tháng BH"
            type="number"
            min={1}
            value={form.warrantyMonths}
            onChange={(e) => setForm({ ...form, warrantyMonths: +e.target.value })}
            required
          />
          <div>
            <label className="label">Danh mục</label>
            <select
              className="input"
              value={form.categoryId}
              onChange={(e) => setForm({ ...form, categoryId: +e.target.value })}
            >
              <option value={0}>— Không chọn —</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>
        </form>
      </Modal>
    </div>
  );
}
