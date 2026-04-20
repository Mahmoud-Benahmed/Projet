import { CommonModule, Location } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../../services/auth/auth.service';
import {
  PaymentService, PaymentDto, UpdatePaymentDto, PaymentMethod, PaymentSummaryDto
} from '../../../services/payments/payment.service';
import { HttpError } from '../../../interfaces/ErrorDto';

@Component({
  selector: 'app-payments-edit',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatDialogModule,
    TranslatePipe,
  ],
  templateUrl: './edit.html',
  styleUrl: './edit.scss',
})
export class EditPaymentComponent implements OnInit, OnDestroy {
  private readonly destroyRef = inject(DestroyRef);
  private readonly translate  = inject(TranslateService);
  private readonly cdr        = inject(ChangeDetectorRef);
  private readonly location   = inject(Location);

  readonly PaymentMethod = PaymentMethod;
  readonly paymentMethods = Object.values(PaymentMethod);

  // ── Alerts ─────────────────────────────────────────────────────────────────
  errors: string[] = [];
  successMessage: string | null = null;

  paymentForm!: FormGroup;
  isSubmitting = false;

  selectedPayment: PaymentDto | null = null;
  paymentSummary: PaymentSummaryDto | null = null;
  paymentIdFromRoute: string | null = null;

  constructor(
    public authService: AuthService,
    private paymentService: PaymentService,
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.buildForm();

    this.paymentIdFromRoute = this.route.snapshot.paramMap.get('id');
    if (!this.paymentIdFromRoute) {
      this.cancel();
      return;
    }

    this.reload();
  }

  private buildForm(): void {
    this.paymentForm = this.fb.group({
      amount:      [null, [Validators.required, Validators.min(0.01)]],
      method:      [PaymentMethod.Cash, Validators.required],
      paymentDate: ['', Validators.required],
    });
  }

  reload(): void {
    this.paymentService.getById(this.paymentIdFromRoute!).subscribe({
      next: payment => {
        this.selectedPayment = payment;
        this.populateForm(payment);
        this.loadSummary(payment.invoiceId);
        this.cdr.markForCheck();
      },
      error: err => {
        const msg = (err.error as HttpError)?.message || this.translate.instant('PAYMENTS.ERRORS.LOAD_FAILED');
        this.flash('error', msg);
        this.cancel();
      },
    });
  }

  private populateForm(payment: PaymentDto): void {
    this.paymentForm.patchValue({
      amount:      payment.amount,
      method:      payment.method,
      paymentDate: payment.paymentDate?.split('T')[0] || '',
    });
    this.paymentForm.markAsPristine();
    this.cdr.detectChanges();
  }

  private loadSummary(invoiceId: string): void {
    this.paymentService.getPaymentSummary(invoiceId).subscribe({
      next: summary => {
        this.paymentSummary = summary;
        this.cdr.markForCheck();
      },
      error: () => { /* optional — skip */ },
    });
  }

  get canSubmit(): boolean {
    return this.paymentForm.valid && !this.isSubmitting;
  }

  getSubmitTooltip(): string {
    if (this.isSubmitting)        return this.translate.instant('COMMON.PROCESSING');
    if (this.paymentForm.invalid) return this.translate.instant('VALIDATION.REQUIRED');
    return '';
  }

  submit(): void {
    if (this.paymentForm.invalid) {
      this.flash('error', this.translate.instant('VALIDATION.REQUIRED'));
      return;
    }

    this.isSubmitting = true;
    const fv = this.paymentForm.value;

    const dto: UpdatePaymentDto = {
      amount:      Number(fv.amount),
      method:      fv.method,
      paymentDate: fv.paymentDate,
    };

    this.paymentService.update(this.paymentIdFromRoute!, dto).subscribe({
      next: () => {
        this.flash('success', this.translate.instant('PAYMENTS.SUCCESS.UPDATED'));
        setTimeout(() => {
          this.isSubmitting = false;
          this.router.navigate(['/payments', this.paymentIdFromRoute]);
        }, 1500);
      },
      error: err => {
        const msg = (err.error as HttpError)?.message || this.translate.instant('PAYMENTS.ERRORS.UPDATE_FAILED');
        this.flash('error', msg);
        this.isSubmitting = false;
      },
    });
  }

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