import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environment';
import { RoleEnum } from '../interfaces/AuthDto';

export interface RoleResponseDto {
  id: string;
  libelle: RoleEnum;
}

// =========================
// Service
// =========================

@Injectable({
  providedIn: 'root'
})
export class RolesService {
  private readonly base = `${environment.apiUrl}/auth/roles`;

  constructor(private http: HttpClient) {}

  // =========================
  // GET ALL
  // =========================
  getAll(): Observable<RoleResponseDto[]> {
    return this.http.get<RoleResponseDto[]>(this.base);
  }

  // =========================
  // GET BY ID
  // =========================
  getById(id: string): Observable<RoleResponseDto> {
    return this.http.get<RoleResponseDto>(`${this.base}/${id}`);
  }
}
