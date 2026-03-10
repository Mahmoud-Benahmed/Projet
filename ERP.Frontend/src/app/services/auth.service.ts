import { jwtDecode } from 'jwt-decode';
import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { environment } from "../environment";
import { AdminChangeProfileRequest, AuthResponseDto, AuthUserGetResponseDto, ChangeProfilePasswordRequestDto, ControleResponseDto, LoginRequestDto, PagedResultDto, PrivilegeResponseDto, RefreshTokenRequestDto, RegisterRequestDto, RoleResponseDto, UpdateProfileDto, UserStatsDto } from "../interfaces/AuthDto";
import { BehaviorSubject, Observable, tap } from 'rxjs';

interface JwtPayload {
  sub: string;
  role: string;
  login: string;
  privilege: string | string[];
  exp: number;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly baseUrl = `${environment.apiUrl}${environment.routes.auth}`;
  private readonly ACCESS_TOKEN_KEY = 'accessToken';
  private readonly REFRESH_TOKEN_KEY = 'refreshToken';
  private readonly PROFILE_KEY = 'userProfile';
  private _userProfile$ = new BehaviorSubject<AuthUserGetResponseDto | null>(
    this.loadProfileFromStorage()  // rehydrate immediately on construction
  );
  readonly userProfile$ = this._userProfile$.asObservable();

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
    localStorage.removeItem(this.PROFILE_KEY);
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
  private loadProfileFromStorage(): AuthUserGetResponseDto | null {
    const raw = localStorage.getItem(this.PROFILE_KEY);
    try {
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }
  get UserProfile(): AuthUserGetResponseDto | null {
    return this._userProfile$.value;
  }

  setUserProfile(profile: AuthUserGetResponseDto): void {
    localStorage.setItem(this.PROFILE_KEY, JSON.stringify(profile));
    this._userProfile$.next(profile);
  }

  clearUserProfile(): void {
    localStorage.removeItem(this.PROFILE_KEY);
    this._userProfile$.next(null);
  }

  // =========================
  // GET ME
  // =========================
  getMe(): Observable<AuthUserGetResponseDto> {
    return this.http.get<AuthUserGetResponseDto>(`${this.baseUrl}/me`);
  }

  // =========================
  // GET BY ID
  // =========================
  getById(id: string): Observable<AuthUserGetResponseDto> {
    return this.http.get<AuthUserGetResponseDto>(`${this.baseUrl}/${id}`);
  }

  // =========================
  // GET BY LOGIN
  // =========================
  getByLogin(login: string): Observable<AuthUserGetResponseDto> {
    return this.http.get<AuthUserGetResponseDto>(`${this.baseUrl}/login/${login}`);
  }

  // ── Auth: User Lists ─────────────────────────────────────────────────────

  /** GET /auth — Get all users (paginated) */
  getUsers(pageNumber: number = 1, pageSize: number = 10): Observable<PagedResultDto<AuthUserGetResponseDto>> {
      const params = new HttpParams().set('pageNumber', pageNumber)
                                      .set('pageSize', pageSize);
      return this.http.get<PagedResultDto<AuthUserGetResponseDto>>(this.baseUrl, { params });
  }

  /** GET /auth/activated — Get all active users (paginated) */
  getActivatedUsers(pageNumber: number = 1,
                    pageSize: number = 10): Observable<PagedResultDto<AuthUserGetResponseDto>> {
      const params = new HttpParams().set('pageNumber', pageNumber)
                                      .set('pageSize', pageSize);
      return this.http.get<PagedResultDto<AuthUserGetResponseDto>>(`${this.baseUrl}/activated`,
                                                                    { params });
  }

  /** GET /auth/deactivated — Get all deactivated users (paginated) */
  getDeactivatedUsers(pageNumber: number = 1,
                      pageSize: number = 10): Observable<PagedResultDto<AuthUserGetResponseDto>> {
    const params = new HttpParams().set('pageNumber', pageNumber)
                                    .set('pageSize', pageSize);
    return this.http.get<PagedResultDto<AuthUserGetResponseDto>>(`${this.baseUrl}/deactivated`,
                                                                  { params });
  }

  /** GET /auth/by-role — Get users filtered by role (paginated) */
  getUsersByRole(roleId: string,
                  pageNumber: number = 1,
                  pageSize: number = 10): Observable<PagedResultDto<AuthUserGetResponseDto>> {
    const params = new HttpParams().set('roleId', roleId)
                                    .set('pageNumber', pageNumber)
                                    .set('pageSize', pageSize);
    return this.http.get<PagedResultDto<AuthUserGetResponseDto>>(`${this.baseUrl}/by-role`,
                                                                  { params });
  }


  // =========================
  // EXISTS BY LOGIN
  // =========================
  existsByLogin(login: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.baseUrl}/exists-login/${login}`);
  }

  // =========================
  // EXISTS BY EMAIL
  // =========================
  existsByEmail(email: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.baseUrl}/exists-email/${email}`);
  }

  getStats(): Observable<UserStatsDto>{
    return this.http.get<UserStatsDto>(`${this.baseUrl}/stats`);
  }

  // =========================
  // REGISTER
  // =========================
  register(request: RegisterRequestDto): Observable<AuthUserGetResponseDto> {
    return this.http.post<AuthUserGetResponseDto>(`${this.baseUrl}/register`, request);
  }

  // =========================
  // LOGIN
  // =========================
  login(request: LoginRequestDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.baseUrl}/login`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  // ========================
  // UPDATE
  // ========================

  update(id: string, request: UpdateProfileDto): Observable<AuthUserGetResponseDto>{
    return this.http.put<AuthUserGetResponseDto>(`${this.baseUrl}/update/${id}`, request);
  }
    // ── Auth: Activation ─────────────────────────────────────────────────────

  /** PATCH /auth/{id}/activate — Activate a user account */
  activate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/activate`, {});
  }

  /** PATCH /auth/{id}/deactivate — Deactivate a user account */
  deactivate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/deactivate`, {});
  }


  // ── Auth: Password Management ────────────────────────────────────────────

  /** PUT /auth/change-password/profile — Change own password (requires current password) */
  changeProfilePassword(payload: ChangeProfilePasswordRequestDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/change-password/profile`, payload);
  }

  /** PUT /auth/change-password/{userId} — Admin: force-change a user's password */
  adminChangePassword(userId: string, payload: AdminChangeProfileRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/change-password/${userId}`, payload);
  }


  // ── Roles ────────────────────────────────────────────────────────────────

  /** GET /auth/roles — Get all roles */
  getRoles(): Observable<RoleResponseDto[]> {
    return this.http.get<RoleResponseDto[]>(`${this.baseUrl}/roles`);
  }

  /** GET /auth/roles/{id} — Get a role by ID */
  getRoleById(id: string): Observable<RoleResponseDto> {
    return this.http.get<RoleResponseDto>(`${this.baseUrl}/roles/${id}`);
  }

  // ── Controles ────────────────────────────────────────────────────────────

  /** GET /auth/controles — Get all controles */
  getControles(): Observable<ControleResponseDto[]> {
    return this.http.get<ControleResponseDto[]>(`${this.baseUrl}/controles`);
  }

  /** GET /auth/controles/{id} — Get a controle by ID */
  getControleById(id: string): Observable<ControleResponseDto> {
    return this.http.get<ControleResponseDto>(`${this.baseUrl}/controles/${id}`);
  }

  /** GET /auth/controles/category/{category} — Get controles by category */
  getControlesByCategory(category: string): Observable<ControleResponseDto[]> {
    return this.http.get<ControleResponseDto[]>(
      `${this.baseUrl}/controles/category/${category}`
    );
  }

  // ── Privileges ───────────────────────────────────────────────────────────

  /** GET /auth/privileges/{roleId} — Get all privileges for a role */
  getPrivilegesByRole(roleId: string): Observable<PrivilegeResponseDto[]> {
    return this.http.get<PrivilegeResponseDto[]>(`${this.baseUrl}/privileges/${roleId}`);
  }

  /** PUT /auth/privileges/{roleId}/{controleId}/allow — Grant a privilege */
  allowPrivilege(roleId: string, controleId: string): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/privileges/${roleId}/${controleId}/allow`,
      {}
    );
  }

  /** PUT /auth/privileges/{roleId}/{controleId}/deny — Revoke a privilege */
  denyPrivilege(roleId: string, controleId: string): Observable<void> {
    return this.http.put<void>(
      `${this.baseUrl}/privileges/${roleId}/${controleId}/deny`,
      {}
    );
  }

  // =========================
  // REFRESH TOKEN
  // =========================
  refresh(request: RefreshTokenRequestDto): Observable<AuthResponseDto> {
    return this.http.post<AuthResponseDto>(`${this.baseUrl}/refresh`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  // =========================
  // REVOKE + LOGOUT
  // =========================
  revoke(request: RefreshTokenRequestDto): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/revoke`, request);
  }

  logout(): void {
    const refreshToken = this.getRefreshToken();

    // Always clear immediately — don't wait for revoke
    this.clearSession();
    this.clearUserProfile();

    if (refreshToken) {
      this.revoke({ refreshToken }).subscribe({
        error: () => {} // silently ignore failures
      });
    }

    this.router.navigate(['/login']);
  }
}
