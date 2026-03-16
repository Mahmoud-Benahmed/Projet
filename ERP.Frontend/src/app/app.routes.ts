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

export const routes: Routes = [
  { path: 'login',                component: LoginComponent },
  { path: 'must-change-password', component: MustChangePasswordComponent, canActivate: [authGuard] },
  {
    path: '',                     component: ShellComponent, canActivate: [authGuard],
    children: [
      { path: 'home',             component: HomeComponent},
      { path: 'profile',          component: ProfileComponent},
      { path: 'change-password',  component: MustChangePasswordComponent},
      { path: 'change-password/:authUserId',  component: ChangePasswordComponent, data: { privileges: ['ViewUsers', 'UpdateUser'] } },
      { path: 'audit-log',        component: AuditLogComponent,           data: { privileges: ['ManageAuditLogs'] } },
      { path: 'permissions',      component: PermissionMatrixComponent,   data: { privileges: ['AssignRoles'] } },

      { path: 'users',            component: UsersHomeComponent,          data: { privileges: ['ViewUsers', 'CreateUser', 'UpdateUser', 'DeleteUser', 'DeactivateUser'] } },
      { path: 'users/register',   component: RegisterComponent,           data: { privileges: ['CreateUser'] } },
      { path: 'users/deactivated',component: DeactivatedComponent,        data: { privileges: ['ActivateUser', 'DeactivateUser'] } },
      { path: 'users/deleted',    component: DeletedUsersComponent,       data: { privileges: ['RestoreUser'] } },
      { path: 'users/:authUserId',component: ProfileComponent,            data: { privileges: ['ViewUsers', 'UpdateUser'] } },

      { path: 'articles',         component: ArticleComponent,            data: { privileges: ['ViewArticles', 'CreateArticle', 'UpdateArticle', 'DeleteArticle'] } },
      { path: 'articles/deleted', component: DeletedArticlesComponent,    data: { privileges: ['RestoreArticle'] } },

      { path: 'clients',          component: ClientComponent,             data: { privileges: ['ViewClients', 'CreateClient', 'UpdateClient', 'DeleteClient'] } },
      { path: 'clients/deleted',  component: DeletedClientsComponent,     data: { privileges: ['RestoreClient'] } },

      { path: '',                 redirectTo: 'home',                     pathMatch: 'full' },
    ]
  },
  { path: '**', redirectTo: 'home' }
];
