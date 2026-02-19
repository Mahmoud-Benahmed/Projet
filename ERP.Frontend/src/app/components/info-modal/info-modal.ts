import { Component, Inject } from '@angular/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';

export interface ModalData {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  showCancel?: boolean;
  icon?: string;
  iconColor?: 'primary' | 'warn' | 'accent';
}

@Component({
  selector: 'app-info-modal',
  imports: [MatDialogModule, MatButtonModule, MatIconModule, CommonModule],
  template: `
    <div class="modal-header">
      @if(data.icon) {
        <mat-icon [color]="data.iconColor || 'primary'">{{ data.icon }}</mat-icon>
      }
      <h2 mat-dialog-title>{{ data.title }}</h2>
    </div>

    <mat-dialog-content>
      <p>{{ data.message }}</p>
    </mat-dialog-content>

    <mat-dialog-actions align="end">
      @if(data.showCancel !== false) {
        <button mat-button (click)="cancel()">
          {{ data.cancelText || 'Annuler' }}
        </button>
      }
      <button mat-raised-button color="primary" (click)="confirm()">
        {{ data.confirmText || 'Confirmer' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .modal-header {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 16px 24px 0;

      mat-icon {
        font-size: 28px;
        width: 28px;
        height: 28px;
      }

      h2 {
        margin: 0;
        padding: 0;
      }
    }

    mat-dialog-content p {
      color: #555;
      line-height: 1.6;
    }
  `]
})
export class InfoModalComponent {
  constructor(
    private dialogRef: MatDialogRef<InfoModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ModalData
  ) {}

  confirm(): void { this.dialogRef.close(true); }
  cancel(): void  { this.dialogRef.close(false); }
}
