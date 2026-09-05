import { Routes } from '@angular/router';
import { LoginComponent } from './core/auth/login/login.component';
import { RegisterComponent } from './core/auth/register/register.component';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { ADMIN_ROUTES } from './admin/admin.routes';
import { CLIENT_ROUTES } from './client/client.routes';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    children: ADMIN_ROUTES
  },
  {
    path: '',
    canActivate: [authGuard],
    children: CLIENT_ROUTES
  },
  { path: 'stores', redirectTo: 'admin/stores', pathMatch: 'full' },
  { path: '**', redirectTo: '' }
];
