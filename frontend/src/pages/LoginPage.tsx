import { FormEvent, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Shield, Eye, EyeOff, Zap } from 'lucide-react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { AuthResponse } from '../types';
import { Button, Input, Alert } from '../components/ui';

const DEMOS = [
  { role: 'Admin', email: 'admin@warranty.local', pass: 'Admin@123', color: 'bg-red-100 text-red-700' },
  { role: 'Manager', email: 'manager@warranty.local', pass: 'Manager@123', color: 'bg-violet-100 text-violet-700' },
  { role: 'Receptionist', email: 'reception@warranty.local', pass: 'Receptionist@123', color: 'bg-blue-100 text-blue-700' },
  { role: 'Technician', email: 'tech01@warranty.local', pass: 'Technician@123', color: 'bg-amber-100 text-amber-700' },
  { role: 'Customer', email: 'customer01@warranty.local', pass: 'Customer@123', color: 'bg-emerald-100 text-emerald-700' },
];

export default function LoginPage() {
  const navigate = useNavigate();
  const { setTokens, fetchMe } = useAuthStore();
  const [email, setEmail] = useState('admin@warranty.local');
  const [password, setPassword] = useState('Admin@123');
  const [showPass, setShowPass] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const { data } = await api.post<AuthResponse>('/api/auth/login', { email, password });
      const d = data as any;
      setTokens(
        d.accessToken ?? d.AccessToken,
        d.refreshToken ?? d.RefreshToken,
        d.accessTokenExpiresAt ?? d.AccessTokenExpiresAt,
        d.refreshTokenExpiresAt ?? d.RefreshTokenExpiresAt
      );
      await fetchMe();
      navigate('/');
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex">
      <div className="hidden lg:flex lg:w-[48%] relative overflow-hidden bg-gradient-to-br from-blue-700 via-blue-800 to-slate-900 text-white">
        <div className="absolute inset-0 opacity-30"
          style={{ backgroundImage: 'radial-gradient(circle at 20% 50%, rgba(96,165,250,0.4), transparent 50%), radial-gradient(circle at 80% 20%, rgba(167,139,250,0.3), transparent 40%)' }}
        />
        <div className="relative z-10 flex flex-col justify-between p-12 w-full">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-xl bg-white/15 backdrop-blur flex items-center justify-center">
              <Shield size={24} />
            </div>
            <span className="text-xl font-bold tracking-tight">E-Warranty</span>
          </div>
          <div>
            <h1 className="text-4xl font-bold leading-tight tracking-tight">
              Hệ thống quản lý<br />bảo hành chuyên nghiệp
            </h1>
            <p className="mt-4 text-blue-100/90 text-base max-w-md leading-relaxed">
              Theo dõi phiếu bảo hành, phân công sửa chữa, quản lý linh kiện và báo cáo — tất cả trên một nền tảng.
            </p>
            <div className="mt-8 flex gap-6">
              {[
                { n: '5', l: 'Vai trò' },
                { n: '100%', l: 'JWT bảo mật' },
                { n: 'BCrypt', l: 'Hash mật khẩu' },
              ].map((s) => (
                <div key={s.l}>
                  <div className="text-2xl font-bold">{s.n}</div>
                  <div className="text-xs text-blue-200 mt-0.5">{s.l}</div>
                </div>
              ))}
            </div>
          </div>
          <p className="text-xs text-blue-300/70">© 2026 E-Warranty System · ASP.NET Core + React</p>
        </div>
      </div>

      <div className="flex-1 flex items-center justify-center p-6 sm:p-10 bg-slate-50">
        <div className="w-full max-w-[420px] animate-in">
          <div className="lg:hidden flex items-center gap-2.5 mb-8">
            <div className="w-10 h-10 rounded-xl bg-blue-600 text-white flex items-center justify-center">
              <Shield size={20} />
            </div>
            <span className="text-lg font-bold text-slate-900">E-Warranty</span>
          </div>

          <h2 className="text-2xl font-bold text-slate-900 tracking-tight">Đăng nhập</h2>
          <p className="text-sm text-slate-500 mt-1.5 mb-7">Nhập thông tin tài khoản để tiếp tục</p>

          {error && <div className="mb-5"><Alert type="error">{error}</Alert></div>}

          <form onSubmit={onSubmit} className="space-y-4">
            <Input label="Email" type="email" autoComplete="email" value={email}
              onChange={(e) => setEmail(e.target.value)} placeholder="you@example.com" required />
            <div className="relative">
              <Input label="Mật khẩu" type={showPass ? 'text' : 'password'} autoComplete="current-password"
                value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" required />
              <button type="button" tabIndex={-1}
                className="absolute right-3 top-[38px] text-slate-400 hover:text-slate-600"
                onClick={() => setShowPass(!showPass)}>
                {showPass ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
            <Button type="submit" className="w-full btn-lg" loading={loading}>Đăng nhập</Button>
          </form>

          <p className="mt-6 text-center text-sm text-slate-500">
            Chưa có tài khoản?{' '}
            <Link to="/register" className="font-semibold text-blue-600 hover:text-blue-700 hover:underline">
              Đăng ký khách hàng
            </Link>
          </p>

          <div className="mt-8 pt-6 border-t border-slate-200">
            <div className="flex items-center gap-2 text-xs font-medium text-slate-400 mb-3">
              <Zap size={12} /> Tài khoản demo — click để điền
            </div>
            <div className="flex flex-wrap gap-2">
              {DEMOS.map((d) => (
                <button key={d.role} type="button"
                  onClick={() => { setEmail(d.email); setPassword(d.pass); setError(''); }}
                  className={`text-xs font-semibold px-2.5 py-1.5 rounded-lg transition hover:opacity-80 ${d.color}`}>
                  {d.role}
                </button>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
