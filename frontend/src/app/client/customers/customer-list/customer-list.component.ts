import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CustomerService } from '../../../core/services/customer.service';
import { Customer } from '../../../core/models/customer.model';
import { PagedResult } from '../../../core/models/paged-result.model';

@Component({
  selector: 'app-customer-list',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './customer-list.component.html',
  styleUrl: './customer-list.component.scss'
})
export class CustomerListComponent implements OnInit {
  private customerService = inject(CustomerService);

  customers: Customer[] = [];
  pagedResult: PagedResult<Customer> | null = null;
  currentPage = 1;
  pageSize = 20;
  searchTerm = '';
  loading = false;

  ngOnInit(): void {
    this.loadCustomers();
  }

  loadCustomers(): void {
    this.loading = true;
    this.customerService.getAll(this.currentPage, this.pageSize, this.searchTerm).subscribe({
      next: (result) => {
        this.pagedResult = result;
        this.customers = result.items;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadCustomers();
  }

  onPageChange(page: number): void {
    if (page < 1 || (this.pagedResult && page > this.pagedResult.totalPages)) return;
    this.currentPage = page;
    this.loadCustomers();
  }

  onDelete(id: string, name: string): void {
    if (!confirm(`Bạn có chắc muốn xóa khách hàng "${name}"?`)) return;

    this.customerService.delete(id).subscribe({
      next: () => {
        alert('Xóa khách hàng thành công');
        this.loadCustomers();
      },
      error: (err) => {
        const msg = err.error?.error || 'Có lỗi xảy ra khi xóa khách hàng';
        alert(msg);
      }
    });
  }
}
