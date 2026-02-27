import { ChangeDetectorRef, Component, HostBinding, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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
import { ThisReceiver } from '@angular/compiler';
import { MatSnackBar } from '@angular/material/snack-bar';

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

    const sanitizedLogin= this.sanitizeLogin(this.credentials.login);
    const loginChanged= sanitizedLogin !== this.credentials.login;
    if(loginChanged){
        const dialogRef= this.dialog.open(ModalComponent, {
            width: '400px',
            data: {
              title: 'Login input changed', // <-- success title
              message: `Login input has been changed to ${sanitizedLogin}, since login cannot contain spaces or be uppercase.
                        Do you want to procceed with this username ?`,
              confirmText: 'Confirm',
              showCancel: true,
              icon: 'check_circle',
              iconColor: 'primary'
            }
        });

      dialogRef.afterClosed().subscribe(result => {
          if (result) {
              this.credentials.login= sanitizedLogin;
              this.register();
          }else{
              this.isLoading= false;
          }
        });
    } else {
        this.isLoading= false;
        this.register(); // ← no change, register directly
    }
  }

  private register(): void {
    this.authService.existsByLogin(this.credentials.login).subscribe({
          next: (exists) => {
            if (!exists) return;

            this.isLoading = false;
            this.dialog.open(ModalComponent, {
              width: '400px',
              data: {
                title: 'Invalid Login',
                message: `Please choose another Login other than ${this.credentials.login}.`,
                confirmText: 'Ok',
                showCancel: false,
                icon:'check_circle',
                iconColor: 'primary'
              },
            });
          },
          error: () => {
            this.isLoading = false;
            this.snackbar.open('Failed to check login availability.', 'Dismiss', { duration: 3000 });
          }
    });

    this.authService.existsByLogin(this.credentials.login).subscribe({
        next: (exists) => {
          if (!exists) return; // login is available, proceed
          this.isLoading = false;
          this.dialog.open(ModalComponent, {
            width: '400px',
            data: {
              title: 'Invalid Login',
              message: `Please choose another Email other than ${this.credentials.login}.`,
              confirmText: 'Ok',
              showCancel: false,
              icon:'check_circle',
              iconColor: 'primary'
            },
          });
        },
        error: () => {
          this.isLoading = false;
          this.snackbar.open('Failed to check login availability.', 'Dismiss', { duration: 3000 });
        }
    });

    this.authService.register(this.credentials).subscribe({
        next: () => {
          this.isLoading = false;
          this.snackbar.open(`User ${this.credentials.login} has been registered successfully`, 'Dismiss', { duration: 3000 });
        },
        error: (error) => {
          this.isLoading = false; // ← don't forget this
          if (error.status === 0) return;
          this.snackbar.open('Failed to register user', 'Dismiss', { duration: 3000 });
        }
    });
  }
  sanitizeLogin(login: string){
    login= login.toLowerCase().replace(/ /g, "_");
    return login;
  }

  generatePassword(){
    this.credentials.password= generatePassword();
    if(!this.showPassword) this.showPassword= true;
    this.onPasswordChange();
  }

  onPasswordChange(): void {
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

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  ngOnDestroy(): void {
    if (this.errorTimeout) clearTimeout(this.errorTimeout);
  }
}
