import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OrderService } from '../../../core/services/order.service';
import { CustomerService } from '../../../core/services/customer.service';
import { ProductService } from '../../../core/services/product.service';
import { PromotionService } from '../../../core/services/promotion.service';
import { Customer } from '../../../core/models/customer.model';
import { Product } from '../../../core/models/product.model';
import { Promotion, DiscountType } from '../../../core/models/promotion.model';

interface CartItem {
  product: Product;
  quantity: number;
  isGift: boolean;
  parentProductId?: string;
}

@Component({
  selector: 'app-order-create',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './order-create.component.html',
  styleUrl: './order-create.component.scss'
})
export class OrderCreateComponent implements OnInit {
  private orderService = inject(OrderService);
  private customerService = inject(CustomerService);
  private productService = inject(ProductService);
  private promotionService = inject(PromotionService);
  private router = inject(Router);

  customers: Customer[] = [];
  products: Product[] = [];
  promotions: Promotion[] = [];

  selectedCustomerId = '';
  selectedProductId = '';
  selectedQuantity = 1;

  cart: CartItem[] = [];

  // Khuyến mãi & Giảm giá
  discountMode: 'PERCENT' | 'AMOUNT' = 'PERCENT';
  discountPercent = 0;
  discountFixedAmount = 0;
  selectedPromotionCode: string | null = null;

  submitting = false;
  loading = false;

  // Modal thêm khách hàng nhanh
  showQuickCustomerModal = false;
  savingCustomer = false;
  quickCustomer = {
    name: '',
    phone: '',
    address: ''
  };
  customerModalError = '';

  // Preset gợi ý nhanh
  presetDiscounts = [
    { code: 'KM5', label: '5%', discountType: DiscountType.Percentage, value: 5, color: 'primary' },
    { code: 'KM10', label: '10%', discountType: DiscountType.Percentage, value: 10, color: 'info' },
    { code: 'KM100K', label: '100K', discountType: DiscountType.FixedAmount, value: 100000, color: 'success' },
    { code: 'KM200K', label: '200K', discountType: DiscountType.FixedAmount, value: 200000, color: 'warning' }
  ];

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;

    this.customerService.getAll(1, 100).subscribe({
      next: (res) => {
        this.customers = res.items;
      }
    });

    this.productService.getAll(1, 100).subscribe({
      next: (res) => {
        this.products = res.items.filter(p => p.isActive);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });

    this.promotionService.getAll(true).subscribe({
      next: (res) => {
        this.promotions = res;
      }
    });
  }

  get selectedProduct(): Product | undefined {
    return this.products.find(p => p.id === this.selectedProductId);
  }

  addToCart(): void {
    if (!this.selectedProductId || this.selectedQuantity <= 0) return;

    const prod = this.selectedProduct;
    if (!prod) return;

    const existing = this.cart.find(c => c.product.id === prod.id && !c.isGift);
    if (existing) {
      existing.quantity += this.selectedQuantity;
    } else {
      this.cart.push({
        product: prod,
        quantity: this.selectedQuantity,
        isGift: false
      });
    }

    // Tự động tặng quà kèm nếu sản phẩm có cấu hình quà tặng
    if (prod.giftProductId) {
      const giftProduct = this.products.find(p => p.id === prod.giftProductId);
      const giftToBundle: Product = giftProduct || {
        id: prod.giftProductId,
        name: prod.giftProductName || 'Quà tặng kèm',
        sku: 'GIFT',
        stockQuantity: 999,
        price: 0,
        currency: prod.currency,
        categoryId: prod.categoryId,
        categoryName: 'Quà tặng',
        isActive: true,
        createdAt: new Date()
      };

      const existingGift = this.cart.find(c => c.product.id === prod.giftProductId && c.isGift && c.parentProductId === prod.id);
      if (existingGift) {
        existingGift.quantity += this.selectedQuantity;
      } else {
        this.cart.push({
          product: { ...giftToBundle, price: 0 },
          quantity: this.selectedQuantity,
          isGift: true,
          parentProductId: prod.id
        });
      }
    }

    this.selectedProductId = '';
    this.selectedQuantity = 1;
  }

  removeFromCart(index: number): void {
    const item = this.cart[index];
    this.cart.splice(index, 1);

    // Nếu xóa sản phẩm chính, tự động xóa luôn quà tặng kèm đi kèm nếu có
    if (!item.isGift) {
      const giftIndex = this.cart.findIndex(c => c.isGift && c.parentProductId === item.product.id);
      if (giftIndex !== -1) {
        this.cart.splice(giftIndex, 1);
      }
    }
  }

  // Khuyến mãi
  applyPreset(preset: any): void {
    if (this.selectedPromotionCode === preset.code) {
      // Toggle off nếu bấm lại
      this.clearDiscount();
      return;
    }

    this.selectedPromotionCode = preset.code;
    if (preset.discountType === DiscountType.Percentage) {
      this.discountMode = 'PERCENT';
      this.discountPercent = preset.value;
      this.discountFixedAmount = 0;
    } else {
      this.discountMode = 'AMOUNT';
      this.discountFixedAmount = preset.value;
      this.discountPercent = 0;
    }
  }

