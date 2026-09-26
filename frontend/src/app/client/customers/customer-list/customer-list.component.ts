import { Component, OnInit, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CustomerService } from '../../../core/services/customer.service';
import { Customer } from '../../../core/models/customer.model';
import { PagedResult } from '../../../core/models/paged-result.model';

@Component({
  selector: 'app-customer-list',
  standalone: true,
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
  successMessage = '';

  // Dropdown 3 chấm
  activeDropdownCustomerId: string | null = null;

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

  resetFilters(): void {
    this.searchTerm = '';
    this.currentPage = 1;
    this.loadCustomers();
  }

  onPageChange(page: number): void {
    if (page < 1 || (this.pagedResult && page > this.pagedResult.totalPages)) return;
    this.currentPage = page;
    this.loadCustomers();
  }

  toggleDropdown(id: string, event: Event): void {
    event.stopPropagation();
    this.activeDropdownCustomerId = this.activeDropdownCustomerId === id ? null : id;
  }

  closeDropdown(): void {
    this.activeDropdownCustomerId = null;
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.closeDropdown();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeDropdown();
  }

  onDelete(id: string, name: string): void {
    this.closeDropdown();
    if (!confirm(`Bạn có chắc muốn xóa khách hàng "${name}"?`)) return;

    this.loading = true;
    this.customerService.delete(id).subscribe({
      next: () => {
        this.successMessage = `Đã xóa khách hàng "${name}" thành công.`;
        this.loadCustomers();
        setTimeout(() => this.successMessage = '', 3500);
      },
      error: (err) => {
        this.loading = false;
        const msg = err.error?.error || 'Có lỗi xảy ra khi xóa khách hàng';
        alert(msg);
      }
    });
  }
}
