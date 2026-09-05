import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OrderService } from '../../../core/services/order.service';
import { CustomerService } from '../../../core/services/customer.service';
import { ProductService } from '../../../core/services/product.service';
import { Customer } from '../../../core/models/customer.model';
import { Product } from '../../../core/models/product.model';

interface CartItem {
  product: Product;
  quantity: number;
}

@Component({
  selector: 'app-order-create',
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './order-create.component.html',
  styleUrl: './order-create.component.scss'
})
export class OrderCreateComponent implements OnInit {
  private orderService = inject(OrderService);
  private customerService = inject(CustomerService);
  private productService = inject(ProductService);
  private router = inject(Router);

  customers: Customer[] = [];
  products: Product[] = [];

  selectedCustomerId = '';
  selectedProductId = '';
  selectedQuantity = 1;

  cart: CartItem[] = [];
  submitting = false;
  loading = false;

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
        // Chỉ lấy sản phẩm đang hoạt động
        this.products = res.items.filter(p => p.isActive);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
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

    const existing = this.cart.find(c => c.product.id === prod.id);
    if (existing) {
      existing.quantity += this.selectedQuantity;
    } else {
      this.cart.push({
        product: prod,
        quantity: this.selectedQuantity
      });
    }

    this.selectedProductId = '';
    this.selectedQuantity = 1;
  }

  removeFromCart(index: number): void {
    this.cart.splice(index, 1);
  }

  get totalAmount(): number {
    return this.cart.reduce((sum, item) => sum + (item.product.price * item.quantity), 0);
  }

  onSubmit(): void {
    if (!this.selectedCustomerId) {
      alert('Vui lòng chọn khách hàng');
      return;
    }

    if (this.cart.length === 0) {
      alert('Vui lòng thêm ít nhất một sản phẩm vào đơn hàng');
      return;
    }

    this.submitting = true;

    const request = {
      customerId: this.selectedCustomerId,
      items: this.cart.map(c => ({
        productId: c.product.id,
        quantity: c.quantity
      }))
    };

    this.orderService.create(request).subscribe({
      next: (orderId) => {
        alert('Tạo đơn hàng thành công (Trạng thái: Chờ duyệt)');
        this.router.navigate(['/orders']);
      },
      error: (err) => {
        this.submitting = false;
        alert(err.error?.error || 'Có lỗi xảy ra khi tạo đơn hàng');
      }
    });
  }
}