  applyPromotion(promo: Promotion): void {
    if (this.selectedPromotionCode === promo.code) {
      this.clearDiscount();
      return;
    }

    this.selectedPromotionCode = promo.code;
    if (promo.discountType === DiscountType.Percentage) {
      this.discountMode = 'PERCENT';
      this.discountPercent = promo.value;
      this.discountFixedAmount = 0;
    } else {
      this.discountMode = 'AMOUNT';
      this.discountFixedAmount = promo.value;
      this.discountPercent = 0;
    }
  }

  clearDiscount(): void {
    this.selectedPromotionCode = null;
    this.discountPercent = 0;
    this.discountFixedAmount = 0;
  }

  setCustomPercent(val: number): void {
    this.selectedPromotionCode = null;
    this.discountMode = 'PERCENT';
    this.discountPercent = Math.max(0, Math.min(100, val));
    this.discountFixedAmount = 0;
  }

  setCustomAmount(val: number): void {
    this.selectedPromotionCode = null;
    this.discountMode = 'AMOUNT';
    this.discountFixedAmount = Math.max(0, val);
    this.discountPercent = 0;
  }

  get subTotal(): number {
    return this.cart
      .filter(item => !item.isGift)
      .reduce((sum, item) => sum + (item.product.price * item.quantity), 0);
  }

  get calculatedDiscount(): number {
    if (this.discountMode === 'PERCENT') {
      return Math.round(this.subTotal * (this.discountPercent / 100));
    } else {
      return Math.min(this.subTotal, this.discountFixedAmount);
    }
  }

  get totalAmount(): number {
    return Math.max(0, this.subTotal - this.calculatedDiscount);
  }

  get giftItemsCount(): number {
    return this.cart.filter(item => item.isGift).reduce((sum, item) => sum + item.quantity, 0);
  }

  // Thêm khách hàng nhanh
  openQuickCustomerModal(): void {
    this.quickCustomer = { name: '', phone: '', address: '' };
    this.customerModalError = '';
    this.showQuickCustomerModal = true;
  }

  closeQuickCustomerModal(): void {
    this.showQuickCustomerModal = false;
    this.customerModalError = '';
  }

  saveQuickCustomer(): void {
    if (!this.quickCustomer.name.trim()) {
      this.customerModalError = 'Vui lòng nhập họ tên khách hàng.';
      return;
    }
    if (!this.quickCustomer.phone.trim()) {
      this.customerModalError = 'Vui lòng nhập số điện thoại.';
      return;
    }

    this.savingCustomer = true;
    this.customerModalError = '';

    this.customerService.create({
      name: this.quickCustomer.name.trim(),
      phone: this.quickCustomer.phone.trim(),
      address: this.quickCustomer.address.trim() || undefined
    }).subscribe({
      next: (newCustomerId) => {
        this.savingCustomer = false;
        // Thêm vào danh sách và tự động chọn
        const newCustomer: Customer = {
          id: newCustomerId,
          name: this.quickCustomer.name.trim(),
          phone: this.quickCustomer.phone.trim(),
          address: this.quickCustomer.address.trim(),
          createdAt: new Date().toISOString()
        };
        this.customers.unshift(newCustomer);
        this.selectedCustomerId = newCustomerId;
        this.closeQuickCustomerModal();
      },
      error: (err) => {
        this.savingCustomer = false;
        this.customerModalError = err?.error?.error || 'Có lỗi xảy ra khi tạo khách hàng.';
      }
    });
  }

  onSubmit(): void {
    if (!this.selectedCustomerId) {
      alert('Vui lòng chọn hoặc thêm khách hàng');
      return;
    }

    if (this.cart.length === 0) {
      alert('Vui lòng thêm ít nhất một sản phẩm vào đơn hàng');
      return;
    }

    this.submitting = true;

    const request = {
      customerId: this.selectedCustomerId,
      discountPercent: this.discountMode === 'PERCENT' ? this.discountPercent : 0,
      discountAmount: this.discountMode === 'AMOUNT' ? this.discountFixedAmount : 0,
      promotionCode: this.selectedPromotionCode || undefined,
      items: this.cart.map(c => ({
        productId: c.product.id,
        quantity: c.quantity,
        isGift: c.isGift
      }))
    };

    this.orderService.create(request).subscribe({
      next: (orderId) => {
        alert('Tạo đơn hàng thành công (Trạng thái: Chờ duyệt - Tồn kho dự báo đã được giữ chỗ)');
        this.router.navigate(['/orders']);
      },
      error: (err) => {
        this.submitting = false;
        alert(err?.error?.error || err?.error?.detail || 'Có lỗi xảy ra khi tạo đơn hàng');
      }
    });
  }
}
