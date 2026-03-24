import { jwtDecode } from 'jwt-decode';
import { HttpClient, HttpParams } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Router } from "@angular/router";
import { environment } from "../../environment";
import { AdminChangeProfileRequest, AuthResponseDto, AuthUserGetResponseDto, ChangeProfilePasswordRequestDto, ControleResponseDto, LoginRequestDto, PagedResultDto, PrivilegeResponseDto, RefreshTokenRequestDto, RegisterRequestDto, RoleResponseDto, UpdateProfileDto, UserStatsDto } from "../../interfaces/AuthDto";
import { BehaviorSubject, Observable, take, tap } from 'rxjs';

interface JwtPayload {
  sub: string;
  role: string;
  login: string;
  privilege: string | string[];
  exp: number;
}
export const PRIVILEGES = {
  // ── Users
  VIEW_USERS: 'VIEWUSERS',
  CREATE_USER: 'CREATEUSER',
  UPDATE_USER: 'UPDATEUSER',
  DELETE_USER: 'DELETEUSER',
  RESTORE_USER: 'RESTOREUSER',
  ACTIVATE_USER: 'ACTIVATEUSER',
  DEACTIVATE_USER: 'DEACTIVATEUSER',
  MANAGE_USERS: 'MANAGEUSERS',

  // ── Roles & Controls
  ASSIGN_ROLES: 'ASSIGNROLES',

  // ── Audit
  MANAGE_AUDIT_LOGS: 'MANAGEAUDITLOGS',

  // ── Clients
  VIEW_CLIENTS: 'VIEWCLIENTS',
  CREATE_CLIENT: 'CREATECLIENT',
  UPDATE_CLIENT: 'UPDATECLIENT',
  DELETE_CLIENT: 'DELETECLIENT',
  RESTORE_CLIENT: 'RESTORECLIENT',
  MANAGE_CLIENTS: 'MANAGECLIENTS',

  // ── Articles
  VIEW_ARTICLES: 'VIEWARTICLES',
  CREATE_ARTICLE: 'CREATEARTICLE',
  UPDATE_ARTICLE: 'UPDATEARTICLE',
  DELETE_ARTICLE: 'DELETEARTICLE',
  RESTORE_ARTICLE: 'RESTOREARTICLE',
  MANAGE_ARTICLES: 'MANAGEARTICLES',

  // ── Invoices
  VIEW_INVOICES: 'VIEWINVOICES',
  CREATE_INVOICE: 'CREATEINVOICE',
  VALIDATE_INVOICE: 'VALIDATEINVOICE',
  DELETE_INVOICE: 'DELETEINVOICE',
  RESTORE_INVOICE: 'RESTOREINVOICE',
  MANAGE_INVOICES: 'MANAGEINVOICES',

  // ── Payments
  VIEW_PAYMENTS: 'VIEWPAYMENTS',
  RECORD_PAYMENT: 'RECORDPAYMENT',
  DELETE_PAYMENT: 'DELETEPAYMENT',
  RESTORE_PAYMENT: 'RESTOREPAYMENT',
  MANAGE_PAYMENTS: 'MANAGEPAYMENTS',

  // ── Stock
  VIEW_STOCK: 'VIEWSTOCK',
  UPDATE_STOCK: 'UPDATESTOCK',
  ADD_ENTRY: 'ADDENTRY',
  MANAGE_STOCK: 'MANAGESTOCK',
};

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly ACCESS_TOKEN_KEY = 'accessToken';
  private readonly REFRESH_TOKEN_KEY = 'refreshToken';
  private readonly PROFILE_KEY = 'userProfile';
  private _cachedPayload: JwtPayload | null = null;
  private _cachedToken: string | null = null;
  private _userProfile$ = new BehaviorSubject<AuthUserGetResponseDto | null>(
    this.loadProfileFromStorage()  // rehydrate immediately on construction
  );
  readonly userProfile$ = this._userProfile$.asObservable();
  private _loggingOut = false;


  private readonly baseUrl = `${environment.apiUrl}${environment.routes.auth}`;

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
      if (token === this._cachedToken) return this._cachedPayload;
      try {
          this._cachedPayload = jwtDecode<JwtPayload>(token);
          this._cachedToken = token;
          return this._cachedPayload;
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

  // ========================
  // ROLES
  // ========================
  get isSystemAdmin(): boolean {
    return this.Role === 'SYSTEMADMIN';}

  get isStockManager(): boolean {
    return this.Role === 'STOCKMANAGER';}

  get isSalesManager(): boolean {
    return this.Role === 'SALESMANAGER';}
  get isAccountant(): boolean {
    return this.Role === 'ACCOUNTANT';}

  // ========================
  // PRIVILEGES
  // ========================
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

  // =========================
  // USERS + AUDIT LOGS
  // =========================
  get canManageUsers(): boolean {
    return this.hasPrivilege(PRIVILEGES.VIEW_USERS)
        || this.hasPrivilege(PRIVILEGES.ACTIVATE_USER)
        || this.hasPrivilege(PRIVILEGES.DEACTIVATE_USER)
        || this.hasPrivilege(PRIVILEGES.CREATE_USER)
        || this.hasPrivilege(PRIVILEGES.UPDATE_USER)
        || this.hasPrivilege(PRIVILEGES.DELETE_USER);
  }
  get canViewUsers(): boolean { return this.hasPrivilege(PRIVILEGES.VIEW_USERS); }
  get canUpdateUsers(): boolean { return this.hasPrivilege(PRIVILEGES.UPDATE_USER); }
  get canActivateUsers(): boolean { return this.hasPrivilege(PRIVILEGES.ACTIVATE_USER); }
  get canDeactivateUsers(): boolean { return this.hasPrivilege(PRIVILEGES.DEACTIVATE_USER); }
  get canRegisterUsers(): boolean { return this.hasPrivilege(PRIVILEGES.CREATE_USER); }
  get canDeleteUsers(): boolean { return this.hasPrivilege(PRIVILEGES.DELETE_USER); }
  get canRestoreUsers(): boolean { return this.hasPrivilege(PRIVILEGES.RESTORE_USER); }
  get canSeePermissions(): boolean { return this.hasPrivilege(PRIVILEGES.ASSIGN_ROLES); }
  get canAssignRoles(): boolean {return this.hasPrivilege(PRIVILEGES.ASSIGN_ROLES);}
  get canSeeAuditLog(): boolean { return this.hasPrivilege(PRIVILEGES.MANAGE_AUDIT_LOGS); }

  // =============================
  // ARTICLES
  // =============================
  get canManageArticles(): boolean {
    return this.hasPrivilege(PRIVILEGES.VIEW_ARTICLES)
        || this.hasPrivilege(PRIVILEGES.CREATE_ARTICLE)
        || this.hasPrivilege(PRIVILEGES.UPDATE_ARTICLE)
        || this.hasPrivilege(PRIVILEGES.DELETE_ARTICLE);
  }
  get canViewArticles(): boolean { return this.hasPrivilege(PRIVILEGES.VIEW_ARTICLES); }
  get canCreateArticles(): boolean { return this.hasPrivilege(PRIVILEGES.CREATE_ARTICLE); }
  get canUpdateArticles(): boolean { return this.hasPrivilege(PRIVILEGES.UPDATE_ARTICLE); }
  get canDeleteArticles(): boolean { return this.hasPrivilege(PRIVILEGES.DELETE_ARTICLE); }
  get canRestoreArticles(): boolean { return this.hasPrivilege(PRIVILEGES.RESTORE_ARTICLE); }

  // =============================
  // CLIENTS
  // =============================
  get canManageClients(): boolean {
    return this.hasPrivilege(PRIVILEGES.VIEW_CLIENTS)
        || this.hasPrivilege(PRIVILEGES.CREATE_CLIENT)
        || this.hasPrivilege(PRIVILEGES.UPDATE_CLIENT)
        || this.hasPrivilege(PRIVILEGES.DELETE_CLIENT);
  }
  get canViewClients(): boolean { return this.hasPrivilege(PRIVILEGES.VIEW_CLIENTS); }
  get canCreateClients(): boolean { return this.hasPrivilege(PRIVILEGES.CREATE_CLIENT); }
  get canUpdateClients(): boolean { return this.hasPrivilege(PRIVILEGES.UPDATE_CLIENT); }
  get canDeleteClients(): boolean { return this.hasPrivilege(PRIVILEGES.DELETE_CLIENT); }
  get canRestoreClients(): boolean { return this.hasPrivilege(PRIVILEGES.RESTORE_CLIENT); }

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

  /** GET /auth/deleted — Get deleted users (paginated) */
  getDeleted(
    pageNumber: number = 1,
    pageSize: number = 10): Observable<PagedResultDto<AuthUserGetResponseDto>> {
    const params = new HttpParams().set('pageNumber', pageNumber)
                                  .set('pageSize', pageSize);
    return this.http.get<PagedResultDto<AuthUserGetResponseDto>>(`${this.baseUrl}/deleted`,
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

  /** DELETE /auth/delete/soft/{id} — Soft delete: set IsDeleted to true */
  softDelete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** PATCH /auth/restore/{id} — Recover: reset IsDeleted to false */
  restore(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/restore/${id}`, {});
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
    if(this._loggingOut) return;
    this._loggingOut= true;

    const refreshToken = this.getRefreshToken();

    if (refreshToken) {
      this.revoke({ refreshToken })
        .pipe(take(1))
        .subscribe({
          complete: () => {
            this.clearSession();
            this.clearUserProfile();
            this._loggingOut = false;
            this.router.navigate(['/login']);
          },
          error: () => {
            this.clearSession();
            this.clearUserProfile();
            this._loggingOut = false;
            this.router.navigate(['/login']);
          }
        });
    }else {
      this.clearSession();
      this.clearUserProfile();
      this._loggingOut = false;
      this.router.navigate(['/login']);
    }
  }
}
