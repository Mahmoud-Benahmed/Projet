import { CommonModule, Location } from '@angular/common';
import { ChangeDetectorRef, Component, computed, DestroyRef, inject, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ClientResponseDto, ClientsService } from '../../../services/clients/clients.service';
import { StockItem, StockService } from '../../../services/stock.service';
import { AuthService, PRIVILEGES } from '../../../services/auth/auth.service';
import { InvoiceDto, InvoiceService } from '../../../services/invoice.service';
import { ArticleService, UnitEnum } from '../../../services/articles/articles.service';
import { catchError, firstValueFrom, forkJoin, map, Observable, of, tap } from 'rxjs';
import { HttpError } from '../../../interfaces/ErrorDto';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ModalComponent } from '../../modal/modal';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LoadingOverlayComponent } from "../../loading-overlay/loading-overlay";

@Component({
  selector: 'app-invoices-view',
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatDialogModule,
    TranslatePipe,
    RouterLink
],
  templateUrl: './view.html',
  styleUrl: './view.scss',
})
export class ViewInvoiceComponent implements OnInit, OnDestroy {
  private themeObserver: MutationObserver | null = null;
  private readonly destroyRef = inject(DestroyRef);
  private translate = inject(TranslateService);
  private cdr = inject(ChangeDetectorRef);
  private location= inject(Location);
  
  // ── Alerts ────────────────────────────────────────────────────────────────
  errors: string[] = [];
  successMessage: string | null = null

  selectedInvoice: InvoiceDto | null = null;
  invoiceIdFromRoute: string|null=null;

  readonly PRIVILEGES = PRIVILEGES;
  readonly units = UnitEnum;
  constructor(
      public authService: AuthService,
      private invoiceService: InvoiceService,
      private clientsService: ClientsService,
      private articleService: ArticleService,
      private fb: FormBuilder,
      private stock: StockService,
      private route: ActivatedRoute,
      private router: Router,
      private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    if(!this.authService.hasPrivilege(PRIVILEGES.INVOICES.VIEW_INVOICES)){
      this.cancel();
      return;
    }

    this.invoiceIdFromRoute = this.route.snapshot.paramMap.get('id');
    
    if (!this.invoiceIdFromRoute) {
      this.cancel();
      return; 
    }
    
    this.reload();
  }

  reload(): void {
    forkJoin({
      selectedInvoice: this.loadInvoice(this.invoiceIdFromRoute!),
    }).subscribe({
      next: () => {          
      },
      error: () => {
        this.cdr.markForCheck();
        this.cancel();
      }
    });
  }

  loadInvoice(invoiceId: string): Observable<InvoiceDto> {
    return this.invoiceService.getById(invoiceId).pipe(
        tap({
          next: (invoice) => {
            this.selectedInvoice = invoice;
          },
          error: (err) => {
            const errorMsg = (err.error as HttpError)?.message || this.translate.instant('INVOICES.ERRORS.LOAD_FAILED');
            this.flash('error', errorMsg);
          }
        })
    );
  }

  finalize(invoice: InvoiceDto): void {
    this.invoiceService.finalize(invoice.id).subscribe({
      next: (updated) => {
        if (this.selectedInvoice?.id === updated.id) {
          this.selectedInvoice = { ...updated }; // spread to trigger reference change
          this.cdr.markForCheck();
        }
        this.flash('success', this.translate.instant('INVOICES.SUCCESS.FINALIZED'));
        this.reload();
      },
      error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.FINALIZE_FAILED')),
    });
  }

  markAsPaid(invoice: InvoiceDto): void {
      this.invoiceService.markAsPaid(invoice.id).subscribe({
        next: (updated) => {
          if (this.selectedInvoice?.id === updated.id) {
            this.selectedInvoice = { ...updated }; // spread to trigger reference change
            this.cdr.markForCheck();
          }
          this.flash('success', this.translate.instant('INVOICES.SUCCESS.MARKED_PAID'));
          this.reload();
        },
        error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.MARK_PAID_FAILED')),
      });
    }
  
