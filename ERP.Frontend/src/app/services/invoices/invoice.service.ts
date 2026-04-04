import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../environment';

// ── DTOs ─────────────────────────────────────────────

export interface InvoiceItemDto {
  id: string;
  articleId: string;
  articleName: string;
  articleBarCode: string;
  quantity: number;
  uniPriceHT: number;
  taxRate: number;
  totalHT: number;
  totalTTC: number;
}

export interface InvoiceDto {
  id: string;
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  totalHT: number;
  totalTVA: number;
  totalTTC: number;
  status: 'DRAFT' | 'UNPAID' | 'PAID' | 'CANCELLED';
  clientId: string;
  clientFullName: string;
  clientAddress: string;
  additionalNotes: string | null;
  items: InvoiceItemDto[];
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateInvoiceDto {
  invoiceNumber: string;
  invoiceDate: string;
  dueDate: string;
  clientId: string;
  clientFullName: string;
  clientAddress: string;
  additionalNotes: string | null;
  items: Array<{
    articleId: string;
    articleName: string;
    articleBarCode: string;
    quantity: number;
    uniPriceHT: number;
    taxRate: number;
  }>;
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
}

// ── SERVICE ──────────────────────────────────────────

@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  // Uses the dedicated invoice microservice on port 5037
  private readonly baseUrl = `${environment.apiUrl}${environment.routes.invoices}`;

  constructor(private readonly http: HttpClient) { }

  // ── Helpers ───────────────────────────────────────

  private paginate<T>(items: T[], page: number, size: number): PagedResultDto<T> {
    const start = (page - 1) * size;
    return { items: items.slice(start, start + size), totalCount: items.length };
  }

  // ── GET ────────────────────────────────────────────

  getAll(pageNumber = 1, pageSize = 10): Observable<PagedResultDto<InvoiceDto>> {
    return this.http
      .get<InvoiceDto[]>(this.baseUrl)
      .pipe(map(items => this.paginate(items.filter(i => !i.isDeleted), pageNumber, pageSize)));
  }

  getDeleted(pageNumber = 1, pageSize = 10): Observable<PagedResultDto<InvoiceDto>> {
    return this.http
      .get<InvoiceDto[]>(this.baseUrl, { params: new HttpParams().set('includeDeleted', 'true') })
      .pipe(map(items => this.paginate(items.filter(i => i.isDeleted), pageNumber, pageSize)));
  }

  getById(id: string): Observable<InvoiceDto> {
    return this.http.get<InvoiceDto>(`${this.baseUrl}/${id}`);
  }

  getByStatus(status: string, pageNumber = 1, pageSize = 10): Observable<PagedResultDto<InvoiceDto>> {
    return this.http
      .get<InvoiceDto[]>(`${this.baseUrl}/status/${status}`)
      .pipe(map(items => this.paginate(items, pageNumber, pageSize)));
  }

  getByClientId(clientId: string, pageNumber = 1, pageSize = 10): Observable<PagedResultDto<InvoiceDto>> {
    return this.http
      .get<InvoiceDto[]>(`${this.baseUrl}/client/${clientId}`)
      .pipe(map(items => this.paginate(items, pageNumber, pageSize)));
  }

  // ── CREATE ─────────────────────────────────────────

  create(dto: CreateInvoiceDto): Observable<InvoiceDto> {
    return this.http.post<InvoiceDto>(this.baseUrl, dto);
  }

  // ── ITEM MANAGEMENT ────────────────────────────────

  addItem(invoiceId: string, item: any): Observable<InvoiceDto> {
    return this.http.post<InvoiceDto>(`${this.baseUrl}/${invoiceId}/items`, item);
  }

  removeItem(invoiceId: string, itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${invoiceId}/items/${itemId}`);
  }

  // ── LIFECYCLE ──────────────────────────────────────

  finalize(invoiceId: string): Observable<InvoiceDto> {
    return this.http.put<InvoiceDto>(`${this.baseUrl}/${invoiceId}/finalize`, {});
  }

  markAsPaid(invoiceId: string): Observable<InvoiceDto> {
    return this.http.put<InvoiceDto>(`${this.baseUrl}/${invoiceId}/pay`, {});
  }

  cancel(invoiceId: string): Observable<InvoiceDto> {
    return this.http.put<InvoiceDto>(`${this.baseUrl}/${invoiceId}/cancel`, {});
  }

  // ── DELETE / RESTORE ───────────────────────────────

  delete(invoiceId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${invoiceId}`);
  }

  restore(invoiceId: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${invoiceId}/restore`, {});
  }
}