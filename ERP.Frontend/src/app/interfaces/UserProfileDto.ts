export interface UserProfileResponseDto {
  id: string;
  authUserId: string;
  email: string;
  fullName?: string;
  phone?: string;
  isActive: boolean;
  isProfileCompleted: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface FullProfile extends UserProfileResponseDto {
  login: string
  roleName: string;
  mustChangePassword: boolean;
  lastLoginAt?: string;
}

export interface CreateUserProfileDto {
  authUserId: string;
  email: string;
}

export interface CompleteProfileDto {
  fullName: string;
  phone: string;
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}


export interface UserStatsDto {
  totalUsers: number;
  activeUsers: number;
  deactivatedUsers: number;
  completedProfiles: number;
}
