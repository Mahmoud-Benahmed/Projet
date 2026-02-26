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
  email: '', roleId: ''
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
    Id: string,
    Email: string,
    Login: string,
    RoleId: string,
    RoleName: string,
    MustChangePassword: boolean,
    IsActive: boolean,
    CreatedAt: string,
    UpdatedAt: string,
    LastLoginAt?: string
}
