import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({ selector: 'app-register', imports: [CommonModule, FormsModule, RouterLink], templateUrl: './register.component.html' })
export class RegisterComponent {
  private auth = inject(AuthService); private router = inject(Router);
  email = ''; fullName = ''; password = ''; storeName = ''; loading = false;
  submit(): void { this.loading = true; this.auth.register(this.email, this.fullName, this.password, this.storeName).subscribe({ next: () => void this.router.navigateByUrl('/products'), error: () => this.loading = false }); }
}
