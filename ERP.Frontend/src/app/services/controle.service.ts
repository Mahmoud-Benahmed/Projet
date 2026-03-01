import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environment';
import { ControleResponseDto } from '../interfaces/AuthDto';

// =========================
// Service
// =========================

@Injectable({
  providedIn: 'root'
})
export class ControlesService {
  private readonly base = `${environment.apiUrl}/auth/controles`;

  constructor(private http: HttpClient) {}

  // =========================
  // GET ALL
  // =========================
  getAll(): Observable<ControleResponseDto[]> {
    return this.http.get<ControleResponseDto[]>(this.base);
  }

  // =========================
  // GET BY ID
  // =========================
  getById(id: string): Observable<ControleResponseDto> {
    return this.http.get<ControleResponseDto>(`${this.base}/${id}`);
  }

  // =========================
  // GET BY CATEGORY
  // =========================
  getByCategory(category: string): Observable<ControleResponseDto[]> {
    return this.http.get<ControleResponseDto[]>(`${this.base}/category/${category}`);
  }
}
