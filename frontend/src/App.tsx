import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuthStore } from './store/authStore';
import AppLayout from './layouts/AppLayout';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import ProductsPage from './pages/ProductsPage';
import WarrantyCardsPage from './pages/WarrantyCardsPage';
import RepairRequestsPage from './pages/RepairRequestsPage';
import UsersPage from './pages/UsersPage';
import PartsPage from './pages/PartsPage';
import CategoriesPage from './pages/CategoriesPage';
import ProfilePage from './pages/ProfilePage';
import type { UserRole } from './types';

function ProtectedRoute({ children, roles }: { children: React.ReactNode; roles?: UserRole[] }) {
  const { isAuthenticated, user, hasRole } = useAuthStore();
  if (!isAuthenticated()) return <Navigate to="/login" replace />;
  if (roles && user && !hasRole(...roles)) return <Navigate to="/" replace />;
  return <>{children}</>;
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<DashboardPage />} />
        <Route path="products" element={<ProductsPage />} />
        <Route path="warranty-cards" element={<WarrantyCardsPage />} />
        <Route path="repair-requests" element={<RepairRequestsPage />} />
        <Route
          path="users"
          element={
            <ProtectedRoute roles={['Admin']}>
              <UsersPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="parts"
          element={
            <ProtectedRoute roles={['Admin', 'Manager', 'Technician', 'Receptionist']}>
              <PartsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="categories"
          element={
            <ProtectedRoute roles={['Admin']}>
              <CategoriesPage />
            </ProtectedRoute>
          }
        />
        <Route path="profile" element={<ProfilePage />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
