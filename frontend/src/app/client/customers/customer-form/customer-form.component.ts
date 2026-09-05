import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CustomerService } from '../../../core/services/customer.service';

@Component({
  selector: 'app-customer-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './customer-form.component.html',
  styleUrl: './customer-form.component.scss'
})
export class CustomerFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private customerService = inject(CustomerService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  customerForm: FormGroup;
  isEditMode = false;
  customerId: string | null = null;
  loading = false;
  submitting = false;

  constructor() {
    this.customerForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(150)]],
      phone: ['', [Validators.required, Validators.maxLength(20)]],
      email: ['', [Validators.email, Validators.maxLength(256)]],
      address: ['', [Validators.maxLength(500)]]
    });
  }

  ngOnInit(): void {
    this.customerId = this.route.snapshot.paramMap.get('id');
    if (this.customerId) {
      this.isEditMode = true;
      this.loadCustomer(this.customerId);
    }
  }

  loadCustomer(id: string): void {
    this.loading = true;
    this.customerService.getById(id).subscribe({
      next: (customer) => {
        this.customerForm.patchValue({
          name: customer.name,
          phone: customer.phone,
          email: customer.email || '',
          address: customer.address || ''
        });
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.router.navigate(['/customers']);
      }
    });
  }

  onSubmit(): void {
    if (this.customerForm.invalid) {
      this.customerForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const formValue = this.customerForm.value;

    if (this.isEditMode && this.customerId) {
      this.customerService.update(this.customerId, formValue).subscribe({
        next: () => {
          alert('Cập nhật khách hàng thành công');
          this.router.navigate(['/customers']);
        },
        error: (err) => {
          this.submitting = false;
          alert(err.error?.error || 'Có lỗi xảy ra khi cập nhật');
        }
      });
    } else {
      this.customerService.create(formValue).subscribe({
        next: () => {
          alert('Tạo khách hàng thành công');
          this.router.navigate(['/customers']);
        },
        error: (err) => {
          this.submitting = false;
          alert(err.error?.error || 'Có lỗi xảy ra khi tạo khách hàng');
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/customers']);
  }

  get f() {
    return this.customerForm.controls;
  }
}
