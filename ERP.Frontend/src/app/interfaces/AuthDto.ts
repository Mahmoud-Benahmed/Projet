

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface AdminChangePasswordRequest {
  newPassword: string;
}

export interface LoginRequest {
  email: string;
  password: string
}

export interface RegisterRequest extends LoginRequest{
  roleId: ''
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
