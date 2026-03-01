// =========================
// DTOs
// =========================

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

export interface RegisterRequestDto {
  login: string;
  email: string;
  password: string;
  roleId: string;
}

export interface LoginRequestDto {
  login: string;
  password: string;
}

export interface AuthResponseDto {
  accessToken: string;
  refreshToken: string;
  mustChangePassword: boolean;
  expiresAt: string;
}

export interface ChangePasswordRequestDto {
  currentPassword: string;
  newPassword: string;
}

export interface AdminChangePasswordRequestDto {
  newPassword: string;
}

export interface RefreshTokenRequestDto {
  refreshToken: string;
}

export interface ControleResponseDto {
  id: string;
  category: string;
  libelle: string;
  description: string;
}

// =========================
// DTOs
// =========================

export interface PrivilegeResponseDto {
  id: string;
  roleId: string;
  controleId: string;
  controleLibelle: string;
  controleCategory: string;
  isGranted: boolean;
}


export enum RoleEnum {
  SystemAdmin = 'SystemAdmin',
  Accountant='Accountant',
  SalesManager='SalesManager',
  StockManager='StockManager'
}
