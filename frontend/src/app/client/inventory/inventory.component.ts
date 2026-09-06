import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InventoryService } from '../../core/services/inventory.service';
import { InventoryItem, InventorySummary, AdjustInventoryRequest } from '../../core/models/inventory.model';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory.component.html',
  styleUrl: './inventory.component.scss'
})
export class InventoryComponent implements OnInit {
  private inventoryService = inject(InventoryService);

  summary: InventorySummary | null = null;
  loading = false;
  saving = false;
  searchTerm = '';
  lowStockOnly = false;

  // Modal điều chỉnh kho
  showAdjustModal = false;
  selectedItem: InventoryItem | null = null;
  stockChange: number = 0;
  adjustReason: string = '';
  errorMessage: string = '';
  successMessage: string = '';

  ngOnInit(): void {
    this.loadInventory();
  }

  loadInventory(): void {
    this.loading = true;
    this.inventoryService.getInventory(this.searchTerm, undefined, this.lowStockOnly).subscribe({
      next: (res) => {
        this.summary = res;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Không thể tải dữ liệu tồn kho.';
        this.loading = false;
      }
    });
  }

  onSearch(): void {
    this.loadInventory();
  }

  toggleLowStockFilter(): void {
    this.lowStockOnly = !this.lowStockOnly;
    this.loadInventory();
  }

  openAdjustModal(item: InventoryItem): void {
    this.selectedItem = item;
    this.stockChange = 0;
    this.adjustReason = '';
    this.errorMessage = '';
    this.showAdjustModal = true;
  }

  closeAdjustModal(): void {
    this.showAdjustModal = false;
    this.selectedItem = null;
    this.stockChange = 0;
    this.adjustReason = '';
    this.errorMessage = '';
  }

  get previewNewStock(): number {
    if (!this.selectedItem) return 0;
    return this.selectedItem.stockQuantity + this.stockChange;
  }

  get previewNewForecast(): number {
    if (!this.selectedItem) return 0;
    return Math.max(0, this.previewNewStock - this.selectedItem.reservedQuantity);
  }

  submitAdjustment(): void {
    if (!this.selectedItem) return;

    if (this.stockChange === 0) {
      this.errorMessage = 'Vui lòng nhập số lượng điều chỉnh khác 0.';
      return;
    }

    if (this.previewNewStock < 0) {
      this.errorMessage = 'Tồn kho thực tế không thể bị âm.';
      return;
    }

    this.saving = true;
    this.errorMessage = '';

    const req: AdjustInventoryRequest = {
      productId: this.selectedItem.id,
      stockChange: this.stockChange,
      reason: this.adjustReason
    };

    this.inventoryService.adjustInventory(req).subscribe({
      next: () => {
        this.saving = false;
        this.successMessage = `Đã điều chỉnh tồn kho cho "${this.selectedItem?.name}" thành công!`;
        this.closeAdjustModal();
        this.loadInventory();
        setTimeout(() => this.successMessage = '', 3500);
      },
      error: (err) => {
        this.saving = false;
        this.errorMessage = err?.error?.error || 'Có lỗi xảy ra khi điều chỉnh kho.';
      }
    });
  }
}
