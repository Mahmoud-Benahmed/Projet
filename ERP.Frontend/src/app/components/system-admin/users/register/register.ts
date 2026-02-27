import { ChangeDetectorRef, Component, HostBinding, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../../services/auth.service';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { ModalComponent } from '../../../modal/modal';
import { MatDialog } from '@angular/material/dialog';
import { MatProgressSpinner } from "@angular/material/progress-spinner";
import { RegisterRequest, RoleDto } from '../../../../interfaces/AuthDto';
import { generatePassword, checkPassword } from '../../../../util/PasswordUtil';
import { RoleResponseDto, RoleService } from '../../../../services/role.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { forkJoin } from 'rxjs';

@HostBinding('class')
@Component({
  selector: 'app-register',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    MatProgressSpinner
],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class RegisterComponent implements OnDestroy {
  @ViewChild('registerForm') registerForm! :NgForm;

  readonly passwordPattern = /^[^<>&"'\/]{8,}$/.source;
  readonly emailPattern = /^(([^<>()[\]\\.,;:\s@"]+(\.[^<>()[\]\\.,;:\s@"]+)*)|.(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/.source;// not mine it's from

  credentials: RegisterRequest = { login:'', email: '', password: '', roleId: '' };
  showPassword = false;
  private errorTimeout: any = null;
  isLoading:boolean = false;

  roles: RoleResponseDto[] = [];


  passwordErrors: string[] = [];
  passwordScore: number = 0;
  passwordStrength: string = '';

  constructor(private router: Router,
              private authService: AuthService,
              private roleService: RoleService,
              private cdr: ChangeDetectorRef,
              private dialog: MatDialog,
              private snackbar: MatSnackBar) {}

  ngOnInit(): void {
    this.roleService.getAll().subscribe(
      roles => this.roles = roles
    );
  }

  get hostClass(): string {
    return this.passwordStrength
      ? this.getStrengthClass()
      : '';
  }

  togglePasswordVisibility(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    this.isLoading = true;
    this.sanitizeInputs();  // ← sanitize before anything else

    const sanitizedLogin= this.sanitizeLogin(this.credentials.login);
    const loginChanged= sanitizedLogin !== this.credentials.login;

    if(loginChanged){
        const dialogRef= this.dialog.open(ModalComponent, {
            width: '400px',
            data: {
              title: 'Login input changed', // <-- success title
              message: `Login input has been changed to ${sanitizedLogin}, since login cannot contain spaces nor uppercase letters.
                        Do you want to procceed with this username ?`,
              confirmText: 'Confirm',
              showCancel: true,
              icon: 'check_circle',
              iconColor: 'warn'
            }
        });

      dialogRef.afterClosed().subscribe(result => {
          if (result) {
              this.credentials.login= sanitizedLogin;
              this.cdr.markForCheck();
              this.checkAndRegister();
          }else{
              this.stopLoading();
          }
      });
    } else {
        this.checkAndRegister();
    }

  }

  private checkAndRegister(): void {
      forkJoin({
        loginExists: this.authService.existsByLogin(this.credentials.login),
        emailExists: this.authService.existsByEmail(this.credentials.email) // ← correct field
      }).subscribe({
        next: ({ loginExists, emailExists }) => {
          if (loginExists) {
            this.stopLoading();
            this.dialog.open(ModalComponent, {
              width: '400px',
              data: {
                title: 'Invalid Login',
                message: `Please choose a login other than ${this.credentials.login}.`,
                confirmText: 'Ok',
                showCancel: false,
                icon: 'check_circle',
                iconColor: 'primary'
              }
            });
            return;
          }

          if (emailExists) {
            this.isLoading = false;
            this.stopLoading();
            this.dialog.open(ModalComponent, {
              width: '400px',
              data: {
                title: 'Invalid Email',
                message: `Please choose an email other than ${this.credentials.email}.`,
                confirmText: 'Ok',
                showCancel: false,
                icon: 'check_circle',
                iconColor: 'warn'
              }
            });
            return;
          }

          this.stopLoading();
          console.log(this.credentials);

          // call the auth.service method to register the user in the backend
          this.register();
        },
        error: () => {
          this.stopLoading();
          this.snackbar.open('Failed to check availability.', 'Dismiss', { duration: 3000 });
        }
      });
  }

  private register(): void {
    this.isLoading= true;
    this.authService.register(this.credentials).subscribe({
        next: (registeredUser) => {
          this.stopLoading();
          this.snackbar.open(`User ${this.credentials.login} has been registered successfully`, 'Dismiss', { duration: 3000 });
          setTimeout(() => this.resetForm(), 3000);
        },
        error: (error) => {
          this.stopLoading();
          if (error.status === 0) return;
          this.snackbar.open('Failed to register user', 'Dismiss', { duration: 3000 });
        }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }


  // helper methods
  generatePassword(){
    this.credentials.password= generatePassword();
    if(!this.showPassword) this.showPassword= true;
    this.onPasswordChange();
  }

  onPasswordChange(): void {
      if (!this.credentials.password) {
        this.passwordErrors = [];
        this.passwordScore = 0;
        this.passwordStrength = '';
        return;
      }

      const result = checkPassword(this.credentials.password);
      this.passwordErrors = result.errors;
      this.passwordScore = result.score;
      this.passwordStrength = result.strength;
  }

  getScore(): number {
    // map strength to 1-4 for the bars
    const map: Record<string, number> = {
      'weak': 1,
      'fair': 2,
      'strong': 3,
      'very strong': 4,
    };
    return map[this.passwordStrength] ?? 0;
  }

  getStrengthClass(): string {
    const map: Record<string, string> = {
      'weak': 'strength--weak',
      'fair': 'strength--fair',
      'strong': 'strength--strong',
      'very strong': 'strength--very-strong',
    };
    return map[this.passwordStrength] ?? '';
  }

  getStrengthLabel(): string {
    const map: Record<string, string> = {
      'weak': 'Faible',
      'fair': 'Moyen',
      'strong': 'Fort',
      'very strong': 'Très fort',
    };
    return map[this.passwordStrength] ?? '';
  }

  private sanitizeLogin(login: string){
    login= login.toLowerCase().replace(/ /g, "_");
    return login;
  }

  private sanitizeInputs(): void {
    this.credentials.login = this.sanitizeText(this.credentials.login);
    this.credentials.email = this.sanitizeText(this.credentials.email).toLowerCase();
  }

  private sanitizeText(value: string): string {
    return value
      .trim()                          // remove leading/trailing spaces
      .replace(/</g, '&lt;')           // escape <
      .replace(/>/g, '&gt;')           // escape >
      .replace(/&/g, '&amp;')          // escape &
      .replace(/"/g, '&quot;')         // escape "
      .replace(/'/g, '&#x27;')         // escape '
      .replace(/\//g, '&#x2F;');       // escape /
  }

  resetForm(): void {
    this.credentials = { login: '', email: '', password: '', roleId: '' };
    this.passwordErrors = [];
    this.passwordScore = 0;
    this.passwordStrength = '';
    this.showPassword = false;
    this.registerForm.resetForm();
  }
  stopLoading():void{
    this.isLoading = false;
    this.cdr.markForCheck();
  }

  ngOnDestroy(): void {
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
  }
}
