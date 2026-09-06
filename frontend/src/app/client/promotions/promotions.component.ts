import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PromotionService } from '../../core/services/promotion.service';
import { Promotion, DiscountType, CreatePromotionRequest, UpdatePromotionRequest } from '../../core/models/promotion.model';

@Component({
  selector: 'app-promotions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './promotions.component.html',
  styleUrl: './promotions.component.scss'
})
export class PromotionsComponent implements OnInit {
  private promotionService = inject(PromotionService);

  promotions: Promotion[] = [];
  loading = false;
  saving = false;
  searchTerm = '';
  filterType = 'ALL'; // ALL, 1 (Percentage), 2 (FixedAmount)

  // Presets gợi ý
  presets = [
    {
      code: 'KM5',
      name: 'Giảm 5% đơn hàng',
      type: DiscountType.Percentage,
      value: 5,
      description: 'Áp dụng giảm 5% trên tổng giá trị sản phẩm',
      badge: '5%',
      color: 'primary'
    },
    {
      code: 'KM10',
      name: 'Giảm 10% đơn hàng',
      type: DiscountType.Percentage,
      value: 10,
      description: 'Áp dụng giảm 10% cho khách hàng thân thiết',
      badge: '10%',
      color: 'info'
    },
    {
      code: 'KM100K',
      name: 'Giảm 100.000đ tiền mặt',
      type: DiscountType.FixedAmount,
      value: 100000,
      description: 'Giảm trực tiếp 100.000đ cho đơn hàng',
      badge: '100K',
      color: 'success'
    },
    {
      code: 'KM200K',
      name: 'Giảm 200.000đ tiền mặt',
      type: DiscountType.FixedAmount,
      value: 200000,
      description: 'Giảm trực tiếp 200.000đ cho đơn hàng lớn',
      badge: '200K',
      color: 'warning'
    }
  ];

  // Modal
  showModal = false;
  isEditing = false;
  editingId: string | null = null;
  formModel = {
    code: '',
    name: '',
    discountType: DiscountType.Percentage,
    value: 5,
    description: '',
    isActive: true
  };
  DiscountTypeEnum = DiscountType;

  errorMessage = '';
  successMessage = '';

  ngOnInit(): void {
    this.loadPromotions();
  }

  loadPromotions(): void {
    this.loading = true;
    this.promotionService.getAll().subscribe({
      next: (list) => {
        this.promotions = list;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Không thể tải danh sách khuyến mãi.';
        this.loading = false;
      }
    });
  }

  get filteredPromotions(): Promotion[] {
    return this.promotions.filter(p => {
      const matchSearch = !this.searchTerm ||
        p.code.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        p.name.toLowerCase().includes(this.searchTerm.toLowerCase());

      if (!matchSearch) return false;

      if (this.filterType !== 'ALL') {
        return p.discountType.toString() === this.filterType;
      }

      return true;
    });
  }

  openCreateModal(preset?: any): void {
    this.errorMessage = '';
    this.isEditing = false;
    this.editingId = null;

    if (preset) {
      this.formModel = {
        code: preset.code,
        name: preset.name,
        discountType: preset.type,
        value: preset.value,
        description: preset.description,
        isActive: true
      };
    } else {
      this.formModel = {
        code: '',
        name: '',
        discountType: DiscountType.Percentage,
        value: 10,
        description: '',
        isActive: true
      };
    }

    this.showModal = true;
  }

  openEditModal(p: Promotion): void {
    this.errorMessage = '';
    this.isEditing = true;
    this.editingId = p.id;
    this.formModel = {
      code: p.code,
      name: p.name,
      discountType: p.discountType,
      value: p.value,
      description: p.description || '',
      isActive: p.isActive
    };
    this.showModal = true;
  }

  closeModal(): void {
    this.showModal = false;
    this.editingId = null;
    this.errorMessage = '';
  }

  savePromotion(): void {
    if (!this.formModel.name.trim()) {
      this.errorMessage = 'Vui lòng nhập tên chương trình khuyến mãi.';
      return;
    }

    if (!this.isEditing && !this.formModel.code.trim()) {
      this.errorMessage = 'Vui lòng nhập mã khuyến mãi.';
      return;
    }

    if (this.formModel.value <= 0) {
      this.errorMessage = 'Giá trị giảm giá phải lớn hơn 0.';
      return;
    }

    if (this.formModel.discountType === DiscountType.Percentage && this.formModel.value > 100) {
      this.errorMessage = 'Phần trăm giảm giá không được vượt quá 100%.';
      return;
    }

    this.saving = true;
    this.errorMessage = '';

    if (this.isEditing && this.editingId) {
      const updateData: UpdatePromotionRequest = {
        name: this.formModel.name,
        discountType: this.formModel.discountType,
        value: this.formModel.value,
        description: this.formModel.description,
        isActive: this.formModel.isActive
      };

      this.promotionService.update(this.editingId, updateData).subscribe({
        next: () => {
          this.saving = false;
          this.successMessage = 'Đã cập nhật khuyến mãi thành công!';
          this.closeModal();
          this.loadPromotions();
          setTimeout(() => this.successMessage = '', 3500);
        },
        error: (err) => {
          this.saving = false;
          this.errorMessage = err?.error?.error || 'Có lỗi xảy ra.';
        }
      });
    } else {
      const createData: CreatePromotionRequest = {
        code: this.formModel.code.toUpperCase(),
        name: this.formModel.name,
        discountType: this.formModel.discountType,
        value: this.formModel.value,
        description: this.formModel.description
      };

      this.promotionService.create(createData).subscribe({
        next: () => {
          this.saving = false;
          this.successMessage = 'Đã tạo chương trình khuyến mãi mới!';
          this.closeModal();
          this.loadPromotions();
          setTimeout(() => this.successMessage = '', 3500);
        },
        error: (err) => {
          this.saving = false;
          this.errorMessage = err?.error?.error || 'Có lỗi xảy ra.';
        }
      });
    }
  }

  toggleActive(p: Promotion): void {
    const updateData: UpdatePromotionRequest = {
      name: p.name,
      discountType: p.discountType,
      value: p.value,
      description: p.description,
      isActive: !p.isActive
    };

    this.promotionService.update(p.id, updateData).subscribe({
      next: () => {
        p.isActive = !p.isActive;
        this.successMessage = `Đã ${p.isActive ? 'bật' : 'tắt'} khuyến mãi "${p.name}".`;
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        alert(err?.error?.error || 'Không thể cập nhật trạng thái.');
      }
    });
  }

  deletePromotion(p: Promotion): void {
    if (!confirm(`Bạn có chắc muốn xóa khuyến mãi "${p.name}" (${p.code})?`)) {
      return;
    }

    this.promotionService.delete(p.id).subscribe({
      next: () => {
        this.successMessage = `Đã xóa khuyến mãi "${p.name}".`;
        this.loadPromotions();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        alert(err?.error?.error || 'Không thể xóa khuyến mãi.');
      }
    });
  }
}
