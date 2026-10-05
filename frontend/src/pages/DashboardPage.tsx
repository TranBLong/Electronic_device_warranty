import { useEffect, useState } from 'react';
import { api, getErrorMessage } from '../api/client';
import { useAuthStore } from '../store/authStore';
import type { DashboardSummary } from '../types';
import { Shield, Wrench, CheckCircle, Clock, Inbox } from 'lucide-react';

function StatCard({ title, value, icon, color }: { title: string; value: number; icon: React.ReactNode; color: string }) {
  return (
    <div className="card p-5 flex items-start gap-4">
      <div className={`rounded-lg p-3 ${color}`}>{icon}</div>
      <div>
        <p className="text-sm text-slate-500">{title}</p>
        <p className="text-2xl font-bold text-slate-800 mt-0.5">{value}</p>
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const { user, hasRole } = useAuthStore();
  const [summary, setSummary] = useState<DashboardSummary | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!hasRole('Admin', 'Manager')) return;
    api
      .get('/api/reports/summary')
      .then((res) => {
        const d = res.data as any;
        setSummary({
          totalWarrantyCards: d.totalWarrantyCards ?? d.TotalWarrantyCards ?? 0,
          activeWarrantyCards: d.activeWarrantyCards ?? d.ActiveWarrantyCards ?? 0,
          totalRepairRequests: d.totalRepairRequests ?? d.TotalRepairRequests ?? 0,
          receivedRepairRequests: d.receivedRepairRequests ?? d.ReceivedRepairRequests ?? 0,
          inProgressRepairRequests: d.inProgressRepairRequests ?? d.InProgressRepairRequests ?? 0,
          completedRepairRequests: d.completedRepairRequests ?? d.CompletedRepairRequests ?? 0,
        });
      })
      .catch((err) => setError(getErrorMessage(err)));
  }, [hasRole]);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold text-slate-800">Xin chào, {user?.fullName}</h2>
        <p className="text-sm text-slate-500 mt-1">
          Vai trò: <span className="font-medium text-brand-700">{user?.role}</span>
        </p>
      </div>

      {error && <div className="rounded-lg bg-red-50 text-red-700 text-sm px-3 py-2">{error}</div>}

      {hasRole('Admin', 'Manager') && summary && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          <StatCard title="Tổng phiếu bảo hành" value={summary.totalWarrantyCards} icon={<Shield size={22} className="text-blue-600" />} color="bg-blue-50" />
          <StatCard title="BH còn hiệu lực" value={summary.activeWarrantyCards} icon={<CheckCircle size={22} className="text-emerald-600" />} color="bg-emerald-50" />
          <StatCard title="Tổng yêu cầu SC" value={summary.totalRepairRequests} icon={<Wrench size={22} className="text-violet-600" />} color="bg-violet-50" />
          <StatCard title="Mới tiếp nhận" value={summary.receivedRepairRequests} icon={<Inbox size={22} className="text-amber-600" />} color="bg-amber-50" />
          <StatCard title="Đang sửa chữa" value={summary.inProgressRepairRequests} icon={<Clock size={22} className="text-orange-600" />} color="bg-orange-50" />
          <StatCard title="Đã hoàn thành" value={summary.completedRepairRequests} icon={<CheckCircle size={22} className="text-green-600" />} color="bg-green-50" />
        </div>
      )}

      {!hasRole('Admin', 'Manager') && (
        <div className="card p-6">
          <h3 className="font-semibold text-slate-800 mb-2">Hướng dẫn nhanh</h3>
          <ul className="text-sm text-slate-600 space-y-2 list-disc list-inside">
            {user?.role === 'Customer' && (
              <>
                <li>Xem phiếu bảo hành của bạn tại mục <strong>Phiếu bảo hành</strong>.</li>
                <li>Tạo yêu cầu sửa chữa khi thiết bị gặp sự cố.</li>
                <li>Theo dõi tiến độ sửa chữa tại mục <strong>Yêu cầu sửa chữa</strong>.</li>
              </>
            )}
            {user?.role === 'Receptionist' && (
              <>
                <li>Tạo phiếu bảo hành mới cho khách hàng.</li>
                <li>Tạo và gán kỹ thuật viên cho yêu cầu sửa chữa.</li>
              </>
            )}
            {user?.role === 'Technician' && (
              <>
                <li>Xem danh sách job được giao.</li>
                <li>Cập nhật trạng thái sửa chữa khi hoàn thành từng bước.</li>
              </>
            )}
          </ul>
        </div>
      )}
    </div>
  );
}
