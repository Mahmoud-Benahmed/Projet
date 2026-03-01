import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environment';
import { CompleteProfileDto, CreateUserProfileDto, PagedResultDto, UserProfileResponseDto, UserStatsDto } from '../interfaces/UserProfileDto';

// =========================
// Service
// =========================

@Injectable({
  providedIn: 'root'
})
export class UsersService {
  private readonly base = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  // =========================
  // GET ME
  // =========================
  getMe(): Observable<UserProfileResponseDto> {
    return this.http.get<UserProfileResponseDto>(`${this.base}/me`);
  }

  // =========================
  // GET ALL
  // =========================
  getAll(): Observable<UserProfileResponseDto[]> {
    return this.http.get<UserProfileResponseDto[]>(this.base);
  }

  // =========================
  // GET BY ID
  // =========================
  getById(id: string): Observable<UserProfileResponseDto> {
    return this.http.get<UserProfileResponseDto>(`${this.base}/${id}`);
  }

  // =========================
  // GET BY AUTH USER ID
  // =========================
  getByAuthUserId(authUserId: string): Observable<UserProfileResponseDto> {
    return this.http.get<UserProfileResponseDto>(`${this.base}/authId/${authUserId}`);
  }

  // =========================
  // GET BY LOGIN
  // =========================
  getByLogin(login: string): Observable<UserProfileResponseDto> {
    return this.http.get<UserProfileResponseDto>(`${this.base}/login/${login}`);
  }

  // =========================
  // EXISTS BY AUTH USER ID
  // =========================
  existsByAuthUserId(authUserId: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.base}/exists-authId/${authUserId}`);
  }

  // =========================
  // EXISTS BY LOGIN
  // =========================
  existsByLogin(login: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.base}/exists-login/${login}`);
  }

  // =========================
  // GET ACTIVE (PAGED)
  // =========================
  getActive(pageNumber = 1, pageSize = 10): Observable<PagedResultDto<UserProfileResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResultDto<UserProfileResponseDto>>(`${this.base}/active`, { params });
  }

  // =========================
  // GET DEACTIVATED (PAGED)
  // =========================
  getDeactivated(pageNumber = 1, pageSize = 10): Observable<PagedResultDto<UserProfileResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResultDto<UserProfileResponseDto>>(`${this.base}/deactivated`, { params });
  }

  // =========================
  // GET BY ROLE (PAGED)
  // =========================
  getByRole(role: string, pageNumber = 1, pageSize = 10): Observable<PagedResultDto<UserProfileResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResultDto<UserProfileResponseDto>>(`${this.base}/role/${role}`, { params });
  }

  // =========================
  // GET STATS
  // =========================
  getStats(): Observable<UserStatsDto> {
    return this.http.get<UserStatsDto>(`${this.base}/stats`);
  }

  // =========================
  // CREATE
  // =========================
  create(dto: CreateUserProfileDto): Observable<UserProfileResponseDto> {
    return this.http.post<UserProfileResponseDto>(this.base, dto);
  }

  // =========================
  // COMPLETE PROFILE
  // =========================
  completeProfile(authUserId: string, dto: CompleteProfileDto): Observable<UserProfileResponseDto> {
    return this.http.put<UserProfileResponseDto>(`${this.base}/${authUserId}/complete`, dto);
  }

  // =========================
  // ACTIVATE
  // =========================
  activate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/activate`, {});
  }

  // =========================
  // DEACTIVATE
  // =========================
  deactivate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/deactivate`, {});
  }

  // =========================
  // DELETE
  // =========================
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
