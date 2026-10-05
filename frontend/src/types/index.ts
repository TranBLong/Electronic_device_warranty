export type UserRole = 'Admin' | 'Manager' | 'Receptionist' | 'Technician' | 'Customer';
export type WarrantyStatus = 'Active' | 'Expired' | 'Voided';
export type RepairRequestStatus = 'Received' | 'InProgress' | 'Completed' | 'Returned' | 'Cancelled';

export interface User {
  id: number;
  fullName: string;
  email: string;
  phone?: string | null;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}

export interface Product {
  id: number;
  name: string;
  brand: string;
  model: string;
  serialNumber: string;
  warrantyMonths: number;
  createdAt: string;
  categoryId?: number | null;
}

export interface WarrantyCard {
  id: number;
  productId: number;
  customerId: number;
  customerName?: string;
  productName?: string;
  productBrand?: string;
  productSerial?: string;
  startDate: string;
  endDate: string;
  status: WarrantyStatus;
}

export interface RepairStatusHistory {
  id: number;
  oldStatus?: RepairRequestStatus | null;
  newStatus: RepairRequestStatus;
  changedBy: number;
  changedAt: string;
}

export interface RepairRequest {
  id: number;
  warrantyCardId: number;
  customerId: number;
  customerName: string;
  receptionistId?: number | null;
  technicianId?: number | null;
  technicianName?: string | null;
  description: string;
  status: RepairRequestStatus;
  createdAt: string;
  updatedAt: string;
  statusHistory: RepairStatusHistory[];
}

export interface DashboardSummary {
  totalWarrantyCards: number;
  activeWarrantyCards: number;
  totalRepairRequests: number;
  receivedRepairRequests: number;
  inProgressRepairRequests: number;
  completedRepairRequests: number;
}

export interface Category {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface Part {
  id: number;
  code: string;
  name: string;
  description?: string | null;
  unitPrice: number;
  stockQuantity: number;
  isActive: boolean;
  createdAt: string;
}

export interface ServiceCenter {
  id: number;
  name: string;
  address: string;
  phone?: string | null;
  isActive: boolean;
  createdAt: string;
}
