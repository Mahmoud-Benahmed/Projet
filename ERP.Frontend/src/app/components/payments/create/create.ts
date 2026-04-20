import { CommonModule, Location } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ActivatedRoute } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';

import { AuthService } from '../../../services/auth/auth.service';
import {
  PaymentService, PaymentSummaryDto,
  PaymentMethod, FeeType, LateFeePolicyDto, CreatePaymentDto
} from '../../../services/payments/payment.service';
import { InvoiceService, InvoiceDto } from '../../../services/invoice.service';
import { HttpError } from '../../../interfaces/ErrorDto';

@Component({
  selector: 'app-create-payment',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatIconModule, MatButtonModule, MatTooltipModule,
    TranslatePipe,
  ],
  templateUrl: './create.html',
  styleUrl: './create.scss',
})
export class CreatePaymentComponent implements OnInit, OnDestroy {

  private readonly destroyRef = inject(DestroyRef);
  private readonly translate  = inject(TranslateService);
  private readonly cdr        = inject(ChangeDetectorRef);
  private readonly location   = inject(Location);

  readonly PaymentMethod = PaymentMethod;
  readonly FeeType       = FeeType;
  readonly methods       = Object.values(PaymentMethod);

  // ── Form ────────────────────────────────────────────────────────────
  paymentForm!: FormGroup;
  isSubmitting = false;

  // ── Invoice search / selection ───────────────────────────────────────
  invoices:          InvoiceDto[]          = [];
  filteredInvoices:  InvoiceDto[]          = [];
  invoiceSearch                            = '';
  selectedInvoice:   InvoiceDto | null     = null;
  paymentSummary:    PaymentSummaryDto | null = null;
  summaryLoading                           = false;

  // ── Active late fee policy ───────────────────────────────────────────
  activePolicy: LateFeePolicyDto | null = null;

  // ── Overdue state ────────────────────────────────────────────────────
  isOverdue        = false;
  estimatedLateFee = 0;

  // ── Alerts ──────────────────────────────────────────────────────────
  errors: string[]            = [];
  successMessage: string|null = null;

  constructor(
    public  authService:    AuthService,
    private paymentService: PaymentService,
    private invoiceService: InvoiceService,
    private fb:             FormBuilder,
    private route:          ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.load();

    // Pre-select invoice if navigated from invoice view
    const invoiceId = this.route.snapshot.queryParamMap.get('invoiceId');
    if (invoiceId) {
      // Will be applied once invoices load
      this._preSelectInvoiceId = invoiceId;
    }
  }

  private _preSelectInvoiceId: string | null = null;

  private buildForm(): void {
    this.paymentForm = this.fb.group({
      invoiceId:   ['', Validators.required],
      amount:      [null, [Validators.required, Validators.min(0.01)]],
      method:      [PaymentMethod.Cash, Validators.required],
      paymentDate: [new Date().toISOString().split('T')[0], Validators.required],
    });

    // When amount changes → recompute late fee estimate
    this.paymentForm.get('amount')?.valueChanges.subscribe(() => this.computeEstimatedFee());
  }

  private load(): void {
    forkJoin({
      invoices: this.invoiceService.getAll(1, 1000).pipe(catchError(() => of({ items: [] }))),
      policy:   this.paymentService.getActivePolicy().pipe(catchError(() => of(null))),
    }).subscribe({
      next: ({ invoices, policy }) => {
        // Only show UNPAID invoices
        this.invoices      = (invoices.items ?? []).filter((inv: InvoiceDto) => inv.status === 'UNPAID');
        this.activePolicy  = policy;

        if (this._preSelectInvoiceId) {
          const inv = this.invoices.find(i => i.id === this._preSelectInvoiceId);
          if (inv) this.selectInvoice(inv);
        }
        this.cdr.markForCheck();
      },
    });
  }

  // ── Invoice search ───────────────────────────────────────────────────
  filterInvoices(query: string): void {
    if (!query || query.length < 1) { this.filteredInvoices = []; return; }
    const q = query.toLowerCase();
    this.filteredInvoices = this.invoices
      .filter(i =>
        i.invoiceNumber?.toLowerCase().includes(q) ||
        i.clientFullName?.toLowerCase().includes(q),
      )
      .slice(0, 8);
  }

