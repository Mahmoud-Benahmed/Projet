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
  role: 'Accountant' | 'SalesManager' | 'StockManager' | 'HRManager' | 'SystemAdmin' | ''
}
