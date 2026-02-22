import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login';
import { HomeComponent } from './components/home/home';
import { RegisterComponent } from './components/register/register';
import { authGuard } from './services/auth.guard';
import { UsersHomeComponent } from './components/system-admin/users/home/home';
import { ShellComponent } from './components/shell/shell';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },

  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'register', component: RegisterComponent, canActivate: [authGuard], data: { roles: ['SystemAdmin'] } },
      { path: 'users', component: UsersHomeComponent, canActivate: [authGuard] },
      { path: 'home', component: HomeComponent, canActivate: [authGuard] },
    ]
  },
  { path: '**', redirectTo: 'login' }
];
