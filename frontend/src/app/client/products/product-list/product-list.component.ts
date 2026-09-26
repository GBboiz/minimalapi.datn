import { Component, OnInit, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { Product } from '../../../core/models/product.model';
import { PagedResult } from '../../../core/models/paged-result.model';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {
  private productService = inject(ProductService);

  products: Product[] = [];
  pagedResult: PagedResult<Product> | null = null;
  currentPage = 1;
  pageSize = 10;
  searchTerm = '';
  filterStatus = 'ALL'; // ALL, ACTIVE, INACTIVE
  loading = false;
  successMessage = '';

  // Dropdown 3 chấm
  activeDropdownProductId: string | null = null;

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading = true;
    this.productService.getAll(this.currentPage, this.pageSize, this.searchTerm || undefined)
      .subscribe({
        next: (result) => {
          this.pagedResult = result;
          this.products = result.items;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
  }

  get filteredProducts(): Product[] {
    if (this.filterStatus === 'ALL') return this.products;
    const isActive = this.filterStatus === 'ACTIVE';
    return this.products.filter(p => p.isActive === isActive);
  }

  onSearch(): void {
    this.currentPage = 1;
    this.loadProducts();
  }

  resetFilters(): void {
    this.searchTerm = '';
    this.filterStatus = 'ALL';
    this.currentPage = 1;
    this.loadProducts();
  }

  onPageChange(page: number): void {
    if (page < 1 || (this.pagedResult && page > this.pagedResult.totalPages)) return;
    this.currentPage = page;
    this.loadProducts();
  }

  toggleDropdown(id: string, event: Event): void {
    event.stopPropagation();
    this.activeDropdownProductId = this.activeDropdownProductId === id ? null : id;
  }

  closeDropdown(): void {
    this.activeDropdownProductId = null;
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
    if (confirm(`Bạn có chắc muốn xóa sản phẩm "${name}"?`)) {
      this.loading = true;
      this.productService.delete(id).subscribe({
        next: () => {
          this.successMessage = `Đã xóa sản phẩm "${name}" thành công.`;
          this.loadProducts();
          setTimeout(() => this.successMessage = '', 3500);
        },
        error: (err) => {
          this.loading = false;
          alert(err?.error?.error || 'Có lỗi xảy ra khi xóa sản phẩm');
        }
      });
    }
  }
}
