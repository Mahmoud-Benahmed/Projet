import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment';

// ── Enums ────────────────────────────────────────────────────────────────
export enum PaymentMethod {
  Cash         = 'Cash',
  BankTransfer = 'BankTransfer',
  Check        = 'Check',
  CreditCard   = 'CreditCard',
  Other        = 'Other',
}

export enum PaymentStatus {
  Pending   = 'Pending',
  Completed = 'Completed',
  Failed    = 'Failed',
  Refunded  = 'Refunded',
}

export enum FeeType {
  Percentage  = 'Percentage',
  FixedPerDay = 'FixedPerDay',
}

// ── DTOs ─────────────────────────────────────────────────────────────────
export interface PaymentDto {
  id: string;
  invoiceId: string;
  clientId: string;
  amount: number;
  method: PaymentMethod;
  status: PaymentStatus;
  lateFeeApplied: number;
  isDeleted: boolean;
  paymentDate: string;
  createdAt: string;
}

export interface CreatePaymentDto {
  invoiceId: string;
  amount: number;
  method: PaymentMethod;
  paymentDate: string;
}

export interface UpdatePaymentDto {
  amount: number;
  method: PaymentMethod;
  paymentDate: string;
}

export interface PaymentSummaryDto {
  invoiceId: string;
  totalTTC: number;
  totalPaid: number;
  remainingAmount: number;
  invoiceStatus: string;
  lateFeeAmount: number;
  payments: PaymentDto[];
}

export interface PaymentStatsDto {
  totalPayments: number;
  totalCompleted: number;
  totalPending: number;
  totalFailed: number;
  totalRevenue: number;
}

export interface LateFeePolicyDto {
  id: string;
  feePercentage: number;
  feeType: FeeType;
  gracePeriodDays: number;
  isActive: boolean;
  createdAt: string;
}

export interface CreateLateFeePolicyDto {
  feePercentage: number;
  feeType: FeeType;
  gracePeriodDays: number;
}

export interface UpdateLateFeePolicyDto {
  feePercentage: number;
  feeType: FeeType;
  gracePeriodDays: number;
}

@Injectable({ providedIn: 'root' })
export class PaymentService {

  private readonly base       = `${environment.apiUrl}${environment.routes.payments}`;
  private readonly invBase    = `${environment.apiUrl}${environment.routes.invoices}`;
  private readonly policyBase = `${environment.apiUrl}/late-fee-policies`;

  constructor(private http: HttpClient) {}

  // ── Payments: GET ────────────────────────────────────────────────────
  getAll(): Observable<PaymentDto[]> {
    return this.http.get<PaymentDto[]>(this.base);
  }

  getById(id: string): Observable<PaymentDto> {
    return this.http.get<PaymentDto>(`${this.base}/${id}`);
  }

  getByInvoiceId(invoiceId: string): Observable<PaymentDto[]> {
    return this.http.get<PaymentDto[]>(`${this.base}/invoice/${invoiceId}`);
  }

  getByClientId(clientId: string): Observable<PaymentDto[]> {
    return this.http.get<PaymentDto[]>(`${this.base}/client/${clientId}`);
  }

  getByStatus(status: PaymentStatus): Observable<PaymentDto[]> {
    return this.http.get<PaymentDto[]>(`${this.base}/status/${status}`);
  }

  getStats(): Observable<PaymentStatsDto> {
    return this.http.get<PaymentStatsDto>(`${this.base}/stats`);
  }

  // ── Invoice-scoped ───────────────────────────────────────────────────
  /**
   * Full payment summary: totalTTC, totalPaid, remainingAmount,
   * lateFeeAmount, invoiceStatus, and the full list of payments.
   * Backend computes SUM(payments) and decides if invoice is PAID.
   */
  getPaymentSummary(invoiceId: string): Observable<PaymentSummaryDto> {
    return this.http.get<PaymentSummaryDto>(`${this.invBase}/${invoiceId}/payment-summary`);
  }

  getPaymentsByInvoice(invoiceId: string): Observable<PaymentDto[]> {
    return this.http.get<PaymentDto[]>(`${this.invBase}/${invoiceId}/payments`);
  }

