import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environment';

export interface RoleResponseDto {
  id: string;
  libelle: string;
}

@Injectable({
  providedIn: 'root'
})
export class RoleService {

  private readonly baseUrl = `${environment.apiUrl}${environment.routes.roles}`;

  constructor(private http: HttpClient) {}

  /**
   * GET /auth/roles
   * Retrieves all roles. (AdminOnly)
   */
  getAll(): Observable<RoleResponseDto[]> {
    return this.http.get<RoleResponseDto[]>(this.baseUrl);
  }

  /**
   * GET /auth/roles/:id
   * Retrieves a single role by its UUID. (AdminOnly)
   */
  getById(id: string): Observable<RoleResponseDto> {
    return this.http.get<RoleResponseDto>(`${this.baseUrl}/${id}`);
  }
}
