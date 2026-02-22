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
