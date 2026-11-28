type Props = {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onPageChange: (page: number) => void;
  onPageSizeChange?: (size: number) => void;
};

export function Pagination({
  page,
  totalPages,
  totalCount,
  pageSize,
  onPageChange,
  onPageSizeChange,
}: Props) {
  if (totalCount <= 0) return null;

  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, totalCount);

  return (
    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-3 border-t border-slate-100">
      <p className="text-sm text-slate-500">
        Hiển thị <span className="font-medium text-slate-700">{from}–{to}</span> /{' '}
        <span className="font-medium text-slate-700">{totalCount}</span> bản ghi
      </p>
      <div className="flex items-center gap-2 flex-wrap">
        {onPageSizeChange && (
          <select
            className="input py-1.5 text-sm w-auto"
            value={pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
          >
            {[5, 10, 20, 50].map((n) => (
              <option key={n} value={n}>
                {n}/trang
              </option>
            ))}
          </select>
        )}
        <button
          type="button"
          className="btn-secondary py-1.5 px-3 text-sm disabled:opacity-40"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
        >
          ← Trước
        </button>
        <span className="text-sm text-slate-600 tabular-nums px-1">
          {page} / {Math.max(1, totalPages)}
        </span>
        <button
          type="button"
          className="btn-secondary py-1.5 px-3 text-sm disabled:opacity-40"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
        >
          Sau →
        </button>
      </div>
    </div>
  );
}

/** Normalize API list payload (array or PagedResult). */
export function parsePaged<T>(data: any, mapFn: (d: any) => T) {
  const raw = Array.isArray(data)
    ? data
    : data?.items ?? data?.Items ?? [];
  return {
    items: (raw as any[]).map(mapFn),
    page: data?.pageNumber ?? data?.PageNumber ?? 1,
    pageSize: data?.pageSize ?? data?.PageSize ?? 20,
    totalCount: data?.totalCount ?? data?.TotalCount ?? (Array.isArray(data) ? data.length : raw.length),
    totalPages: data?.totalPages ?? data?.TotalPages ?? 1,
  };
}
