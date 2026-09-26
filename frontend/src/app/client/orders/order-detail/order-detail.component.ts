import { Component, OnInit, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { OrderService } from '../../../core/services/order.service';
import { ProductService } from '../../../core/services/product.service';
import { OrderDetail, OrderItem } from '../../../core/models/order.model';
import { Product } from '../../../core/models/product.model';

@Component({
  selector: 'app-order-detail',
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './order-detail.component.html',
  styleUrl: './order-detail.component.scss'
})
export class OrderDetailComponent implements OnInit {
  private orderService = inject(OrderService);
  private productService = inject(ProductService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  order: OrderDetail | null = null;
  loading = false;

  // Trạng thái chỉnh sửa
  isEditing = false;
  saving = false;
  editCustomerName = '';
  editCustomerPhone = '';
  editCustomerAddress = '';
  editDiscountPercent = 0;
  editItems: OrderItem[] = [];

  // Tìm kiếm và thêm sản phẩm khác
  availableProducts: Product[] = [];
  productSearchTerm = '';
  showProductDropdown = false;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadOrder(id);
    } else {
      this.router.navigate(['/orders']);
    }
    this.loadProducts();
  }

  loadProducts(): void {
    this.productService.getAll(1, 100).subscribe({
      next: (res) => {
        this.availableProducts = res.items.filter(p => p.isActive);
      }
    });
  }

  get filteredProducts(): Product[] {
    const term = this.productSearchTerm.trim().toLowerCase();
    if (!term) return this.availableProducts.slice(0, 10);
    return this.availableProducts.filter(p =>
      p.name.toLowerCase().includes(term) ||
      p.sku.toLowerCase().includes(term)
    ).slice(0, 10);
  }

  onProductSearchInput(): void {
    this.showProductDropdown = true;
  }

  addProductToOrder(prod: Product): void {
    const existing = this.editItems.find(i => i.productId === prod.id && !i.isGift);
    if (existing) {
      existing.quantity++;
      existing.totalPrice = existing.unitPrice * existing.quantity;
    } else {
      this.editItems.push({
        id: 'item_' + Date.now() + Math.random().toString(36).substring(2, 7),
        productId: prod.id,
        productName: prod.name,
        unitPrice: prod.price,
        unitPriceDisplay: this.formatPrice(prod.price),
        currency: prod.currency || 'VND',
        quantity: 1,
        totalPrice: prod.price,
        isGift: false
      });
    }

    // Tự động thêm quà tặng kèm nếu sản phẩm có cấu hình
    if (prod.giftProductId) {
      const giftProduct = this.availableProducts.find(p => p.id === prod.giftProductId);
      const existingGift = this.editItems.find(i => i.productId === prod.giftProductId && i.isGift);
      if (existingGift) {
        existingGift.quantity++;
      } else {
        this.editItems.push({
          id: 'gift_' + Date.now(),
          productId: prod.giftProductId,
          productName: giftProduct?.name ? `[Quà tặng] ${giftProduct.name}` : (prod.giftProductName ? `[Quà tặng] ${prod.giftProductName}` : '[Quà tặng] Sản phẩm kèm'),
          unitPrice: 0,
          unitPriceDisplay: '0',
          currency: prod.currency || 'VND',
          quantity: 1,
          totalPrice: 0,
          isGift: true
        });
      }
    }

    this.productSearchTerm = '';
    this.showProductDropdown = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.product-search-container')) {
      this.showProductDropdown = false;
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

  startEdit(): void {
    if (!this.order) return;
    this.isEditing = true;
    this.editCustomerName = this.order.customerName;
    this.editCustomerPhone = this.order.customerPhone || '';
    this.editCustomerAddress = this.order.customerAddress || '';
    this.editDiscountPercent = this.order.discountPercent || 0;
    this.editItems = this.order.items.map(item => ({
      ...item,
      unitPriceDisplay: this.formatPrice(item.unitPrice)
    }));
  }

  cancelEdit(): void {
    this.isEditing = false;
  }

  increaseQuantity(index: number): void {
    if (index >= 0 && index < this.editItems.length) {
      this.editItems[index].quantity++;
      this.editItems[index].totalPrice = this.editItems[index].unitPrice * this.editItems[index].quantity;
    }
  }

  decreaseQuantity(index: number): void {
    if (index >= 0 && index < this.editItems.length) {
      if (this.editItems[index].quantity > 1) {
        this.editItems[index].quantity--;
        this.editItems[index].totalPrice = this.editItems[index].unitPrice * this.editItems[index].quantity;
      } else {
        this.removeItem(index);
      }
    }
  }

  updateQuantity(index: number, val: any): void {
    if (val === '' || val === null || val === undefined) return;
    const qty = parseInt(val, 10);
    if (!isNaN(qty) && qty > 0) {
      this.editItems[index].quantity = qty;
      this.editItems[index].totalPrice = this.editItems[index].unitPrice * qty;
    }
  }

  onUnitPriceChange(index: number, val: string): void {
    const item = this.editItems[index];
    if (!item || item.isGift) return;

    const clean = (val || '').replace(/[^\d]/g, '');
    const parsed = clean ? parseInt(clean, 10) : 0;
    item.unitPrice = parsed;
    item.totalPrice = parsed * item.quantity;
    item.unitPriceDisplay = clean ? this.formatPrice(parsed) : '';
  }

  onUnitPriceBlur(index: number): void {
    const item = this.editItems[index];
    if (!item || item.isGift) return;
    item.unitPriceDisplay = this.formatPrice(item.unitPrice || 0);
  }

  removeItem(index: number): void {
    const nonGifts = this.editItems.filter(i => !i.isGift);
    if (nonGifts.length <= 1 && !this.editItems[index].isGift) {
      alert('Đơn hàng phải có ít nhất một sản phẩm chính.');
      return;
    }
    this.editItems.splice(index, 1);
  }

  get editSubTotal(): number {
    return this.editItems
      .filter(item => !item.isGift)
      .reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);
  }

  get editDiscountAmount(): number {
    const pct = Math.max(0, Math.min(100, this.editDiscountPercent || 0));
    return Math.round(this.editSubTotal * (pct / 100));
  }

  get editTotalAmount(): number {
    return Math.max(0, this.editSubTotal - this.editDiscountAmount);
  }

  saveEdit(): void {
    if (!this.order) return;

    if (!this.editCustomerName.trim()) {
      alert('Vui lòng nhập họ tên khách hàng.');
      return;
    }

    if (!this.editCustomerPhone.trim()) {
      alert('Vui lòng nhập số điện thoại.');
      return;
    }

    if (this.order.status === 'Pending') {
      const validItems = this.editItems.filter(i => i.quantity > 0);
      if (validItems.length === 0) {
        alert('Đơn hàng phải có ít nhất một sản phẩm.');
        return;
      }
    }

    this.saving = true;

    const payload: any = {
      customerName: this.editCustomerName.trim(),
      customerPhone: this.editCustomerPhone.trim(),
      customerAddress: this.editCustomerAddress.trim()
    };

    if (this.order.status === 'Pending') {
      payload.items = this.editItems.map(item => ({
        productId: item.productId,
        quantity: item.quantity,
        isGift: item.isGift,
        unitPrice: item.unitPrice
      }));
      payload.discountPercent = Math.max(0, Math.min(100, this.editDiscountPercent || 0));
    }

    this.orderService.update(this.order.id, payload).subscribe({
      next: (res) => {
        this.saving = false;
        this.isEditing = false;
        alert(res.message || 'Cập nhật đơn hàng thành công');
        this.loadOrder(this.order!.id);
      },
      error: (err) => {
        this.saving = false;
        alert(err.error?.error || 'Có lỗi xảy ra khi cập nhật đơn hàng');
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

