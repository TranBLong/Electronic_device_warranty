import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { User, UserRole } from '../types';
import { api } from '../api/client';

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  accessExpiresAt: string | null;
  refreshExpiresAt: string | null;
  user: User | null;
  setTokens: (access: string, refresh: string, accessExp: string, refreshExp: string) => void;
  setUser: (user: User | null) => void;
  logout: () => void;
  isAuthenticated: () => boolean;
  hasRole: (...roles: UserRole[]) => boolean;
  fetchMe: () => Promise<User | null>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      accessExpiresAt: null,
      refreshExpiresAt: null,
      user: null,

      setTokens: (access, refresh, accessExp, refreshExp) =>
        set({
          accessToken: access,
          refreshToken: refresh,
          accessExpiresAt: accessExp,
          refreshExpiresAt: refreshExp,
        }),

      setUser: (user) => set({ user }),

      logout: () =>
        set({
          accessToken: null,
          refreshToken: null,
          accessExpiresAt: null,
          refreshExpiresAt: null,
          user: null,
        }),

      isAuthenticated: () => !!get().accessToken,

      hasRole: (...roles) => {
        const u = get().user;
        return !!u && roles.includes(u.role);
      },

      fetchMe: async () => {
        try {
          const { data } = await api.get<User>('/api/users/me');
          // Map PascalCase if needed
          const user: User = {
            id: (data as any).id ?? (data as any).Id,
            fullName: (data as any).fullName ?? (data as any).FullName,
            email: (data as any).email ?? (data as any).Email,
            phone: (data as any).phone ?? (data as any).Phone,
            role: (data as any).role ?? (data as any).Role,
            isActive: (data as any).isActive ?? (data as any).IsActive,
            createdAt: (data as any).createdAt ?? (data as any).CreatedAt,
          };
          set({ user });
          return user;
        } catch {
          get().logout();
          return null;
        }
      },
    }),
    { name: 'e-warranty-auth' }
  )
);
