import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, firstValueFrom } from 'rxjs';
import { environment } from '../environment'; 
import { TranslateService } from '@ngx-translate/core';

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
  invoiceDate: string;
  dueDate: string;
  clientId: string;
  additionalNotes: string | null;
  items: Array<{
    articleId: string;
    quantity: number;
    uniPriceHT: number;
    taxRate: number;
  }>;
}

export interface ClientRevenueDto {
  clientId: string;
  clientFullName: string;
  invoiceCount: number;
  revenueTTC: number;
}

export interface MonthlyStatsDto {
  year: number;
  month: number;
  issuedCount: number;
  paidCount: number;
  issuedTTC: number;
  paidTTC: number;
}

export interface InvoiceStatsDto {
  totalInvoices: number;
  draftCount: number;
  unpaidCount: number;
  paidCount: number;
  cancelledCount: number;
  deletedCount: number;
  overdueCount: number;
  totalRevenueHT: number;
  totalRevenueTTC: number;
  totalTVACollected: number;
  outstandingHT: number;
  outstandingTTC: number;
  overdueHT: number;
  overdueTTC: number;
  averageInvoiceValueHT: number;
  averagePaymentDays: number;
  topClients: ClientRevenueDto[];
  monthlyBreakdown: MonthlyStatsDto[];
}

export interface PagedResultDto<T> {
  items: T[];
  totalCount: number;
}

export interface InvoiceValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  discountedTotal?: number;
  originalTotal?: number;
  discountApplied?: number;
  discountRate?: number;
}

export interface UpdateInvoiceDto extends CreateInvoiceDto{}

// ── SERVICE ──────────────────────────────────────────

