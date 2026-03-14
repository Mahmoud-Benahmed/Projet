import { Client } from './services/client.service';
import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { HomeComponent } from './components/home/home';
import { RegisterComponent } from './components/system-admin/users/register/register';
import { authGuard } from './services/auth.guard';
import { UsersHomeComponent } from './components/system-admin/users/home/home';
import { ShellComponent } from './components/shell/shell';
import { ProfileComponent } from './components/user/profile/profile';
import { DeactivatedComponent } from './components/system-admin/users/deactivated/deactivated';
import { MustChangePasswordComponent } from './components/user/must-change-password/must-change-password';
import { PermissionMatrixComponent } from './components/system-admin/permission-matrix/permission-matrix';
import { ArticleComponent } from './components/articles/home/home';
import { AuditLogComponent } from './components/system-admin/audit-log/audit-log';
import {DeletedUsersComponent } from './components/system-admin/users/deleted/deleted';
import { DeletedArticlesComponent } from './components/articles/deleted/deleted';
import { ClientComponent } from './components/clients/home/home';
import { DeletedClientsComponent } from './components/clients/deleted/deleted';

export const routes: Routes = [
  { path: 'login',                component: LoginComponent },
  { path: 'must-change-password', component: MustChangePasswordComponent, canActivate: [authGuard] },
  {
    path: '',                     component: ShellComponent, canActivate: [authGuard],
    children: [
      { path: 'home',             component: HomeComponent,               canActivate: [authGuard] },
      { path: 'profile',          component: ProfileComponent,            canActivate: [authGuard] },
      { path: 'change-password',  component: MustChangePasswordComponent, canActivate: [authGuard] },
      { path: 'audit-log',        component: AuditLogComponent,           canActivate: [authGuard], data: { privileges: ['ManageAuditLogs'] } },
      { path: 'permissions',      component: PermissionMatrixComponent,   canActivate: [authGuard], data: { privileges: ['AssignRoles'] } },

      { path: 'users',            component: UsersHomeComponent,          canActivate: [authGuard], data: { privileges: ['ViewUsers', 'CreateUser', 'UpdateUser', 'DeleteUser', 'DeactivateUser'] } },
      { path: 'users/register',   component: RegisterComponent,           canActivate: [authGuard], data: { privileges: ['CreateUser'] } },
      { path: 'users/deactivated',component: DeactivatedComponent,        canActivate: [authGuard], data: { privileges: ['ActivateUser', 'DeactivateUser'] } },
      { path: 'users/deleted',    component: DeletedUsersComponent,       canActivate: [authGuard], data: { privileges: ['RestoreUser'] } },
      { path: 'users/:authUserId',component: ProfileComponent,            canActivate: [authGuard], data: { privileges: ['ViewUsers', 'UpdateUser'] } },

      { path: 'articles',         component: ArticleComponent,            canActivate: [authGuard], data: { privileges: ['ViewArticles', 'CreateArticle', 'UpdateArticle', 'DeleteArticle'] } },
      { path: 'articles/deleted', component: DeletedArticlesComponent,    canActivate: [authGuard], data: { privileges: ['RestoreArticle'] } },

      { path: 'clients',          component: ClientComponent,             canActivate: [authGuard], data: { privileges: ['ViewClients', 'CreateClient', 'UpdateClient', 'DeleteClient'] } },
      { path: 'clients/deleted',  component: DeletedClientsComponent,     canActivate: [authGuard], data: { privileges: ['RestoreClient'] } },

      { path: '',                 redirectTo: 'home',                     pathMatch: 'full' },
    ]
  },
  { path: '**', redirectTo: 'home' }
];
