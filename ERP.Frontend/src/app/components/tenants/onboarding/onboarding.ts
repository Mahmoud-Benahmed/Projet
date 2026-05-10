import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { TenantService } from '../../../services/tenant.service';
import { TranslateModule } from '@ngx-translate/core';
import { UserSettingsService } from '../../../services/user-settings.service';
@Component({
  selector: 'app-onboarding',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, TranslateModule],
  templateUrl: './onboarding.html',
  styleUrl: './onboarding.scss'
})
export class OnboardingComponent implements OnInit {
  form!: FormGroup;
  planId = '';
  planName = '';
  loading = false;
  error = '';
  success = false;
  currentStep = 1;
  totalSteps = 2;

  timezones = [
    'Africa/Tunis', 'Africa/Cairo', 'Europe/Paris',
    'Europe/London', 'America/New_York', 'Asia/Dubai'
  ];

  currencies = ['TND', 'EUR', 'USD', 'GBP', 'MAD', 'DZD'];
  locales = ['fr-TN', 'en-US', 'fr-FR', 'ar-TN'];

  constructor(
    private fb: FormBuilder,
    private tenantService: TenantService,
    private router: Router,
    private route: ActivatedRoute,
    public userSettings: UserSettingsService
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.planId = params['planId'] ?? '';
      this.planName = params['planName'] ?? '';
    });

    this.form = this.fb.group({
      // Step 1 — Company Info
      name: ['', [Validators.required, Validators.maxLength(150)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
      phone: ['', [Validators.required, Validators.maxLength(20)]],
      subdomainSlug: ['', [Validators.required, Validators.maxLength(100), Validators.pattern(/^[a-zA-Z0-9-]+$/)]],
      // Step 2 — Regional Settings
      currency: ['TND', Validators.required],
      locale: ['fr-TN', Validators.required],
      timezone: ['Africa/Tunis', Validators.required],
    });
  }

  get step1Valid() {
    return ['name', 'email', 'phone', 'subdomainSlug'].every(f => this.form.get(f)?.valid);
  }

  nextStep() {
    if (this.currentStep === 1 && this.step1Valid) this.currentStep = 2;
  }

  prevStep() {
    if (this.currentStep > 1) this.currentStep--;
  }

  submit() {
    if (this.form.invalid) return;
    this.loading = true;
    this.error = '';

    const payload = { ...this.form.value, planId: this.planId };
    this.tenantService.createTenant(payload).subscribe({
      next: () => {
        this.loading = false;
        this.success = true;
        setTimeout(() => this.router.navigate(['/login']), 2500);
      },
      error: (err) => {
        this.loading = false;
        this.error = err?.error?.message ?? 'Something went wrong. Please try again.';
      }
    });
  }

  getFieldError(field: string): string {
    const control = this.form.get(field);
    if (!control?.touched || !control.errors) return '';
    if (control.errors['required']) return 'This field is required';
    if (control.errors['email']) return 'Enter a valid email';
    if (control.errors['pattern']) return 'Only letters, numbers and hyphens allowed';
    if (control.errors['maxlength']) return 'Value is too long';
    return 'Invalid value';
  }
}