@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  // Uses the dedicated invoice microservice on port 5037
  private readonly baseUrl = `${environment.apiUrl}${environment.routes.invoices}`;

  constructor(private readonly http: HttpClient, 
              private readonly translate: TranslateService) { }


  // ── Helpers ───────────────────────────────────────
  private t(key: string, params?: any): string {
    return this.translate.instant(key, params);
  }

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

  // ✅ Get client total TTC (all invoices - for reporting)
  getClientTotalTTC(clientId: string): Observable<number> {
    return this.http.get<InvoiceDto[]>(`${this.baseUrl}/client/${clientId}`).pipe(
      map(invoices => {
        const totalTTC = invoices.reduce((sum, invoice) => sum + invoice.totalTTC, 0);
        return totalTTC;
      })
    );
  }

  // ✅ Get client outstanding balance (UNPAID invoices only - for credit limit)
  getClientOutstandingBalance(clientId: string): Observable<number> {
    return this.http.get<InvoiceDto[]>(`${this.baseUrl}/client/${clientId}`).pipe(
      map(invoices => {
        const outstandingTTC = invoices
          .filter(invoice => invoice.status === 'UNPAID')
          .reduce((sum, invoice) => sum + invoice.totalTTC, 0);
        return outstandingTTC;
      })
    );
  }

  getStats(topClientsCount = 5): Observable<InvoiceStatsDto> {
    const params = new HttpParams().set('topClientsCount', topClientsCount);
    return this.http.get<InvoiceStatsDto>(`${this.baseUrl}/stats`, { params });
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

  // ── DISCOUNT & VALIDATION METHODS ──────────────────

  // Calculate discount from client categories
  calculateBulkDiscount(client: any): { discountRate: number; applies: boolean } {
    if (!client.categories || client.categories.length === 0) {
      return { discountRate: 0, applies: false };
    }

    const bulkCategories = client.categories.filter((cat: any) => cat.useBulkPricing === true);
    
    if (bulkCategories.length === 0) {
      return { discountRate: 0, applies: false };
    }

    const highestDiscount = Math.max(...bulkCategories.map((cat: any) => cat.discountRate || 0));
    
    return { 
      discountRate: highestDiscount, 
      applies: highestDiscount > 0 
    };
  }

  // Apply discount to items
  applyDiscountToItems(
    items: CreateInvoiceDto['items'],
    discountRate: number
  ): CreateInvoiceDto['items'] {
    if (discountRate <= 0) return items;
    
    const discountMultiplier = 1 - (discountRate / 100);
    
    return items.map(item => ({
      ...item,
      uniPriceHT: item.uniPriceHT * discountMultiplier
    }));
  }

  // In your invoice.service.ts
  calculateDiscountedTotals(
    items: CreateInvoiceDto['items'],
    discountRate: number
  ): { originalTotalHT: number; originalTotalTTC: number; discountedTotalHT: number; discountedTotalTTC: number; discountAmount: number } {
    let originalTotalHT = 0;
    let originalTotalTTC = 0;

    for (const item of items) {
      const itemTotalHT = item.quantity * item.uniPriceHT;
      // TaxRate is already percentage (e.g., 10.1 for 10.1%)
      const taxRateDecimal = item.taxRate / 100;
      const itemTotalTTC = itemTotalHT * (1 + taxRateDecimal);
      originalTotalHT += itemTotalHT;
      originalTotalTTC += itemTotalTTC;
    }

    const discountMultiplier = 1 - (discountRate / 100);
    const discountedTotalHT = originalTotalHT * discountMultiplier;
    const discountedTotalTTC = originalTotalTTC * discountMultiplier;
    const discountAmount = originalTotalTTC - discountedTotalTTC;

    return {
      originalTotalHT,
      originalTotalTTC,
      discountedTotalHT,
      discountedTotalTTC,
      discountAmount
    };
  }

  // Validate credit limit
  validateCreditLimit(
    client: any,
    invoiceTotalTTC: number,
    currentOutstanding: number
  ): { hasSufficientCredit: boolean; currentUsage: number; remainingCredit: number; message: string } {
    // No credit limit set
    if (!client.creditLimit || client.creditLimit <= 0) {
      return {
        hasSufficientCredit: true,
        currentUsage: currentOutstanding,
        remainingCredit: Infinity,
        message: 'No credit limit restrictions apply'
      };
    }
    
    const totalWithOutstanding = currentOutstanding + invoiceTotalTTC;
    const hasSufficientCredit = totalWithOutstanding <= client.creditLimit;
    const remainingCredit = Math.max(0, client.creditLimit - currentOutstanding);
    
    return {
        hasSufficientCredit,
        currentUsage: currentOutstanding,
        remainingCredit,
        message: hasSufficientCredit
          ? this.t('INVOICES.ERRORS.HAS_SUFFICIENT_CREDIT', { remainingCredit: remainingCredit.toFixed(2) })
          : this.t('INVOICES.ERRORS.INSUFFICIENT_CREDIT', {
        creditLimit: client.creditLimit.toFixed(2),
        currentOutstanding: currentOutstanding.toFixed(2),
        invoiceTotal: invoiceTotalTTC.toFixed(2)
      })
    }
  }

  // Full validation before submission
  async validateInvoiceBeforeSubmission(
    client: any,
    items: CreateInvoiceDto['items']
  ): Promise<InvoiceValidationResult> {
    const errors: string[] = [];
    const warnings: string[] = [];

    // Check if client is blocked
    if (client.isBlocked) {
      errors.push('Client is blocked and cannot create invoices');
      return { isValid: false, errors, warnings };
    }

    // Check if client is deleted
    if (client.isDeleted) {
      errors.push('Client account is deleted');
      return { isValid: false, errors, warnings };
    }

    // Calculate bulk discount
    const { discountRate, applies } = this.calculateBulkDiscount(client);

    // Calculate totals
    const { originalTotalTTC, discountedTotalTTC, discountAmount } = this.calculateDiscountedTotals(items, discountRate);

    if (applies && discountRate > 0) {
      warnings.push(`Bulk discount of ${discountRate}% applied. You save: ${discountAmount.toFixed(2)} TND`);
    }

    // Get current outstanding balance
    try {
      const currentOutstanding = await firstValueFrom(this.getClientOutstandingBalance(client.id));

      // Validate credit limit
      const creditCheck = this.validateCreditLimit(client, discountedTotalTTC, currentOutstanding || 0);

      if (!creditCheck.hasSufficientCredit) {
        errors.push(creditCheck.message);
      } else if (creditCheck.remainingCredit !== Infinity) {
        warnings.push(creditCheck.message);
      }
    } catch (error) {
      console.error('Credit limit verification failed:', error);
      errors.push('Unable to verify credit limit. Please try again.');
    }

    return {
      isValid: errors.length === 0,
      errors,
      warnings,
      discountedTotal: discountedTotalTTC,
      originalTotal: originalTotalTTC,
      discountApplied: discountAmount,
      discountRate: applies ? discountRate : 0
    };
  }


  update(id: string, dto: UpdateInvoiceDto): Observable<InvoiceDto> {
    return this.http.put<InvoiceDto>(`${this.baseUrl}/update/${id}`, dto);
  }
  
}