import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard, Package, Shield, Wrench, Users, Boxes, Tags, User, LogOut, Menu, X, Bell, Building2,
  Settings2, Eye, EyeOff, ChevronUp, ChevronDown, RotateCcw,
} from 'lucide-react';
import { useState } from 'react';
import { useAuthStore } from '../store/authStore';
import { useMenuPreferences } from '../hooks/useMenuPreferences';
import clsx from 'clsx';
import type { UserRole } from '../types';

const navItems: { to: string; label: string; icon: React.ReactNode; roles?: UserRole[] }[] = [
  { to: '/', label: 'Tổng quan', icon: <LayoutDashboard size={18} /> },
  { to: '/products', label: 'Sản phẩm', icon: <Package size={18} /> },
  { to: '/warranty-cards', label: 'Phiếu bảo hành', icon: <Shield size={18} /> },
  { to: '/repair-requests', label: 'Sửa chữa', icon: <Wrench size={18} /> },
  { to: '/parts', label: 'Linh kiện', icon: <Boxes size={18} />, roles: ['Admin', 'Manager', 'Technician', 'Receptionist'] },
  { to: '/categories', label: 'Danh mục', icon: <Tags size={18} />, roles: ['Admin', 'Manager'] },
  { to: '/service-centers', label: 'Trung tâm BH', icon: <Building2 size={18} />, roles: ['Admin', 'Manager', 'Receptionist'] },
  { to: '/users', label: 'Người dùng', icon: <Users size={18} />, roles: ['Admin', 'Manager'] },
  { to: '/profile', label: 'Hồ sơ', icon: <User size={18} /> },
];

const roleBadge: Record<string, string> = {
  Admin: 'badge-danger',
  Manager: 'badge-purple',
  Receptionist: 'badge-info',
  Technician: 'badge-warning',
  Customer: 'badge-success',
};

