import { NotSameAsDirective } from './../../../../util/NotSameAsDirective';
import { Component, Directive, Input } from '@angular/core';
import { AbstractControl, FormsModule, ValidationErrors, Validator } from '@angular/forms';
import { MatFormField, MatLabel, MatPrefix, MatError } from "@angular/material/form-field";
import { MatIcon } from "@angular/material/icon";

@Component({
  selector: 'app-change-password',
  imports: [MatFormField, MatLabel, MatPrefix, MatIcon, MatError, FormsModule, NotSameAsDirective],
  templateUrl: './change-password.html',
  styleUrl: './change-password.scss',
})
export class ChangePassword{

  credentials= {newPassword:'', currentPassword:''};
  showCurrentPassword: boolean= false;
  showNewPassword: boolean= false;
  isLoading: boolean= false;


  togglePasswordVisibility(field: 'current' | 'new'): void {
    if (field === 'current') this.showCurrentPassword = !this.showCurrentPassword;
    if (field === 'new') this.showNewPassword = !this.showNewPassword;
  }
}
