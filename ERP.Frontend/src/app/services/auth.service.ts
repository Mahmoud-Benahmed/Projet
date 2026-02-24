import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { environment } from '../environment';
import { AdminChangePasswordRequest, AuthResponse, ChangePasswordRequest, LoginRequest, RegisterRequest, RoleDto } from '../interfaces/AuthDto';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private baseUrl = `${environment.apiUrl}${environment.authUrl}`;
  readonly roles: RoleDto[] = [
    { value: 'Accountant', label: 'Accountant' },
    { value: 'HRManager', label: 'HR Manager' },
    { value: 'SalesManager', label: 'Sales Manager' },
    { value: 'StockManager', label: 'Stock Manager' },
    { value: 'SystemAdmin', label: 'System Admin' }
  ];

  constructor(private http: HttpClient, private router: Router) {}

  getUserById(id: string): Observable<any>{
    return this.http.get(this.baseUrl+`/${id}`);
  }

  login(credentials: LoginRequest): Observable<any> {
    return this.http.post<any>(this.baseUrl + '/login', credentials);
  }

  register(credentials: RegisterRequest): Observable<any> {
    return this.http.post<any>(this.baseUrl + '/register', credentials);
  }

  // =========================
  // CHANGE PASSWORD (Self)
  // =========================
  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/change-password/profile`, request);
  }

  // =========================
  // CHANGE PASSWORD (Admin)
  // =========================
  adminChangePassword(userId: string, request: AdminChangePasswordRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/change-password/${userId}`, request);
  }







  private decodeAccessToken(): any {
    const token = this.getAccessToken();
    if (!token) return null;
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch (e) {
      console.error('Invalid token', e);
      return null;
    }
  }

  logout(): void {
    const token = this.getRefreshToken();
    if (!token) {
      this.clearSession();
      this.router.navigate(['/login']);
      return;
    }
    this.revokeToken(token).subscribe({
      next: () => {
        this.clearSession();
        this.router.navigate(['/login']);
      },
      error: () => {
        this.clearSession();
        this.router.navigate(['/login']);
      }
    });
  }


  storeTokens(response: AuthResponse){
    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('expiresAt', response.expiresAt);
    localStorage.setItem('mustChangePassword', String(response.mustChangePassword)); // ADD THIS
  }
  // Update clearSession to include it:
  private clearSession(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('mustChangePassword');
  }


  get isLoggedIn(): boolean {
    const token = this.getAccessToken();
    const expiresAt = localStorage.getItem('expiresAt');
    if (!token || !expiresAt) return false;
    return new Date(expiresAt) > new Date();
  }

  get Role(): string | null {
    return this.decodeAccessToken()?.role ?? null;
  }

  get Email(): string | null {
    return this.decodeAccessToken()?.email ?? null;
  }

  get UserId(): string | null {
    return this.decodeAccessToken()?.sub ?? null;
  }

  get mustChangePassword(): boolean {
    return localStorage.getItem('mustChangePassword') === 'true';
  }


  get hasRole(): boolean {
    return this.roles.some(r => r.value === this.Role);
  }

    // Add to storeTokens or call separately after login
  storeMustChangePassword(value: boolean): void {
    localStorage.setItem('mustChangePassword', String(value));
  }

  clearMustChangePassword(): void {
    localStorage.removeItem('mustChangePassword');
  }


  revokeToken(token: string): Observable<any> {
    return this.http.post<any>(this.baseUrl + '/revoke', { refreshToken: token });
  }

  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }
}
