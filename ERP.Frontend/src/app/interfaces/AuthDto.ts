export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface AdminChangePasswordRequest {
  newPassword: string;
}

export interface LoginRequest {
  login: string;
  password: string
}

export interface RegisterRequest extends LoginRequest{
  email: string, roleId: string
}

export interface AuthResponse{
  accessToken: string,
  refreshToken: string,
  expiresAt: string,
  mustChangePassword: boolean
}


export interface RoleDto{
  value: string,
  label: string
}

export interface AuthUserDto{
    id: string,
    email: string,
    login: string,
    roleId: string,
    roleName: string,
    mustChangePassword: boolean,
    isActive: boolean,
    createdAt: string,
    updatedAt: string,
    lastLoginAt?: string
}
