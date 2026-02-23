import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { HomeComponent } from './components/home/home';
import { RegisterComponent } from './components/register/register';
import { authGuard } from './services/auth.guard';
import { UsersHomeComponent } from './components/system-admin/users/home/home';
import { ShellComponent } from './components/shell/shell';
import { ProfileComponent } from './components/profile/profile';
import { DeactivatedComponent } from './components/system-admin/users/deactivated/deactivated';

export const routes: Routes = [
  {     path: 'login',              component: LoginComponent },

  {
      path: '',                   component: ShellComponent,      canActivate: [authGuard],
  children: [

    { path: 'home',               component: HomeComponent,       canActivate: [authGuard] },
    { path: 'profile',            component: ProfileComponent,    canActivate: [authGuard] },
    { path: 'users',              component: UsersHomeComponent,  canActivate: [authGuard],   data: { roles: ['SystemAdmin'] } },
    { path: 'users/register',     component: RegisterComponent,   canActivate: [authGuard],   data: { roles: ['SystemAdmin'] } },
    { path: 'users/deactivated',  component: DeactivatedComponent,   canActivate: [authGuard],   data: { roles: ['SystemAdmin'] } },
    { path: 'users/:authUserId',  component: ProfileComponent,    canActivate: [authGuard],   data: { roles: ['SystemAdmin'] } },
    { path: '',                   redirectTo: 'home',             pathMatch: 'full' },
    ]
  },
  {     path: '**',                 redirectTo: 'home' }
];
