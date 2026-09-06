import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { OrderService } from '../../../core/services/order.service';
import { OrderDetail } from '../../../core/models/order.model';

@Component({
  selector: 'app-order-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss'
})
export class OrderDetailComponent implements OnInit {
  private orderService = inject(OrderService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  order: OrderDetail | null = null;
  loading = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadOrder(id);
    } else {
      this.router.navigate(['/orders']);
    }
  }

  loadOrder(id: string): void {
    this.loading = true;
    this.orderService.getById(id).subscribe({
      next: (order) => {
        this.order = order;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        alert('Không tìm thấy đơn hàng');
        this.router.navigate(['/orders']);
      }
    });
  }

  onConfirm(): void {
    if (!this.order) return;
    if (!confirm(`Xác nhận đơn hàng "${this.order.code}"? Tồn kho các sản phẩm sẽ tự động bị trừ.`)) return;

    this.orderService.confirm(this.order.id).subscribe({
      next: (res) => {
        alert(res.message || 'Xác nhận đơn hàng thành công');
        this.loadOrder(this.order!.id);
      },
      error: (err) => {
        alert(err.error?.error || 'Có lỗi xảy ra khi xác nhận đơn');
      }
    });
  }

  onCancel(): void {
    if (!this.order) return;
    if (!confirm(`Hủy đơn hàng "${this.order.code}"?`)) return;

    this.orderService.cancel(this.order.id).subscribe({
      next: (res) => {
        alert(res.message || 'Đã hủy đơn hàng');
        this.loadOrder(this.order!.id);
      },
      error: (err) => {
        alert(err.error?.error || 'Có lỗi xảy ra khi hủy đơn');
      }
    });
  }

  onComplete(): void {
    if (!this.order) return;
    if (!confirm(`Đánh dấu đơn hàng "${this.order.code}" là đã hoàn tất?`)) return;

    this.orderService.complete(this.order.id).subscribe({
      next: (res) => {
        alert(res.message || 'Đơn hàng đã hoàn tất');
        this.loadOrder(this.order!.id);
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
