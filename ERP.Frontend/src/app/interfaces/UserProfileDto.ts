
// =========================
// DTOs
// =========================

export interface UserProfileResponseDto  {
  id: string;
  authUserId: string;
  login: string;
  email: string;
  fullName: string | null;
  phone: string | null;
  role: string;
  isActive: boolean;
  isProfileCompleted: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface AuthUserGetResponseDto {
  id: string;
  email: string;
  login: string;
  roleId: string;
  roleName: string;
  mustChangePassword: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  lastLoginAt: string | null;
}
export interface CreateUserProfileDto {
  login: string;
  role: string;
  authUserId: string;
  email: string;
}

export interface CompleteProfileDto {
  fullName: string;
  phone: string;
}

export interface UserStatsDto {
  totalUsers: number;
  activeUsers: number;
  deactivatedUsers: number;
  completedProfiles: number;
  incompletedProfiles: number;
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}
