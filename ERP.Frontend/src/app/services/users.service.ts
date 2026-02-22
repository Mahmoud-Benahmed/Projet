import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  UserProfileResponseDto,
  CreateUserProfileDto,
  CompleteProfileDto,
  PagedResultDto,
} from '../interfaces/UserProfileDto';
import { environment } from '../environment';

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly baseUrl = `${environment.apiUrl}${environment.usersUrl}`;

  constructor(private http: HttpClient) {}

  // =========================
  // CREATE
  // =========================
  create(dto: CreateUserProfileDto): Observable<UserProfileResponseDto> {
    return this.http.post<UserProfileResponseDto>(this.baseUrl, dto);
  }

  // =========================
  // GET BY ID
  // =========================
  getById(id: string): Observable<UserProfileResponseDto> {
    return this.http.get<UserProfileResponseDto>(`${this.baseUrl}/${id}`);
  }

  // =========================
  // GET BY AUTH USER ID
  // =========================
  getByAuthUserId(authUserId: string): Observable<UserProfileResponseDto> {
    return this.http.get<UserProfileResponseDto>(`${this.baseUrl}/auth/${authUserId}`);
  }

  // =========================
  // GET ALL
  // =========================
  getAll(): Observable<UserProfileResponseDto[]> {
    return this.http.get<UserProfileResponseDto[]>(this.baseUrl);
  }

  // =========================
  // GET ACTIVE (PAGED)
  // =========================
  getActiveUsers(
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResultDto<UserProfileResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http.get<PagedResultDto<UserProfileResponseDto>>(
      `${this.baseUrl}/active`,
      { params }
    );
  }

  // =========================
  // GET DEACTIVATED (PAGED)
  // =========================
  getDeactivatedUsers(
    pageNumber: number = 1,
    pageSize: number = 10
  ): Observable<PagedResultDto<UserProfileResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);

    return this.http.get<PagedResultDto<UserProfileResponseDto>>(
      `${this.baseUrl}/deactivated`,
      { params }
    );
  }

  // =========================
  // COMPLETE PROFILE
  // =========================
  completeProfile(
    authUserId: string,
    dto: CompleteProfileDto
  ): Observable<UserProfileResponseDto> {
    return this.http.put<UserProfileResponseDto>(
      `${this.baseUrl}/${authUserId}/complete`,
      dto
    );
  }

  // =========================
  // ACTIVATE
  // =========================
  activate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/activate`, {});
  }

  // =========================
  // DEACTIVATE
  // =========================
  deactivate(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/deactivate`, {});
  }

  // =========================
  // DELETE (SystemAdmin only)
  // =========================
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
