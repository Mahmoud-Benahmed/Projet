import { NotSameAsDirective } from './../../../../util/NotSameAsDirective';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormField, MatLabel, MatPrefix, MatError, MatSuffix } from "@angular/material/form-field";
import { MatInput } from "@angular/material/input";
import { MatIcon } from "@angular/material/icon";
import { MatIconButton, MatButton } from "@angular/material/button";

@Component({
  selector: 'app-change-password',
  imports: [
    CommonModule,
    FormsModule,
    MatFormField,
    MatLabel,
    MatPrefix,
    MatSuffix,
    MatIcon,
    MatError,
    MatInput,
    MatIconButton,
    MatButton,
    NotSameAsDirective
  ],
  templateUrl: './change-password.html',
  styleUrl: './change-password.scss',
})
export class ChangePassword {

  credentials = { newPassword: '', currentPassword: '' };
  showCurrentPassword: boolean = false;
  showNewPassword: boolean = false;
  isLoading: boolean = false;

  togglePasswordVisibility(field: 'current' | 'new'): void {
    if (field === 'current') this.showCurrentPassword = !this.showCurrentPassword;
    if (field === 'new') this.showNewPassword = !this.showNewPassword;
  }
}
