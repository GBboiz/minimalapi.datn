import { Component, OnInit, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CategoryService } from '../../../core/services/category.service';
import { Category } from '../../../core/models/category.model';
import { PagedResult } from '../../../core/models/paged-result.model';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './category-list.component.html',
  styleUrl: './category-list.component.scss'
})
export class CategoryListComponent implements OnInit {
  private categoryService = inject(CategoryService);

  categories: Category[] = [];
  pagedResult: PagedResult<Category> | null = null;
  currentPage = 1;
  pageSize = 20;
  searchTerm = '';
  loading = false;
  successMessage = '';

  // Dropdown 3 chấm
  activeDropdownCategoryId: string | null = null;

  ngOnInit(): void {
    this.loadCategories();
  }

  loadCategories(): void {
    this.loading = true;
    this.categoryService.getAll(this.currentPage, this.pageSize)
      .subscribe({
        next: (result) => {
          this.pagedResult = result;
          this.categories = result.items;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
        }
      });
  }

  get filteredCategories(): Category[] {
    if (!this.searchTerm.trim()) return this.categories;
    const term = this.searchTerm.toLowerCase().trim();
    return this.categories.filter(c =>
      c.name.toLowerCase().includes(term) ||
      (c.description && c.description.toLowerCase().includes(term))
    );
  }

  resetFilters(): void {
    this.searchTerm = '';
    this.currentPage = 1;
    this.loadCategories();
  }

  onPageChange(page: number): void {
    if (page < 1 || (this.pagedResult && page > this.pagedResult.totalPages)) return;
    this.currentPage = page;
    this.loadCategories();
  }

  toggleDropdown(id: string, event: Event): void {
    event.stopPropagation();
    this.activeDropdownCategoryId = this.activeDropdownCategoryId === id ? null : id;
  }

  closeDropdown(): void {
    this.activeDropdownCategoryId = null;
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
    if (confirm(`Bạn có chắc muốn xóa danh mục "${name}"?`)) {
      this.loading = true;
      this.categoryService.delete(id).subscribe({
        next: () => {
          this.successMessage = `Đã xóa danh mục "${name}" thành công.`;
          this.loadCategories();
          setTimeout(() => this.successMessage = '', 3500);
        },
        error: (err) => {
          this.loading = false;
          alert(err?.error?.error || 'Có lỗi xảy ra khi xóa danh mục');
        }
      });
    }
  }
}
