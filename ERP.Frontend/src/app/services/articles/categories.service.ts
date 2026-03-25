// category.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment';

// ── DTOs ──────────────────────────────────────────────────────────────────────

export interface CategoryRequestDto {
  name: string;
  tva: number;
}

export interface CategoryResponseDto {
  id: string;
  name: string;
  tva: number;
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly base = `${environment.apiUrl}/articles/categories`;

  constructor(private http: HttpClient) {}

  // GET /articles/categories
  getAll(): Observable<CategoryResponseDto[]> {
    return this.http.get<CategoryResponseDto[]>(this.base);
  }

  // GET /articles/categories/paged?pageNumber=&pageSize=
  getPaged(pageNumber = 1, pageSize = 10): Observable<PagedResultDto<CategoryResponseDto>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResultDto<CategoryResponseDto>>(`${this.base}/paged`, { params });
  }

  // GET /articles/categories/{id}
  getById(id: string): Observable<CategoryResponseDto> {
    return this.http.get<CategoryResponseDto>(`${this.base}/${id}`);
  }

  // GET /articles/categories/by-name?name=
  getByName(name: string): Observable<CategoryResponseDto> {
    const params = new HttpParams().set('name', name);
    return this.http.get<CategoryResponseDto>(`${this.base}/by-name`, { params });
  }

  // GET /articles/categories/by-date-range?from=&to=&pageNumber=&pageSize=
  getByDateRange(from: Date, to: Date, pageNumber = 1, pageSize = 10): Observable<PagedResultDto<CategoryResponseDto>> {
    const params = new HttpParams()
      .set('from', from.toISOString())
      .set('to', to.toISOString())
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResultDto<CategoryResponseDto>>(`${this.base}/by-date-range`, { params });
  }

  // GET /articles/categories/tva/below?tva=
  getBelowTVA(tva: number): Observable<CategoryResponseDto[]> {
    const params = new HttpParams().set('tva', tva);
    return this.http.get<CategoryResponseDto[]>(`${this.base}/tva/below`, { params });
  }

  // GET /articles/categories/tva/higher?tva=
  getHigherThanTVA(tva: number): Observable<CategoryResponseDto[]> {
    const params = new HttpParams().set('tva', tva);
    return this.http.get<CategoryResponseDto[]>(`${this.base}/tva/higher`, { params });
  }

  // GET /articles/categories/tva/between?min=&max=
  getBetweenTVA(min: number, max: number): Observable<CategoryResponseDto[]> {
    const params = new HttpParams()
      .set('min', min)
      .set('max', max);
    return this.http.get<CategoryResponseDto[]>(`${this.base}/tva/between`, { params });
  }

  // POST /articles/categories
  create(dto: CategoryRequestDto): Observable<CategoryResponseDto> {
    return this.http.post<CategoryResponseDto>(this.base, dto);
  }

  // PUT /articles/categories/{id}
  update(id: string, dto: CategoryRequestDto): Observable<CategoryResponseDto> {
    return this.http.put<CategoryResponseDto>(`${this.base}/${id}`, dto);
  }

  // DELETE /articles/categories/{id}
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
