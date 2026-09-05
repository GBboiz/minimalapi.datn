import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../core/services/order.service';
import { OrderSummary } from '../../../core/models/order.model';
import { PagedResult } from '../../../core/models/paged-result.model';

@Component({
  selector: 'app-order-list',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './order-list.component.html',
  styleUrl: './order-list.component.scss'
})
export class OrderListComponent implements OnInit {
  private orderService = inject(OrderService);

  orders: OrderSummary[] = [];
  pagedResult: PagedResult<OrderSummary> | null = null;
  currentPage = 1;
  pageSize = 10;
  searchTerm = '';
  selectedStatus = '';
  loading = false;

  statuses = [
    { label: 'Tất cả trạng thái', value: '' },
    { label: 'Chờ duyệt (Pending)', value: 'Pending' },
    { label: 'Đã xác nhận (Confirmed)', value: 'Confirmed' },
    { label: 'Hoàn tất (Completed)', value: 'Completed' },
    { label: 'Đã hủy (Cancelled)', value: 'Cancelled' }
  ];

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading = true;
    this.orderService.getAll(this.currentPage, this.pageSize, this.searchTerm, this.selectedStatus).subscribe({
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

  onSearch(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  onStatusChange(): void {
    this.currentPage = 1;
    this.loadOrders();
  }

  onPageChange(page: number): void {
    if (page < 1 || (this.pagedResult && page > this.pagedResult.totalPages)) return;
    this.currentPage = page;
    this.loadOrders();
  }

  onConfirm(id: string, code: string): void {
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
    if (!confirm(`Đánh dấu đơn hàng "${code}" là đã hoàn tất?`)) return;

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
      default: return 'bg-secondary';
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
}
