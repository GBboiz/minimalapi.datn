import { Component, OnInit, OnDestroy, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { OrderService } from '../../../core/services/order.service';
import { OrderSummary } from '../../../core/models/order.model';
import { PagedResult } from '../../../core/models/paged-result.model';

@Component({
  selector: 'app-order-list',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.scss'
})
export class OrderListComponent implements OnInit, OnDestroy {
  private orderService = inject(OrderService);
  private searchSubject = new Subject<string>();
  private searchSub?: Subscription;

  orders: OrderSummary[] = [];
  pagedResult: PagedResult<OrderSummary> | null = null;
  currentPage = 1;
  pageSize = 10;
  searchTerm = '';
  selectedStatus = '';
  selectedDate = '';
  loading = false;
  activeDropdownOrderId: string | null = null;

  statuses = [
    { label: 'Tất cả trạng thái', value: '' },
    { label: 'Chờ duyệt (Pending)', value: 'Pending' },
    { label: 'Đã xác nhận (Confirmed)', value: 'Confirmed' },
    { label: 'Hoàn tất (Completed)', value: 'Completed' },
    { label: 'Đã hủy (Cancelled)', value: 'Cancelled' }
  ];

  ngOnInit(): void {
    // Tự động tìm kiếm sau 300ms khi người dùng nhập tên, mã hoặc SĐT
    this.searchSub = this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage = 1;
      this.loadOrders();
    });

    this.loadOrders();
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  loadOrders(): void {
    this.loading = true;
    this.orderService.getAll(
      this.currentPage,
      this.pageSize,
      this.searchTerm.trim() || undefined,
      this.selectedStatus || undefined,
      this.selectedDate || undefined
    ).subscribe({
      next: (result) => {
        this.pagedResult = result;
        this.orders = result.items;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  onSearchInput(): void {
    this.searchSubject.next(this.searchTerm);
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  onStatusChange(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  onDateChange(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  resetFilters(): void {
    this.searchTerm = '';
    this.selectedStatus = '';
    this.selectedDate = '';
    this.currentPage = 1;
    this.loadOrders();
  }

  onPageChange(page: number): void {
    if (page < 1 || (this.pagedResult && page > this.pagedResult.totalPages)) return;
    this.currentPage = page;
    this.loadOrders();
  }

  toggleDropdown(id: string, event: Event): void {
    event.stopPropagation();
    this.activeDropdownOrderId = this.activeDropdownOrderId === id ? null : id;
  }

  closeDropdown(): void {
    this.activeDropdownOrderId = null;
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.closeDropdown();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closeDropdown();
  }

  onConfirm(id: string, code: string): void {
    this.closeDropdown();
    if (!confirm(`Xác nhận đơn hàng "${code}"? Tồn kho các sản phẩm sẽ được tự động trừ.`)) return;

    this.orderService.confirm(id).subscribe({
      next: (res) => {
        alert(res.message || 'Xác nhận đơn hàng thành công');
        this.loadOrders();
      },
      error: (err) => {
        alert(err.error?.error || 'Có lỗi xảy ra khi xác nhận đơn');
      }
    });
  }

  onCancel(id: string, code: string): void {
    this.closeDropdown();
    if (!confirm(`Bạn có chắc muốn hủy đơn hàng "${code}"?`)) return;

    this.orderService.cancel(id).subscribe({
      next: (res) => {
        alert(res.message || 'Đã hủy đơn hàng');
        this.loadOrders();
      },
      error: (err) => {
        alert(err.error?.error || 'Có lỗi xảy ra khi hủy đơn');
      }
    });
  }

  onComplete(id: string, code: string): void {
    this.closeDropdown();
    if (!confirm(`Đánh dấu đơn hàng "${code}" là đã hoàn tất (đóng đơn)?`)) return;

    this.orderService.complete(id).subscribe({
      next: (res) => {
        alert(res.message || 'Đơn hàng đã hoàn tất');
        this.loadOrders();
      },
      error: (err) => {
        alert(err.error?.error || 'Có lỗi xảy ra khi hoàn tất đơn');
      }
    });
  }

  getStatusBadgeClass(status: string): string {
    switch (status) {
      case 'Pending': return 'badge-pending';
      case 'Confirmed': return 'badge-confirmed';
      case 'Completed': return 'badge-completed';
      case 'Cancelled': return 'badge-cancelled';
      default: return 'badge-default';
    }
  }

  getStatusLabel(status: string): string {
    switch (status) {
      case 'Pending': return 'Chờ duyệt';
      case 'Confirmed': return 'Đã xác nhận';
      case 'Completed': return 'Hoàn tất';
      case 'Cancelled': return 'Đã hủy';
      default: return status;
    }
  }

  formatPrice(val: number | null | undefined): string {
    if (val === null || val === undefined || isNaN(val)) return '0';
    return Math.round(val).toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  }
}
