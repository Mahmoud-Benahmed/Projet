import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../environment';
import { AuthUserGetResponseDto, AdminChangePasswordRequestDto, AuthResponseDto, ChangePasswordRequestDto, LoginRequestDto, RefreshTokenRequestDto, RegisterRequestDto } from '../interfaces/AuthDto';
import { jwtDecode } from 'jwt-decode';
import { UserProfileResponseDto } from '../interfaces/UserProfileDto';
import { Router } from '@angular/router';

interface JwtPayload {
  sub: string;
  role: string;
  login: string;
  email: string;
  privilege: string | string[];
  exp: number;
}

export interface FullProfile extends UserProfileResponseDto{
  mustChangePassword: boolean;
  lastLoginAt: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly base = `${environment.apiUrl}${environment.routes.auth}`;
  private readonly ACCESS_TOKEN_KEY = 'accessToken';
  private readonly REFRESH_TOKEN_KEY = 'refreshToken';
  private _userProfile: FullProfile | null = null;

  constructor(private http: HttpClient,   private router: Router) {}

  // =========================
  // TOKEN STORAGE
  // =========================
  storeTokens(response: AuthResponseDto): void {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, response.accessToken);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem('expiresAt', response.expiresAt);
    localStorage.setItem('mustChangePassword', String(response.mustChangePassword)); // ADD THIS
  }

  getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  getExpiresAt(): Date | null {
    const value = localStorage.getItem('expiresAt');
    return value ? new Date(value) : null;
  }

  getMustChangePassword(): boolean {
    return localStorage.getItem('mustChangePassword') === 'true';
  }


  clearSession(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem('expiresAt');
    localStorage.removeItem('mustChangePassword');
  }

  isLoggedIn(): boolean {
    const token = this.getAccessToken();
    if (!token) return false;
    try {
      const decoded = jwtDecode<JwtPayload>(token);
      return decoded.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  // =========================
  // CLAIM GETTERS
  // =========================
  private getPayload(): JwtPayload | null {
    const token = this.getAccessToken();
    if (!token) return null;
    try {
      return jwtDecode<JwtPayload>(token);
    } catch {
      return null;
    }
  }

  get UserId(): string | null {
    return this.getPayload()?.sub ?? null;
  }

  get Role(): string | null {
    return this.getPayload()?.role ?? null;
  }

  get Login(): string | null {
    return this.getPayload()?.login ?? null;
  }

  get Privileges(): string[] {
    const payload = this.getPayload();
    if (!payload?.privilege) return [];
    return Array.isArray(payload.privilege)
      ? payload.privilege
      : [payload.privilege];
  }

  hasPrivilege(privilege: string): boolean {
    return this.Privileges.includes(privilege);
  }


  storeMustChangePassword(value: boolean): void {
    localStorage.setItem('mustChangePassword', String(value));
  }

  clearMustChangePassword(): void {
    localStorage.removeItem('mustChangePassword');
  }


  // =========================
  // USER PROFILE CACHE
  // =========================
  get UserProfile(): FullProfile | null {
    return this._userProfile;
  }

  setUserProfile(profile: FullProfile): void {
    this._userProfile = profile;
  }

  clearUserProfile(): void {
    this._userProfile = null;
  }

  // =========================
  // GET ME
  // =========================
  getMe(): Observable<AuthUserGetResponseDto> {
    return this.http.get<AuthUserGetResponseDto>(`${this.base}/me`);
  }

  // =========================
  // GET BY ID
  // =========================
  getById(id: string): Observable<AuthUserGetResponseDto> {
    return this.http.get<AuthUserGetResponseDto>(`${this.base}/${id}`);
  }

  // =========================
  // GET BY LOGIN
  // =========================
  getByLogin(login: string): Observable<AuthUserGetResponseDto> {
    return this.http.get<AuthUserGetResponseDto>(`${this.base}/login/${login}`);
  }

  // =========================
  // EXISTS BY LOGIN
  // =========================
  existsByLogin(login: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.base}/exists-login/${login}`);
  }

  // =========================
  // EXISTS BY EMAIL
  // =========================
  existsByEmail(email: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.base}/exists-email/${email}`);
  }

  // =========================
  // REGISTER
  // =========================
  register(request: RegisterRequestDto): Observable<AuthUserGetResponseDto> {
    return this.http.post<AuthUserGetResponseDto>(`${this.base}/register`, request);
  }

  // =========================
  // LOGIN
  // =========================
  login(request: LoginRequestDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.base}/login`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  // =========================
  // CHANGE PASSWORD (own profile)
  // =========================
  changePassword(request: ChangePasswordRequestDto): Observable<void> {
    return this.http.put<void>(`${this.base}/change-password/profile`, request);
  }

  // =========================
  // CHANGE PASSWORD (admin)
  // =========================
  adminChangePassword(userId: string, request: AdminChangePasswordRequestDto): Observable<void> {
    return this.http.put<void>(`${this.base}/change-password/${userId}`, request);
  }

  // =========================
  // REFRESH TOKEN
  // =========================
  refresh(request: RefreshTokenRequestDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.base}/refresh`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  // =========================
  // REVOKE + LOGOUT
  // =========================
  revoke(request: RefreshTokenRequestDto): Observable<void> {
    return this.http.post<void>(`${this.base}/revoke`, request).pipe(
      tap(() => {
        this.clearSession();
        this.clearUserProfile();
      })
    );
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();

    if (refreshToken) {
      this.revoke({ refreshToken }).subscribe({
        next: () => this.router.navigate(['/login']),
        error: () => {
          this.clearSession();
          this.clearUserProfile();
          this.router.navigate(['/login']);
        }
      });
    } else {
      this.clearSession();
      this.clearUserProfile();
      this.router.navigate(['/login']);
    }
  }
}
