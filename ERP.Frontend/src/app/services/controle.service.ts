import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environment';

export interface ControleResponseDto {
  id: string;
  category: string;
  libelle: string;
  description: string;
}

@Injectable({
  providedIn: 'root'
})
export class ControleService {

  private readonly baseUrl = `${environment.apiUrl}${environment.routes.controles}`;

  constructor(private http: HttpClient) {}

  /**
   * GET /auth/controles
   * Retrieves all controles. (AdminOnly)
   */
  getAll(): Observable<ControleResponseDto[]> {
    return this.http.get<ControleResponseDto[]>(this.baseUrl);
  }

  /**
   * GET /auth/controles/:id
   * Retrieves a single controle by its UUID. (AdminOnly)
   */
  getById(id: string): Observable<ControleResponseDto> {
    return this.http.get<ControleResponseDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * GET /auth/controles/category/:category
   * Retrieves all controles belonging to a given category. (AdminOnly)
   */
  getByCategory(category: string): Observable<ControleResponseDto[]> {
    return this.http.get<ControleResponseDto[]>(`${this.baseUrl}/category/${category}`);
  }
}
