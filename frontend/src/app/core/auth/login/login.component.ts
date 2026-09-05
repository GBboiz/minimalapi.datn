import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({ selector: 'app-login', imports: [CommonModule, FormsModule, RouterLink], templateUrl: './login.component.html' })
export class LoginComponent {
  private auth = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  loading = false;
  errorMessage = '';

  fillSuperAdmin(): void {
    this.email = 'superadmin';
    this.password = '12345678';
    this.errorMessage = '';
  }

  fillDemo(): void {
    this.email = 'demo@minimalapi.local';
    this.password = 'Demo@123456';
    this.errorMessage = '';
  }

  submit(): void {
    this.loading = true;
    this.errorMessage = '';
    this.auth.login(this.email, this.password).subscribe({
      next: (res) => {
        if (res.isAdmin || this.auth.isAdmin) {
          void this.router.navigateByUrl('/admin/stores');
        } else {
          void this.router.navigateByUrl('/');
        }
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err?.error?.detail || err?.error?.title || err?.error?.error || 'Đăng nhập thất bại. Vui lòng kiểm tra tài khoản hoặc mật khẩu.';
      }
    });
  }
}
