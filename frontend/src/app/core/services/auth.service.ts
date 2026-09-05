import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private readonly tokenKey = 'minimalapi.access-token';
  private readonly storeNameKey = 'minimalapi.store-name';
  private readonly isAdminKey = 'minimalapi.is-admin';

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/login`, { email, password })
      .pipe(tap(response => this.saveSession(response)));
  }

  register(email: string, fullName: string, password: string, storeName: string) {
    return this.http.post<AuthResponse>(`${environment.apiUrl}/auth/register`, { email, fullName, password, storeName })
      .pipe(tap(response => this.saveSession(response)));
  }

  get token(): string | null { return localStorage.getItem(this.tokenKey); }
  get storeName(): string | null { return localStorage.getItem(this.storeNameKey); }
  get isAuthenticated(): boolean { return !!this.token; }

  get isAdmin(): boolean {
    if (localStorage.getItem(this.isAdminKey) === 'true') return true;
    const token = this.token;
    if (!token) return false;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.is_admin === 'true' ||
             payload.role === 'SuperAdmin' ||
             payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] === 'SuperAdmin' ||
             payload.email === 'superadmin@minimalapi.local' ||
             payload.email === 'superadmin';
    } catch {
      return false;
    }
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.storeNameKey);
    localStorage.removeItem(this.isAdminKey);
    void this.router.navigateByUrl('/login');
  }

  updateSession(accessToken: string, storeName: string, isAdmin?: boolean): void {
    localStorage.setItem(this.tokenKey, accessToken);
    localStorage.setItem(this.storeNameKey, storeName);
    if (isAdmin !== undefined) {
      localStorage.setItem(this.isAdminKey, isAdmin ? 'true' : 'false');
    }
  }

  private saveSession(response: AuthResponse): void {
    localStorage.setItem(this.tokenKey, response.accessToken);
    localStorage.setItem(this.storeNameKey, response.storeName);
    if (response.isAdmin !== undefined) {
      localStorage.setItem(this.isAdminKey, response.isAdmin ? 'true' : 'false');
    }
  }
}
