import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { HomeComponent } from './components/home/home';
import { RegisterComponent } from './components/system-admin/users/register/register';
import { authGuard } from './guard/auth.guard';
import { UsersHomeComponent } from './components/system-admin/users/home/home';
import { ShellComponent } from './components/shell/shell';
import { ProfileComponent } from './components/user/profile/profile';
import { DeactivatedComponent } from './components/system-admin/users/deactivated/deactivated';
import { MustChangePasswordComponent } from './components/user/must-change-password/must-change-password';
import { PermissionMatrixComponent } from './components/system-admin/permission-matrix/permission-matrix';
import { ArticleComponent } from './components/articles/home/home';
import { AuditLogComponent } from './components/system-admin/audit-log/audit-log';
import { DeletedUsersComponent } from './components/system-admin/users/deleted/deleted';
import { ChangePasswordComponent } from './components/system-admin/users/change-password/change-password';
import { ControleComponent } from './components/system-admin/controles/controles';
import { RoleComponent } from './components/system-admin/roles/roles';
import { ArticleCategoriesComponent } from './components/articles/categories/categories';
import { PRIVILEGES } from './services/auth/auth.service';
import { ClientsComponent } from './components/clients/home/home';
import { ClientCategoriesComponent } from './components/clients/categories/categories';

// helper function to pick multiple privileges from a category
function pickPrivileges(category: keyof typeof PRIVILEGES, keys: string[]) {
  return keys.map(k => PRIVILEGES[category][k as keyof typeof PRIVILEGES[typeof category]]);
}

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'must-change-password', component: MustChangePasswordComponent, canActivate: [authGuard] },
  {
    path: '', component: ShellComponent, canActivate: [authGuard],
    children: [
      { path: 'home', component: HomeComponent },
      { path: 'profile', component: ProfileComponent },
      { path: 'change-password', component: MustChangePasswordComponent },
      { path: 'change-password/:authUserId', component: ChangePasswordComponent, data: { privileges: pickPrivileges('USERS', ['VIEW_USERS', 'UPDATE_USER']) } },
      { path: 'audit-log', component: AuditLogComponent, data: { privileges: pickPrivileges('AUDIT', ['MANAGE_AUDITLOGS']) } },
      { path: 'permissions', component: PermissionMatrixComponent, data: { privileges: pickPrivileges('USERS', ['ASSIGN_ROLES']) } },

      { path: 'users', component: UsersHomeComponent, data: { privileges: pickPrivileges('USERS', ['VIEW_USERS','CREATE_USER','UPDATE_USER','DELETE_USER','DEACTIVATE_USER']) } },
      { path: 'users/register', component: RegisterComponent, data: { privileges: pickPrivileges('USERS', ['CREATE_USER']) } },
      { path: 'users/deactivated', component: DeactivatedComponent, data: { privileges: pickPrivileges('USERS', ['ACTIVATE_USER','DEACTIVATE_USER']) } },
      { path: 'users/deleted', component: DeletedUsersComponent, data: { privileges: pickPrivileges('USERS', ['RESTORE_USER']) } },
      { path: 'users/controles', component: ControleComponent, data: { privileges: pickPrivileges('USERS', ['ASSIGN_ROLES']) } },
      { path: 'users/roles', component: RoleComponent, data: { privileges: pickPrivileges('USERS', ['ASSIGN_ROLES']) } },
      { path: 'users/:authUserId', component: ProfileComponent, data: { privileges: pickPrivileges('USERS', ['VIEW_USERS','UPDATE_USER']) } },

      { path: 'articles', component: ArticleComponent, data: { privileges: pickPrivileges('ARTICLES', ['VIEW_ARTICLES','CREATE_ARTICLE','UPDATE_ARTICLE','DELETE_ARTICLE']) } },
      { path: 'articles/categories', component: ArticleCategoriesComponent, data: { privileges: pickPrivileges('ARTICLES', ['VIEW_ARTICLES','CREATE_ARTICLE','UPDATE_ARTICLE']) } },

      { path: 'clients', component: ClientsComponent, data: { privileges: pickPrivileges('CLIENTS', ['VIEW_CLIENTS','CREATE_CLIENT','UPDATE_CLIENT','DELETE_CLIENT']) } },
      { path: 'clients/categories', component: ClientCategoriesComponent, data: { privileges: pickPrivileges('CLIENTS', ['VIEW_CLIENTS','CREATE_CLIENT','UPDATE_CLIENT']) } },

      { path: '', redirectTo: 'home', pathMatch: 'full' },
    ]
  },
  { path: '**', redirectTo: 'home' }
];