  // ── Payments: COMMAND ────────────────────────────────────────────────
  /**
   * Create a partial or full payment.
   * Backend logic (in CreateAsync / UpdateAsync):
   *   1. Load invoice, check status (must be UNPAID)
   *   2. If overdue → compute & attach late fee via active LateFeePolicy
   *   3. Save payment
   *   4. SUM all payments for invoiceId
   *   5. If SUM >= invoice.totalTTC + lateFee → invoice.Status = PAID
   *      + publish Kafka event consumed by Invoice service
   */
  create(dto: CreatePaymentDto): Observable<PaymentDto> {
    return this.http.post<PaymentDto>(this.base, dto);
  }

  update(id: string, dto: UpdatePaymentDto): Observable<void> {
    return this.http.put<void>(`${this.base}/update/${id}`, dto);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  restore(id: string): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}/restore`, {});
  }

  // ── Late Fee Policies ────────────────────────────────────────────────
  getAllPolicies(): Observable<LateFeePolicyDto[]> {
    return this.http.get<LateFeePolicyDto[]>(this.policyBase);
  }

  getActivePolicy(): Observable<LateFeePolicyDto> {
    return this.http.get<LateFeePolicyDto>(`${this.policyBase}/active`);
  }

  getPolicyById(id: string): Observable<LateFeePolicyDto> {
    return this.http.get<LateFeePolicyDto>(`${this.policyBase}/${id}`);
  }

  createPolicy(dto: CreateLateFeePolicyDto): Observable<LateFeePolicyDto> {
    return this.http.post<LateFeePolicyDto>(this.policyBase, dto);
  }

  updatePolicy(id: string, dto: UpdateLateFeePolicyDto): Observable<void> {
    return this.http.put<void>(`${this.policyBase}/update/${id}`, dto);
  }

  activatePolicy(id: string): Observable<void> {
    return this.http.put<void>(`${this.policyBase}/${id}/activate`, {});
  }

  deletePolicy(id: string): Observable<void> {
    return this.http.delete<void>(`${this.policyBase}/${id}`);
  }

  // ── Display Helpers ──────────────────────────────────────────────────
  methodLabel(method: PaymentMethod): string {
    const map: Record<PaymentMethod, string> = {
      [PaymentMethod.Cash]:         'PAYMENTS.METHOD.CASH',
      [PaymentMethod.BankTransfer]: 'PAYMENTS.METHOD.BANK_TRANSFER',
      [PaymentMethod.Check]:        'PAYMENTS.METHOD.CHECK',
      [PaymentMethod.CreditCard]:   'PAYMENTS.METHOD.CREDIT_CARD',
      [PaymentMethod.Other]:        'PAYMENTS.METHOD.OTHER',
    };
    return map[method] ?? method;
  }

  methodIcon(method: PaymentMethod): string {
    const map: Record<PaymentMethod, string> = {
      [PaymentMethod.Cash]:         'payments',
      [PaymentMethod.BankTransfer]: 'account_balance',
      [PaymentMethod.Check]:        'receipt',
      [PaymentMethod.CreditCard]:   'credit_card',
      [PaymentMethod.Other]:        'more_horiz',
    };
    return map[method] ?? 'payments';
  }

  statusBadgeClass(status: PaymentStatus): Record<string, boolean> {
    return {
      'badge--green': status === PaymentStatus.Completed,
      'badge--amber': status === PaymentStatus.Pending,
      'badge--red':   status === PaymentStatus.Failed,
      'badge--blue':  status === PaymentStatus.Refunded,
    };
  }

  feeTypeLabel(feeType: FeeType): string {
    return feeType === FeeType.Percentage
      ? 'PAYMENTS.FEE_TYPE.PERCENTAGE'
      : 'PAYMENTS.FEE_TYPE.FIXED_PER_DAY';
  }

  /** Client-side overdue hint only — backend is authoritative */
  isOverdue(dueDate: string): boolean {
    if (!dueDate) return false;
    const due = new Date(dueDate);
    const now = new Date();
    due.setHours(0, 0, 0, 0);
    now.setHours(0, 0, 0, 0);
    return now > due;
  }

  /** Progress percentage: how much has been paid toward totalTTC */
  paymentProgress(totalPaid: number, totalTTC: number): number {
    if (!totalTTC || totalTTC === 0) return 0;
    return Math.min(100, Math.round((totalPaid / totalTTC) * 100));
  }
}