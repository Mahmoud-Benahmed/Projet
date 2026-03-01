import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { HomeComponent } from './components/home/home';
import { RegisterComponent } from './components/system-admin/users/register/register';
import { authGuard } from './services/auth.guard';
import { UsersHomeComponent } from './components/system-admin/users/home/home';
import { ShellComponent } from './components/shell/shell';
import { ProfileComponent } from './components/profile/profile';
import { DeactivatedComponent } from './components/system-admin/users/deactivated/deactivated';
import { MustChangePasswordComponent } from './components/must-change-password/must-change-password';
import { CompleteProfileComponent } from './components/complete-profile/complete-profile';
import { PermissionMatrixComponent } from './components/system-admin/permission-matrix/permission-matrix';

export const routes: Routes = [
  { path: 'login',                component: LoginComponent },
  { path: 'must-change-password', component: MustChangePasswordComponent, canActivate: [authGuard] },
  { path: 'complete-profile',     component: CompleteProfileComponent,    canActivate: [authGuard] },
  {
    path: '',                     component: ShellComponent,          canActivate: [authGuard],
    children: [
      { path: 'home',               component: HomeComponent,           canActivate: [authGuard] },
      { path: 'profile',            component: ProfileComponent,        canActivate: [authGuard] },
      { path: 'change-password',            component: MustChangePasswordComponent,        canActivate: [authGuard] },
      { path: 'permissions',              component: PermissionMatrixComponent,      canActivate: [authGuard], data: { roles: ['SystemAdmin'] } },
      { path: 'users',              component: UsersHomeComponent,      canActivate: [authGuard], data: { roles: ['SystemAdmin'] } },
      { path: 'users/register',     component: RegisterComponent,       canActivate: [authGuard], data: { roles: ['SystemAdmin'] } },
      { path: 'users/deactivated',  component: DeactivatedComponent,    canActivate: [authGuard], data: { roles: ['SystemAdmin'] } },
      { path: 'users/:authUserId',  component: ProfileComponent,        canActivate: [authGuard], data: { roles: ['SystemAdmin'] } },
      { path: '',                   redirectTo: 'home',                 pathMatch: 'full' },
    ]
  },
  { path: '**', redirectTo: 'home' }
];
