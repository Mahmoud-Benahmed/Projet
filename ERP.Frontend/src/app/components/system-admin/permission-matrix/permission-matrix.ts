import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { HttpClient } from '@angular/common/http';
import { environment } from  '../../../environment';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../../services/auth/auth.service';
import { RoleService } from '../../../services/auth/roles.service';
import { ControleService } from '../../../services/auth/controle.service';

interface RoleDto {
  id: string;
  libelle: string;
}

interface ControleDto {
  id: string;
  category: string;
  libelle: string;
  description: string;
}

interface PrivilegeDto {
  id: string;
  roleId: string;
  controleId: string;
  controleLibelle: string;
  controleCategory: string;
  isGranted: boolean;
}

interface MatrixCell {
  roleId: string;
  controleId: string;
  isGranted: boolean;
  loading: boolean;
}

@Component({
  selector: 'app-permission-matrix',
  standalone: true,
  imports: [
    CommonModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatIconModule,
    MatButtonModule
],
  templateUrl: './permission-matrix.html',
  styleUrl: './permission-matrix.scss',
})
export class PermissionMatrixComponent implements OnInit {
  roles: RoleDto[] = [];
  controles: ControleDto[] = [];
  categories: string[] = [];
  matrix: Map<string, MatrixCell> = new Map();
  isLoading = true;
  error: string | null = null;
  successMessage: string | null = null;

  private baseUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient,
              private cdr: ChangeDetectorRef,
              public authService: AuthService,
              private roleService: RoleService,
              private controleService: ControleService
  ) {}

  ngOnInit(): void {
    this.loadMatrix();
  }

  loadMatrix(): void {
    this.isLoading = true;
    forkJoin({
      roles: this.roleService.getAll(),
      controles: this.controleService.getAll()
    }).subscribe({
      next: ({ roles, controles }) => {
        this.roles = roles;
        this.controles = controles;

        this.categories = [...new Set(this.controles.map((c) => c.category))];
        this.loadPrivileges();
      },
      error: () => {
        this.isLoading = false;
        this.flash('error', 'Failed to load permission matrix.');
      },
    });
  }

  loadPrivileges(): void {
    const requests = this.roles.map((role) =>
      this.http.get<PrivilegeDto[]>(`${this.baseUrl}/auth/privileges/${role.id}`)
    );

    forkJoin(requests).subscribe({
      next: (results) => {
        results.forEach((privileges) => {
          privileges.forEach((p) => {
            const key = this.cellKey(p.roleId, p.controleId);
            this.matrix.set(key, {
              roleId: p.roleId,
              controleId: p.controleId,
              isGranted: p.isGranted,
              loading: false,
            });
          });
        });
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.flash('error', 'Failed to load privileges.');
      },
    });
  }

  getCell(roleId: string, controleId: string): MatrixCell | undefined {
    return this.matrix.get(this.cellKey(roleId, controleId));
  }

  togglePrivilege(roleId: string, controleId: string): void {
    const cell = this.getCell(roleId, controleId);
    if (!cell || cell.loading) return;

    const wasGranted = cell.isGranted;
    cell.loading = true;

    const action = wasGranted ? 'deny' : 'allow';
    const url = `${this.baseUrl}/auth/privileges/${roleId}/${controleId}/${action}`;

    this.http.put(url, {}).subscribe({
      next: () => {
        this.flash('success', 'Privilege has been updated succcessfully.');
        cell.isGranted = !wasGranted;
        cell.loading = false;
      },
      error: () => {
        cell.loading = false;
        this.flash('error', 'Operation failed, please retry later.');
      },
    });
  }

  getControlesByCategory(category: string): ControleDto[] {
    return this.controles.filter((c) => c.category === category);
  }

  formatRole(libelle: string): string {
    return libelle.replace(/([A-Z])/g, ' $1').trim();
  }

  private cellKey(roleId: string, controleId: string): string {
    return `${roleId}::${controleId}`;
  }

  dismissError(): void { this.error = null; }

  flash(type: 'success' | 'error', msg: string): void {
    if(type === 'success'){
      this.successMessage = msg;
      this.cdr.markForCheck();
      setTimeout(() => (this.successMessage = null), 3000);
    }
    else{
      this.error = msg;
      this.cdr.markForCheck();
      setTimeout(() => (this.error = null), 3000);
    }
  }
}
