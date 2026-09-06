import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../core/services/product.service';
import { Product } from '../../core/models/product.model';

@Component({
  selector: 'app-gifts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gifts.component.html',
  styleUrl: './gifts.component.scss'
})
export class GiftsComponent implements OnInit {
  private productService = inject(ProductService);

  products: Product[] = [];
  loading = false;
  saving = false;
  searchTerm = '';
  filterStatus = 'ALL'; // ALL, HAS_GIFT, NO_GIFT

  // Modal gán quà
  showAssignModal = false;
  selectedProductId: string = '';
  selectedGiftProductId: string = '';
  errorMessage: string = '';
  successMessage: string = '';

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading = true;
    this.productService.getAll(1, 100).subscribe({
      next: (res) => {
        this.products = res.items;
        this.loading = false;
      },
      error: (err) => {
        this.errorMessage = 'Không thể tải danh sách sản phẩm.';
        this.loading = false;
      }
    });
  }

  get filteredProducts(): Product[] {
    return this.products.filter(p => {
      const matchSearch = !this.searchTerm ||
        p.name.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        p.sku.toLowerCase().includes(this.searchTerm.toLowerCase());

      if (!matchSearch) return false;

      if (this.filterStatus === 'HAS_GIFT') return !!p.giftProductId;
      if (this.filterStatus === 'NO_GIFT') return !p.giftProductId;
      return true;
    });
  }

  get availableGifts(): Product[] {
    // Không cho phép chọn chính sản phẩm đang cấu hình làm quà tặng
    return this.products.filter(p => p.id !== this.selectedProductId);
  }

  openAssignModal(product?: Product): void {
    this.errorMessage = '';
    this.successMessage = '';
    if (product) {
      this.selectedProductId = product.id;
      this.selectedGiftProductId = product.giftProductId || '';
    } else {
      this.selectedProductId = this.products.length > 0 ? this.products[0].id : '';
      this.selectedGiftProductId = '';
    }
    this.showAssignModal = true;
  }

  closeModal(): void {
    this.showAssignModal = false;
    this.selectedProductId = '';
    this.selectedGiftProductId = '';
    this.errorMessage = '';
  }

  saveGift(): void {
    if (!this.selectedProductId) {
      this.errorMessage = 'Vui lòng chọn sản phẩm chính.';
      return;
    }

    this.saving = true;
    this.errorMessage = '';

    const giftId = this.selectedGiftProductId ? this.selectedGiftProductId : null;

    this.productService.setGift(this.selectedProductId, giftId).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = giftId ? 'Đã gán quà tặng thành công!' : 'Đã gỡ quà tặng!';
        this.closeModal();
        this.loadProducts();
        setTimeout(() => this.successMessage = '', 3500);
      },
      error: (err) => {
        this.saving = false;
        this.errorMessage = err?.error?.error || 'Có lỗi xảy ra khi gán quà tặng.';
      }
    });
  }

  removeGift(product: Product): void {
    if (!confirm(`Bạn có chắc muốn gỡ quà tặng kèm của sản phẩm "${product.name}"?`)) {
      return;
    }

    this.productService.setGift(product.id, null).subscribe({
      next: () => {
        this.successMessage = `Đã gỡ quà tặng kèm cho sản phẩm "${product.name}".`;
        this.loadProducts();
        setTimeout(() => this.successMessage = '', 3500);
      },
      error: (err) => {
        alert(err?.error?.error || 'Không thể gỡ quà tặng.');
      }
    });
  }
}