  cancelInvoice(invoice: InvoiceDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '420px',
      data: {
        icon: 'cancel', iconColor: 'warn',
        title: this.translate.instant('INVOICES.DIALOG.CANCEL_INVOICE_TITLE'),
        message: this.translate.instant('INVOICES.DIALOG.CANCEL_INVOICE_MESSAGE', { number: invoice.invoiceNumber }),
        confirmText: this.translate.instant('INVOICES.DIALOG.CANCEL_CONFIRM'),
        cancelText: this.translate.instant('INVOICES.DIALOG.GO_BACK'),
        showCancel: true,
      },
    });
    dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(confirmed => {
      if (!confirmed) return;
      this.invoiceService.cancel(invoice.id).subscribe({
        next: (updated) => {
          if (this.selectedInvoice?.id === updated.id) {
            this.selectedInvoice = { ...updated };
            this.cdr.markForCheck();
          }
          this.flash('success', this.translate.instant('INVOICES.SUCCESS.CANCELLED'));
          this.reload();
        },
        error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.CANCEL_FAILED')),
      });
    });
  }

  delete(invoice: InvoiceDto): void {
    const dialogRef = this.dialog.open(ModalComponent, {
      width: '420px',
      data: {
        icon: 'delete', iconColor: 'warn',
        title: this.translate.instant('INVOICES.DIALOG.DELETE_INVOICE_TITLE'),
        message: this.translate.instant('INVOICES.DIALOG.DELETE_INVOICE_MESSAGE', { number: invoice.invoiceNumber }),
        confirmText: this.translate.instant('INVOICES.DIALOG.DELETE_CONFIRM'),
        cancelText: this.translate.instant('COMMON.CANCEL'),
        showCancel: true,
      },
    });
    dialogRef.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(confirmed => {
      if (!confirmed) return;
      this.invoiceService.delete(invoice.id).subscribe({
        next: () => {
          this.flash('success', this.translate.instant('INVOICES.SUCCESS.DELETED'));
          setTimeout(()=>{
            this.cancel();
          }, 2000);
        },
        error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.DELETE_FAILED')),
      });
    });
  }

  restore(invoice: InvoiceDto): void {
    this.invoiceService.restore(invoice.id).subscribe({
      next: () => {
        this.flash('success', this.translate.instant('INVOICES.SUCCESS.RESTORED'));
        this.flash('success', this.translate.instant('INVOICES.SUCCESS.CREATED'));
        setTimeout(()=>{
            this.cancel();
          }, 2000);
      },
      error: () => this.flash('error', this.translate.instant('INVOICES.ERRORS.RESTORE_FAILED')),
    });
  }

  previewPdf(invoice: InvoiceDto): void {
    this.invoiceService.downloadInvoicePdf(invoice.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        window.open(url, '_blank');
        // optional: revoke after a delay to free memory
        setTimeout(() => window.URL.revokeObjectURL(url), 10000);
      },
      error: () => this.flash('error', 'PDF preview failed')
    });
  }

  downloadPdf(invoice: InvoiceDto): void {
    this.invoiceService.downloadInvoicePdf(invoice.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `Invoice_${invoice.invoiceNumber}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => this.flash('error', 'PDF generation failed')
    });
  }

  statusClass(status: string): Record<string, boolean> {
    return {
      'badge--amber': status === 'DRAFT',
      'badge--red':   status === 'UNPAID',
      'badge--green': status === 'PAID',
      'badge--grey':  status === 'CANCELLED',
    };
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

  openEdit(id: string){
    this.router.navigate(['/invoices/edit', this.invoiceIdFromRoute]);
  }
  

  trackById(_: number, item: { id: string }) { return item.id; }

  get pageTitle():string{
    return 'INVOICES.TITLE_EDIT';
  }
  cancel(){
    this.location.back();
  }
  ngOnDestroy(): void {
    this.themeObserver?.disconnect();
  }
}
