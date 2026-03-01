import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environment';
import { PrivilegeResponseDto } from '../interfaces/AuthDto';

// =========================
// Service
// =========================

@Injectable({
  providedIn: 'root'
})
export class PrivilegesService {
  private readonly base = `${environment.apiUrl}/auth/privileges`;

  constructor(private http: HttpClient) {}

  // =========================
  // GET BY ROLE ID
  // =========================
  getByRoleId(roleId: string): Observable<PrivilegeResponseDto[]> {
    return this.http.get<PrivilegeResponseDto[]>(`${this.base}/${roleId}`);
  }

  // =========================
  // ALLOW
  // =========================
  allow(roleId: string, controleId: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${roleId}/${controleId}/allow`, {});
  }

  // =========================
  // DENY
  // =========================
  deny(roleId: string, controleId: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${roleId}/${controleId}/deny`, {});
  }
}
