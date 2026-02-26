import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environment';

export interface PrivilegeResponseDto {
  id: string;
  roleId: string;
  controleId: string;
  controleLibelle: string;
  controleCategory: string;
  isGranted: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class PrivilegeService {

  private readonly baseUrl = `${environment.apiUrl}${environment.routes.privileges}`;

  constructor(private http: HttpClient) {}

  /**
   * GET /auth/privileges/:roleId
   * Retrieves all privileges for a given role, enriched with controle details. (AdminOnly)
   */
  getByRoleId(roleId: string): Observable<PrivilegeResponseDto[]> {
    return this.http.get<PrivilegeResponseDto[]>(`${this.baseUrl}/${roleId}`);
  }

  /**
   * PUT /auth/privileges/:roleId/:controleId/allow
   * Grants a privilege (sets IsGranted = true) for the given role/controle pair. (AdminOnly)
   */
  allow(roleId: string, controleId: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${roleId}/${controleId}/allow`, {});
  }

  /**
   * PUT /auth/privileges/:roleId/:controleId/deny
   * Revokes a privilege (sets IsGranted = false) for the given role/controle pair. (AdminOnly)
   */
  deny(roleId: string, controleId: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${roleId}/${controleId}/deny`, {});
  }
}
