import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, DestroyRef, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { AuthService, PRIVILEGES } from '../../../services/auth/auth.service';
import {
  PaymentService, LateFeePolicyDto,
  CreateLateFeePolicyDto, UpdateLateFeePolicyDto, FeeType
} from '../../../services/payments/payment.service';
import { ModalComponent } from '../../modal/modal';

@Component({
  selector: 'app-late-fee-policies',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule,
    MatIconModule, MatButtonModule, MatTooltipModule, MatDialogModule,
    TranslatePipe,
  ],
  templateUrl: './late-fee-policies.html',
  styleUrl: './late-fee-policies.scss',
})
export class LateFeePoliciesComponent implements OnInit {

  private readonly destroyRef = inject(DestroyRef);
  private readonly translate  = inject(TranslateService);
  private readonly cdr        = inject(ChangeDetectorRef);

  readonly PRIVILEGES = PRIVILEGES;
  readonly FeeType    = FeeType;
  readonly feeTypes   = Object.values(FeeType);

  // ── Data ────────────────────────────────────────────────────────────
  policies:       LateFeePolicyDto[] = [];
  activePolicy:   LateFeePolicyDto | null = null;

  // ── Inline form ──────────────────────────────────────────────────────
  policyForm!:    FormGroup;
  editingId:      string | null = null;
  formOpen                      = false;

  // ── Alerts ──────────────────────────────────────────────────────────
  errors: string[]            = [];
  successMessage: string|null = null;

  constructor(
    public  authService:    AuthService,
    private paymentService: PaymentService,
    private dialog:         MatDialog,
    private fb:             FormBuilder,
  ) {}

  ngOnInit(): void {
    this.buildForm();
    this.load();
  }

  private buildForm(): void {
    this.policyForm = this.fb.group({
      feePercentage:  [null, [Validators.required, Validators.min(0.01)]],
      feeType:        [FeeType.Percentage, Validators.required],
      gracePeriodDays:[0,   [Validators.required, Validators.min(0)]],
    });
  }

  load(): void {
    this.paymentService.getAllPolicies().subscribe({
      next: policies => {
        this.policies     = policies;
        this.activePolicy = policies.find(p => p.isActive) ?? null;
        this.cdr.markForCheck();
      },
      error: () => this.flash('error', this.translate.instant('PAYMENTS.POLICIES.ERRORS.LOAD_FAILED')),
    });
  }

  // ── Form open/close ──────────────────────────────────────────────────
  openCreate(): void {
    this.editingId = null;
    this.policyForm.reset({ feePercentage: null, feeType: FeeType.Percentage, gracePeriodDays: 0 });
    this.formOpen = true;
  }

  openEdit(policy: LateFeePolicyDto): void {
    this.editingId = policy.id;
    this.policyForm.patchValue({
      feePercentage:   policy.feePercentage,
      feeType:         policy.feeType,
      gracePeriodDays: policy.gracePeriodDays,
    });
    this.formOpen = true;
  }

  closeForm(): void {
    this.formOpen  = false;
    this.editingId = null;
    this.policyForm.reset();
  }

  // ── CRUD ────────────────────────────────────────────────────────────
  submit(): void {
    if (this.policyForm.invalid) {
      this.policyForm.markAllAsTouched();
      return;
    }
    const dto: CreateLateFeePolicyDto = this.policyForm.value;

    if (this.editingId) {
      this.paymentService.updatePolicy(this.editingId, dto as UpdateLateFeePolicyDto).subscribe({
        next:  () => { this.flash('success', this.translate.instant('PAYMENTS.POLICIES.SUCCESS.UPDATED')); this.closeForm(); this.load(); },
        error: () => this.flash('error', this.translate.instant('PAYMENTS.POLICIES.ERRORS.UPDATE_FAILED')),
      });
    } else {
      this.paymentService.createPolicy(dto).subscribe({
        next:  () => { this.flash('success', this.translate.instant('PAYMENTS.POLICIES.SUCCESS.CREATED')); this.closeForm(); this.load(); },
        error: () => this.flash('error', this.translate.instant('PAYMENTS.POLICIES.ERRORS.CREATE_FAILED')),
      });
    }
  }

  activate(policy: LateFeePolicyDto): void {
    this.paymentService.activatePolicy(policy.id).subscribe({
      next:  () => { this.flash('success', this.translate.instant('PAYMENTS.POLICIES.SUCCESS.ACTIVATED')); this.load(); },
      error: () => this.flash('error', this.translate.instant('PAYMENTS.POLICIES.ERRORS.ACTIVATE_FAILED')),
    });
  }

  delete(policy: LateFeePolicyDto): void {
    const ref = this.dialog.open(ModalComponent, {
      width: '420px',
      data: {
        icon: 'delete', iconColor: 'warn',
        title:       this.translate.instant('PAYMENTS.POLICIES.DIALOG.DELETE_TITLE'),
        message:     this.translate.instant('PAYMENTS.POLICIES.DIALOG.DELETE_MESSAGE'),
        confirmText: this.translate.instant('PAYMENTS.POLICIES.DIALOG.DELETE_CONFIRM'),
        cancelText:  this.translate.instant('COMMON.CANCEL'),
        showCancel:  true,
      },
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(confirmed => {
      if (!confirmed) return;
      this.paymentService.deletePolicy(policy.id).subscribe({
        next:  () => { this.flash('success', this.translate.instant('PAYMENTS.POLICIES.SUCCESS.DELETED')); this.load(); },
        error: () => this.flash('error', this.translate.instant('PAYMENTS.POLICIES.ERRORS.DELETE_FAILED')),
      });
    });
  }

  // ── Helpers ──────────────────────────────────────────────────────────
  feeLabel(feeType: FeeType): string { return this.paymentService.feeTypeLabel(feeType); }

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
  trackById(_: number, item: { id: string }) { return item.id; }
}