export default function AppLayout() {
  const { user, logout, hasRole } = useAuthStore();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [editMenu, setEditMenu] = useState(false);
  const { orderMap, isHidden, toggleHidden, move, reset } = useMenuPreferences();

  const handleLogout = () => { logout(); navigate('/login'); };
  const editable = navItems
    .filter((item) => !item.roles || (user && hasRole(...item.roles)))
    .sort((a, b) => (orderMap.get(a.to) ?? 99) - (orderMap.get(b.to) ?? 99));
  const editableIds = editable.map((item) => item.to);
  const visible = editable.filter((item) => !isHidden(item.to));

  return (
    <div className="min-h-screen flex bg-slate-100">
      <aside className={clsx(
        'fixed inset-y-0 left-0 z-40 w-[260px] bg-slate-900 text-white flex flex-col transform transition-transform duration-200 lg:translate-x-0 lg:sticky lg:top-0 lg:h-screen lg:shrink-0',
        open ? 'translate-x-0' : '-translate-x-full'
      )}>
        <div className="flex items-center gap-3 h-16 px-5 border-b border-slate-800">
          <div className="w-9 h-9 rounded-xl bg-blue-600 flex items-center justify-center shadow-lg shadow-blue-600/30">
            <Shield size={18} />
          </div>
          <div className="flex-1 min-w-0">
            <div className="font-bold text-sm tracking-tight">E-Warranty</div>
            <div className="text-[10px] text-slate-400 truncate">Quản lý bảo hành</div>
          </div>
          <button className="lg:hidden p-1 text-slate-400" onClick={() => setOpen(false)}><X size={18} /></button>
        </div>

        <nav className="flex-1 p-3 space-y-0.5 overflow-y-auto min-h-0">
          <div className="flex items-center justify-between px-3 py-2">
            <p className="text-[10px] font-semibold uppercase tracking-widest text-slate-500">Menu</p>
            <button
              type="button"
              title="Tùy chỉnh menu"
              aria-label="Tùy chỉnh menu"
              onClick={() => setEditMenu((value) => !value)}
              className="p-1 rounded-lg text-slate-500 hover:text-white hover:bg-slate-800 transition"
            >
              <Settings2 size={14} />
            </button>
          </div>

          {editMenu && (
            <div className="mb-2 rounded-xl border border-slate-700 bg-slate-800/80 p-2 space-y-1">
              <p className="px-1 text-[11px] text-slate-400">Ẩn/hiện · Sắp xếp</p>
              {editable.map((item, index) => {
                const hidden = isHidden(item.to);
                return (
                  <div
                    key={item.to}
                    className="flex items-center gap-1 rounded-lg px-2 py-1.5 text-sm text-slate-200 hover:bg-slate-700/60"
                  >
                    <span className="flex-1 min-w-0 truncate flex items-center gap-2 opacity-90">
                      {item.icon}
                      {item.label}
                    </span>
                    <button
                      type="button"
                      className="p-1 rounded hover:bg-slate-600 text-slate-400 disabled:opacity-30 disabled:cursor-not-allowed"
                      title="Lên"
                      aria-label={`Đưa ${item.label} lên`}
                      disabled={index === 0}
                      onClick={() => move(item.to, 'up', editableIds)}
                    >
                      <ChevronUp size={14} />
                    </button>
                    <button
                      type="button"
                      className="p-1 rounded hover:bg-slate-600 text-slate-400 disabled:opacity-30 disabled:cursor-not-allowed"
                      title="Xuống"
                      aria-label={`Đưa ${item.label} xuống`}
                      disabled={index === editable.length - 1}
                      onClick={() => move(item.to, 'down', editableIds)}
                    >
                      <ChevronDown size={14} />
                    </button>
                    <button
                      type="button"
                      className="p-1 rounded hover:bg-slate-600 text-slate-400"
                      title={hidden ? 'Hiện' : 'Ẩn'}
                      aria-label={`${hidden ? 'Hiện' : 'Ẩn'} ${item.label}`}
                      onClick={() => toggleHidden(item.to)}
                    >
                      {hidden ? <EyeOff size={14} /> : <Eye size={14} />}
                    </button>
                  </div>
                );
              })}
              <div className="flex gap-2 pt-1">
                <button
                  type="button"
                  onClick={reset}
                  className="flex-1 flex items-center justify-center gap-1 rounded-lg py-1.5 text-[11px] text-slate-300 hover:bg-slate-700"
                >
                  <RotateCcw size={12} /> Mặc định
                </button>
                <button
                  type="button"
                  onClick={() => setEditMenu(false)}
                  className="flex-1 rounded-lg py-1.5 text-[11px] bg-blue-600 text-white hover:bg-blue-500"
                >
                  Xong
                </button>
              </div>
            </div>
          )}

          {visible.map((item) => (
            <NavLink key={item.to} to={item.to} end={item.to === '/'} onClick={() => setOpen(false)}
              className={({ isActive }) => clsx(
                'flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition',
                isActive ? 'bg-blue-600 text-white shadow-md shadow-blue-600/25' : 'text-slate-300 hover:bg-slate-800 hover:text-white'
              )}>
              {item.icon}
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="mt-auto shrink-0 p-4 border-t border-slate-800">
          <div className="flex items-center gap-3 mb-3">
            <div className="w-9 h-9 rounded-full bg-gradient-to-br from-blue-500 to-violet-500 flex items-center justify-center text-sm font-bold">
              {user?.fullName?.charAt(0)?.toUpperCase() || 'U'}
            </div>
            <div className="flex-1 min-w-0">
              <div className="text-sm font-medium truncate">{user?.fullName}</div>
              <span className={clsx('mt-0.5', roleBadge[user?.role || ''] || 'badge-neutral')}>{user?.role}</span>
            </div>
          </div>
          <button onClick={handleLogout}
            className="w-full flex items-center justify-center gap-2 rounded-xl px-3 py-2 text-sm text-slate-300 hover:bg-slate-800 hover:text-white transition">
            <LogOut size={16} /> Đăng xuất
          </button>
        </div>
      </aside>

      {open && <div className="fixed inset-0 bg-black/40 z-30 lg:hidden backdrop-blur-sm" onClick={() => setOpen(false)} />}

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-14 bg-white/80 backdrop-blur border-b border-slate-200/80 flex items-center px-4 gap-3 lg:px-6 sticky top-0 z-20">
          <button className="lg:hidden p-2 rounded-xl hover:bg-slate-100" onClick={() => setOpen(true)}>
            <Menu size={20} />
          </button>
          <div className="flex-1">
            <p className="text-sm font-semibold text-slate-700 hidden sm:block">Hệ thống Quản lý Bảo hành Thiết bị Điện tử</p>
          </div>
          <button className="p-2 rounded-xl hover:bg-slate-100 text-slate-400 relative">
            <Bell size={18} />
          </button>
          <div className="hidden sm:flex items-center gap-2 text-sm text-slate-600">
            <span className="font-medium">{user?.fullName?.split(' ').pop()}</span>
          </div>
        </header>
        <main className="flex-1 p-4 lg:p-6 overflow-auto">
          <div className="max-w-7xl mx-auto animate-in">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
