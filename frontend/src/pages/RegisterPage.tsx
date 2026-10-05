import { FormEvent, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Shield, Eye, EyeOff, Check, X } from 'lucide-react';
import { api, getErrorMessage } from '../api/client';
import { Button, Input, Alert } from '../components/ui';

function passwordStrength(pw: string) {
  let score = 0;
  if (pw.length >= 8) score++;
  if (pw.length >= 12) score++;
  if (/[A-Z]/.test(pw)) score++;
  if (/[0-9]/.test(pw)) score++;
  if (/[^A-Za-z0-9]/.test(pw)) score++;
  return score;
}

const strengthLabel = ['Rất yếu', 'Yếu', 'Trung bình', 'Khá', 'Mạnh', 'Rất mạnh'];
const strengthColor = ['bg-red-400', 'bg-orange-400', 'bg-amber-400', 'bg-lime-400', 'bg-emerald-400', 'bg-emerald-600'];

export default function RegisterPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({ fullName: '', email: '', phone: '', password: '', confirmPassword: '' });
  const [showPass, setShowPass] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const [error, setError] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  const strength = useMemo(() => passwordStrength(form.password), [form.password]);
  const rules = useMemo(() => [
    { ok: form.password.length >= 8, text: 'Tối thiểu 8 ký tự' },
    { ok: /[A-Z]/.test(form.password), text: 'Có chữ hoa' },
    { ok: /[0-9]/.test(form.password), text: 'Có chữ số' },
    { ok: /[^A-Za-z0-9]/.test(form.password), text: 'Có ký tự đặc biệt' },
    { ok: form.confirmPassword.length > 0 && form.password === form.confirmPassword, text: 'Xác nhận mật khẩu khớp' },
  ], [form.password, form.confirmPassword]);

  const validate = () => {
    const errs: Record<string, string> = {};
    if (!form.fullName.trim()) errs.fullName = 'Vui lòng nhập họ tên';
    if (!form.email.trim()) errs.email = 'Vui lòng nhập email';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) errs.email = 'Email không hợp lệ';
    if (form.password.length < 8) errs.password = 'Mật khẩu tối thiểu 8 ký tự';
    if (form.password !== form.confirmPassword) errs.confirmPassword = 'Mật khẩu xác nhận không khớp';
    setFieldErrors(errs);
    return Object.keys(errs).length === 0;
  };

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    if (!validate()) return;
    setLoading(true);
    try {
      await api.post('/api/auth/register', {
        fullName: form.fullName.trim(),
        email: form.email.trim(),
        password: form.password,
        phone: form.phone.trim() || null,
      });
      navigate('/login');
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const set = (key: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm({ ...form, [key]: e.target.value });

  return (
    <div className="min-h-screen flex">
      <div className="hidden lg:flex lg:w-[42%] relative overflow-hidden bg-gradient-to-br from-slate-800 via-blue-900 to-blue-800 text-white">
        <div className="absolute inset-0 opacity-20"
          style={{ backgroundImage: 'radial-gradient(circle at 70% 60%, rgba(96,165,250,0.5), transparent 50%)' }} />
        <div className="relative z-10 flex flex-col justify-between p-12 w-full">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-xl bg-white/15 flex items-center justify-center"><Shield size={24} /></div>
            <span className="text-xl font-bold">E-Warranty</span>
          </div>
          <div>
            <h1 className="text-3xl font-bold leading-snug">Tạo tài khoản khách hàng</h1>
            <p className="mt-3 text-blue-100/80 text-sm leading-relaxed max-w-sm">
              Theo dõi phiếu bảo hành, gửi yêu cầu sửa chữa và nhận cập nhật tiến độ trực tuyến.
            </p>
            <ul className="mt-6 space-y-2.5 text-sm text-blue-100/90">
              {['Mật khẩu được mã hóa BCrypt', 'JWT Access + Refresh Token', 'Chỉ tạo được vai trò Customer'].map((t) => (
                <li key={t} className="flex items-center gap-2">
                  <Check size={14} className="text-emerald-300 shrink-0" /> {t}
                </li>
              ))}
            </ul>
          </div>
          <p className="text-xs text-blue-300/60">Đã có tài khoản? Đăng nhập ngay.</p>
        </div>
      </div>

      <div className="flex-1 flex items-center justify-center p-6 sm:p-10 bg-slate-50">
        <div className="w-full max-w-[440px] animate-in">
          <h2 className="text-2xl font-bold text-slate-900 tracking-tight">Đăng ký</h2>
          <p className="text-sm text-slate-500 mt-1.5 mb-6">Tạo tài khoản khách hàng mới</p>

          {error && <div className="mb-4"><Alert type="error">{error}</Alert></div>}

          <form onSubmit={onSubmit} className="space-y-4">
            <Input label="Họ và tên" value={form.fullName} onChange={set('fullName')} error={fieldErrors.fullName} placeholder="Nguyễn Văn A" required />
            <Input label="Email" type="email" value={form.email} onChange={set('email')} error={fieldErrors.email} placeholder="email@example.com" required />
            <Input label="Số điện thoại" value={form.phone} onChange={set('phone')} placeholder="09xxxxxxxx" hint="Không bắt buộc" />

            <div className="relative">
              <Input label="Mật khẩu" type={showPass ? 'text' : 'password'} value={form.password}
                onChange={set('password')} error={fieldErrors.password} placeholder="Tối thiểu 8 ký tự" required minLength={8} />
              <button type="button" tabIndex={-1} className="absolute right-3 top-[38px] text-slate-400 hover:text-slate-600"
                onClick={() => setShowPass(!showPass)}>
                {showPass ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>

            {form.password.length > 0 && (
              <div className="-mt-1 space-y-2">
                <div className="flex gap-1">
                  {[0, 1, 2, 3, 4].map((i) => (
                    <div key={i} className={`h-1.5 flex-1 rounded-full transition ${i < strength ? strengthColor[strength] : 'bg-slate-200'}`} />
                  ))}
                </div>
                <p className="text-xs text-slate-500">Độ mạnh: <span className="font-medium">{strengthLabel[strength]}</span></p>
                <ul className="grid grid-cols-1 gap-1">
                  {rules.map((r) => (
                    <li key={r.text} className={`flex items-center gap-1.5 text-xs ${r.ok ? 'text-emerald-600' : 'text-slate-400'}`}>
                      {r.ok ? <Check size={12} /> : <X size={12} />} {r.text}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            <div className="relative">
              <Input label="Xác nhận mật khẩu" type={showConfirm ? 'text' : 'password'} value={form.confirmPassword}
                onChange={set('confirmPassword')} error={fieldErrors.confirmPassword} placeholder="Nhập lại mật khẩu" required />
              <button type="button" tabIndex={-1} className="absolute right-3 top-[38px] text-slate-400 hover:text-slate-600"
                onClick={() => setShowConfirm(!showConfirm)}>
                {showConfirm ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>

            <Button type="submit" className="w-full btn-lg" loading={loading}>Tạo tài khoản</Button>
          </form>

          <p className="mt-6 text-center text-sm text-slate-500">
            Đã có tài khoản?{' '}
            <Link to="/login" className="font-semibold text-blue-600 hover:underline">Đăng nhập</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
