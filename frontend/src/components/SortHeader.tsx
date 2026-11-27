import type { SortDir } from '../hooks/useSort';

export function SortHeader({
  label,
  column,
  sortKey,
  sortDir,
  onToggle,
}: {
  label: string;
  column: string;
  sortKey: string;
  sortDir: SortDir;
  onToggle: (key: string) => void;
}) {
  const active = sortKey === column;
  return (
    <th
      className="text-left px-4 py-3 font-semibold text-xs uppercase tracking-wider cursor-pointer select-none hover:text-blue-600"
      onClick={() => onToggle(column)}
    >
      <span className="inline-flex items-center gap-1">
        {label}
        <span className="text-[10px] text-slate-400">
          {active ? (sortDir === 'asc' ? '▲' : '▼') : '⇅'}
        </span>
      </span>
    </th>
  );
}
