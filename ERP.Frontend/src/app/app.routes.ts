import { Client } from './services/client.service';
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
import { DeletedArticlesComponent } from './components/articles/deleted/deleted';
import { ClientComponent } from './components/clients/home/home';
import { DeletedClientsComponent } from './components/clients/deleted/deleted';
import { ChangePasswordComponent } from './components/system-admin/users/change-password/change-password';
import { ControleComponent } from './components/system-admin/controles/controles';
import { RoleComponent } from './components/system-admin/roles/roles';
import { PRIVILEGES } from './services/auth/auth.service';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'must-change-password', component: MustChangePasswordComponent, canActivate: [authGuard] },
  {
    path: '', component: ShellComponent, canActivate: [authGuard],
    children: [
      { path: 'home', component: HomeComponent },
      { path: 'profile', component: ProfileComponent },
      { path: 'change-password', component: MustChangePasswordComponent },
      { path: 'change-password/:authUserId', component: ChangePasswordComponent, data: { privileges: [PRIVILEGES.VIEW_USERS, PRIVILEGES.UPDATE_USER] } },
      { path: 'audit-log', component: AuditLogComponent, data: { privileges: [PRIVILEGES.MANAGE_AUDIT_LOGS] } },
      { path: 'permissions', component: PermissionMatrixComponent, data: { privileges: [PRIVILEGES.ASSIGN_ROLES] } },

      { path: 'users', component: UsersHomeComponent, data: { privileges: [PRIVILEGES.VIEW_USERS, PRIVILEGES.CREATE_USER, PRIVILEGES.UPDATE_USER, PRIVILEGES.DELETE_USER, PRIVILEGES.DEACTIVATE_USER] } },
      { path: 'users/register', component: RegisterComponent, data: { privileges: [PRIVILEGES.CREATE_USER] } },
      { path: 'users/deactivated', component: DeactivatedComponent, data: { privileges: [PRIVILEGES.ACTIVATE_USER, PRIVILEGES.DEACTIVATE_USER] } },
      { path: 'users/deleted', component: DeletedUsersComponent, data: { privileges: [PRIVILEGES.RESTORE_USER] } },
      { path: 'users/controles', component: ControleComponent, data: { privileges: [PRIVILEGES.ASSIGN_ROLES] } },
      { path: 'users/roles', component: RoleComponent, data: { privileges: [PRIVILEGES.ASSIGN_ROLES] } },
      { path: 'users/:authUserId', component: ProfileComponent, data: { privileges: [PRIVILEGES.VIEW_USERS, PRIVILEGES.UPDATE_USER] } },

      { path: 'articles', component: ArticleComponent, data: { privileges: [PRIVILEGES.VIEW_ARTICLES, PRIVILEGES.CREATE_ARTICLE, PRIVILEGES.UPDATE_ARTICLE, PRIVILEGES.DELETE_ARTICLE] } },
      { path: 'articles/deleted', component: DeletedArticlesComponent, data: { privileges: [PRIVILEGES.RESTORE_ARTICLE] } },

      { path: 'clients', component: ClientComponent, data: { privileges: [PRIVILEGES.VIEW_CLIENTS, PRIVILEGES.CREATE_CLIENT, PRIVILEGES.UPDATE_CLIENT, PRIVILEGES.DELETE_CLIENT] } },
      { path: 'clients/deleted', component: DeletedClientsComponent, data: { privileges: [PRIVILEGES.RESTORE_CLIENT] } },

      { path: '', redirectTo: 'home', pathMatch: 'full' },
    ]
  },
  { path: '**', redirectTo: 'home' }
];