  selectInvoice(invoice: InvoiceDto): void {
    this.selectedInvoice    = invoice;
    this.invoiceSearch      = `${invoice.invoiceNumber} — ${invoice.clientFullName}`;
    this.filteredInvoices   = [];
    this.paymentForm.patchValue({ invoiceId: invoice.id });

    // Check overdue
    this.isOverdue = this.paymentService.isOverdue(invoice.dueDate);

    // Load payment summary (totalPaid so far, remaining, any late fee)
    this.loadSummary(invoice.id);

    // Pre-fill amount with remaining balance (set after summary loads)
  }

  clearInvoice(): void {
    this.selectedInvoice  = null;
    this.paymentSummary   = null;
    this.invoiceSearch    = '';
    this.filteredInvoices = [];
    this.isOverdue        = false;
    this.estimatedLateFee = 0;
    this.paymentForm.patchValue({ invoiceId: '', amount: null });
  }

  private loadSummary(invoiceId: string): void {
    this.summaryLoading = true;
    this.paymentService.getPaymentSummary(invoiceId).subscribe({
      next: summary => {
        this.paymentSummary = summary;
        this.summaryLoading = false;
        // Pre-fill amount = remaining balance
        this.paymentForm.patchValue({ amount: summary.remainingAmount > 0 ? summary.remainingAmount : null });
        this.computeEstimatedFee();
        this.cdr.markForCheck();
      },
      error: () => {
        this.summaryLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  // ── Late fee estimate (client-side preview only — backend is authoritative) ──
  computeEstimatedFee(): void {
    if (!this.isOverdue || !this.activePolicy || !this.paymentSummary) {
      this.estimatedLateFee = 0;
      return;
    }
    const amount = this.paymentForm.get('amount')?.value ?? 0;
    if (this.activePolicy.feeType === FeeType.Percentage) {
      this.estimatedLateFee = amount * (this.activePolicy.feePercentage / 100);
    } else {
      // FixedPerDay: days overdue × feePercentage
      const dueDate    = new Date(this.selectedInvoice!.dueDate);
      const today      = new Date();
      const daysOver   = Math.max(0, Math.floor((today.getTime() - dueDate.getTime()) / 86400000));
      const daysAfterGrace = Math.max(0, daysOver - this.activePolicy.gracePeriodDays);
      this.estimatedLateFee = daysAfterGrace * this.activePolicy.feePercentage;
    }
  }

  // ── Progress bar ────────────────────────────────────────────────────
  get progressPercent(): number {
    if (!this.paymentSummary) return 0;
    return this.paymentService.paymentProgress(
      this.paymentSummary.totalPaid,
      this.paymentSummary.totalTTC + this.paymentSummary.lateFeeAmount,
    );
  }

  // ── Submit ───────────────────────────────────────────────────────────
  submit(): void {
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      this.flash('error', this.translate.instant('VALIDATION.REQUIRED'));
      return;
    }

    const { invoiceId, amount, method, paymentDate } = this.paymentForm.value;

    // Guard: don't allow paying more than remaining
    if (this.paymentSummary && amount > this.paymentSummary.remainingAmount + this.paymentSummary.lateFeeAmount + 0.01) {
      this.flash('error', this.translate.instant('PAYMENTS.ERRORS.AMOUNT_EXCEEDS_REMAINING'));
      return;
    }

    this.isSubmitting = true;

    const dto: CreatePaymentDto = { invoiceId, amount: Number(amount), method, paymentDate };

    this.paymentService.create(dto).subscribe({
      next: () => {
        this.flash('success', this.translate.instant('PAYMENTS.SUCCESS.CREATED'));
        setTimeout(() => { this.cancel(); }, 2000);
      },
      error: (err) => {
        const msg = (err.error as HttpError)?.message || this.translate.instant('PAYMENTS.ERRORS.CREATE_FAILED');
        this.flash('error', msg);
        this.isSubmitting = false;
      },
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────────
  get pageTitle(): string { return 'PAYMENTS.TITLE_NEW'; }

  get canSubmit(): boolean {
    return this.paymentForm.valid && !this.isSubmitting && !!this.selectedInvoice;
  }

  methodLabel(method: PaymentMethod): string { return this.paymentService.methodLabel(method); }
  methodIcon(method: PaymentMethod):  string { return this.paymentService.methodIcon(method); }

  flash(type: 'success' | 'error', msg: string): void {
    if (type === 'success') {
      this.successMessage = msg;
      setTimeout(() => { this.successMessage = null; }, 3000);
    } else {
      this.errors = [msg];
      setTimeout(() => { this.errors = []; }, 4000);
    }
  }
  dismissError(): void { this.errors = []; }
  cancel(): void { this.location.back(); }
  ngOnDestroy(): void {}
}