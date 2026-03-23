import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../environment';

// ========================
// Models
// ========================
export type ClientType = 'Individual' | 'Company';

export interface Client {
  id: string;
  type: ClientType;
  name: string;
  email: string;
  address: string;
  phone?: string;
  taxNumber?: string;
  isDeleted: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface ClientStatsDto {
  TotalCount: number;
  ActiveCount: number;
  DeletedCount: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

export interface CreateClientRequest {
  type: ClientType;
  name: string;
  email: string;
  address: string;
  phone?: string;
  taxNumber?: string;
}

export interface UpdateClientRequest {
  type: ClientType;
  name: string;
  email: string;
  address: string;
  phone?: string;
  taxNumber?: string;
}

// ========================
// Service
// ========================
@Injectable({
  providedIn: 'root',
})
export class ClientService {

  private readonly baseUrl = `${environment.apiUrl}${environment.routes.clients}`;

  constructor(private http: HttpClient) {}

  // -------------------------------------------------------
  // GET ALL
  // -------------------------------------------------------
  getAll(pageNumber: number = 1, pageSize: number = 10): Observable<PagedResult<Client>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Client>>(this.baseUrl, { params });
  }

  // -------------------------------------------------------
  // GET BY ID
  // -------------------------------------------------------
  getById(id: string): Observable<Client> {
    return this.http.get<Client>(`${this.baseUrl}/${id}`);
  }

  // -------------------------------------------------------
  // GET PAGED BY TYPE
  // -------------------------------------------------------
  getPagedByType(type: ClientType, pageNumber: number = 1, pageSize: number = 10): Observable<PagedResult<Client>> {
    const params = new HttpParams()
      .set('type', type)
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Client>>(`${this.baseUrl}/by-type`, { params });
  }

  // -------------------------------------------------------
  // GET DELETED
  // -------------------------------------------------------
  getDeleted(pageNumber: number = 1, pageSize: number = 10): Observable<PagedResult<Client>> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize);
    return this.http.get<PagedResult<Client>>(`${this.baseUrl}/deleted`, { params });
  }

  // -------------------------------------------------------
  // STATS
  // -------------------------------------------------------
  getStats(): Observable<ClientStatsDto> {
    return this.http.get<any>(`${this.baseUrl}/stats`).pipe(
          map(res => ({
            TotalCount: res.totalCount,
            ActiveCount: res.activeCount,
            DeletedCount: res.deletedCount
          })));
  }

  // -------------------------------------------------------
  // CREATE
  // -------------------------------------------------------
  create(request: CreateClientRequest): Observable<Client> {
    return this.http.post<Client>(this.baseUrl, request);
  }

  // -------------------------------------------------------
  // UPDATE
  // -------------------------------------------------------
  update(id: string, request: UpdateClientRequest): Observable<Client> {
    return this.http.put<Client>(`${this.baseUrl}/${id}`, request);
  }

  // -------------------------------------------------------
  // DELETE
  // -------------------------------------------------------
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  // -------------------------------------------------------
  // RESTORE
  // -------------------------------------------------------
  restore(id: string): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/restore/${id}`, {});
  }
}
