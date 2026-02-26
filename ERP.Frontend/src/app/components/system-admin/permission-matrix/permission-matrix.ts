import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { HttpClient } from '@angular/common/http';
import { environment } from  '../../../environment';
import { forkJoin } from 'rxjs';

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
    MatButtonModule,
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

  private baseUrl = `${environment.apiUrl}`;

  constructor(private http: HttpClient, private snackBar: MatSnackBar) {}

  ngOnInit(): void {
    this.loadMatrix();
  }

  loadMatrix(): void {
    this.isLoading = true;
    forkJoin({
      roles: this.http.get<RoleDto[]>(`${this.baseUrl}/auth/roles`),
      controles: this.http.get<ControleDto[]>(`${this.baseUrl}/auth/controles`),
    }).subscribe({
      next: ({ roles, controles }) => {
        this.roles = roles;
        this.controles = controles;
        this.categories = [...new Set(controles.map((c) => c.category))];
        this.loadPrivileges();
      },
      error: () => {
        this.isLoading = false;
        this.snackBar.open('Failed to load permission matrix.', 'Dismiss', {
          duration: 3000,
        });
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
        this.snackBar.open('Failed to load privileges.', 'Dismiss', {
          duration: 3000,
        });
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
        cell.isGranted = !wasGranted;
        cell.loading = false;
      },
      error: () => {
        cell.loading = false;
        this.snackBar.open('Failed to update privilege.', 'Dismiss', {
          duration: 3000,
        });
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
}
