import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { environment } from '../environment';
import { AdminChangePasswordRequest, ChangePasswordRequest, LoginRequest, RegisterRequest } from '../interfaces/AuthDto';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private baseUrl = `${environment.apiUrl}${environment.authUrl}`;

  constructor(private http: HttpClient, private router: Router) {}

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

  revokeToken(token: string): Observable<any> {
    return this.http.post<any>(this.baseUrl + '/revoke', { refreshToken: token });
  }

  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  getRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
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

  private clearSession(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('fullName');
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

  isLoggedIn(): boolean {
    const token = this.getAccessToken();
    const expiresAt = localStorage.getItem('expiresAt');
    if (!token || !expiresAt) return false;
    return new Date(expiresAt) > new Date();
  }

  getRole(): string | null {
    return this.decodeAccessToken()?.role ?? null;
  }

  getEmail(): string | null {
    return this.decodeAccessToken()?.email ?? null;
  }

  getUserId(): string | null {
    return this.decodeAccessToken()?.sub ?? null;
  }
}
