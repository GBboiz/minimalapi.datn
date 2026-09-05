import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminStoreService } from '../../core/services/admin-store.service';
import { AuthService } from '../../core/services/auth.service';
import { AdminStore, CreateStoreRequest, UpdateStoreRequest } from '../../core/models/admin-store.model';

@Component({
  selector: 'app-store-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './store-list.component.html',
  styleUrl: './store-list.component.scss'
})
export class StoreListComponent implements OnInit {
  private adminStoreService = inject(AdminStoreService);
  auth = inject(AuthService);
  private router = inject(Router);

  stores: AdminStore[] = [];
  filteredStores: AdminStore[] = [];
  loading = true;
  searchTerm = '';

  // Modal Tạo mới
  showCreateModal = false;
  creating = false;
  newStore: CreateStoreRequest = {
    name: '',
    ownerFullName: '',
    ownerEmail: '',
    password: ''
  };

  // Modal Sửa
  showEditModal = false;
  editing = false;
  editStoreData: { id: string; name: string; slug: string; ownerFullName: string; ownerEmail: string } = {
    id: '',
    name: '',
    slug: '',
    ownerFullName: '',
    ownerEmail: ''
  };

  switchingStoreId: string | null = null;
  deletingStoreId: string | null = null;
  successMsg = '';
  errorMsg = '';

  ngOnInit(): void {
    this.loadStores();
  }

  loadStores(): void {
    this.loading = true;
    this.adminStoreService.getAll().subscribe({
      next: (data) => {
        this.stores = data;
        this.applyFilter();
        this.loading = false;
      },
      error: (err) => {
        this.errorMsg = err.error?.error || 'Không thể tải danh sách cửa hàng.';
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    if (!this.searchTerm.trim()) {
      this.filteredStores = [...this.stores];
      return;
    }
    const term = this.searchTerm.trim().toLowerCase();
    this.filteredStores = this.stores.filter(s =>
      s.name.toLowerCase().includes(term) ||
      s.slug.toLowerCase().includes(term) ||
      s.ownerName.toLowerCase().includes(term) ||
      s.ownerEmail.toLowerCase().includes(term)
    );
  }

  // --- THÊM SHOP ---
  openCreateModal(): void {
    this.newStore = {
      name: '',
      ownerFullName: '',
      ownerEmail: '',
      password: ''
    };
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  onCreateStore(): void {
    if (!this.newStore.name.trim() || !this.newStore.ownerEmail.trim() || !this.newStore.ownerFullName.trim()) {
      this.errorMsg = 'Vui lòng điền đầy đủ Tên cửa hàng, Họ tên chủ sở hữu và Email.';
      return;
    }

    this.creating = true;
    this.adminStoreService.create(this.newStore).subscribe({
      next: (created) => {
        this.creating = false;
        this.showCreateModal = false;
        this.successMsg = `Đã tạo thành công cửa hàng "${created.name}"!`;
        this.loadStores();
        setTimeout(() => this.successMsg = '', 5000);
      },
      error: (err) => {
        this.creating = false;
        this.errorMsg = err.error?.error || 'Có lỗi xảy ra khi tạo cửa hàng.';
      }
    });
  }

  // --- SỬA SHOP ---
  openEditModal(store: AdminStore): void {
    this.editStoreData = {
      id: store.id,
      name: store.name,
      slug: store.slug,
      ownerFullName: store.ownerName,
      ownerEmail: store.ownerEmail
    };
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
  }

  onUpdateStore(): void {
    if (!this.editStoreData.name.trim()) {
      this.errorMsg = 'Tên cửa hàng không được để trống.';
      return;
    }

    this.editing = true;
    const req: UpdateStoreRequest = {
      name: this.editStoreData.name,
      slug: this.editStoreData.slug,
      ownerFullName: this.editStoreData.ownerFullName
    };

    this.adminStoreService.update(this.editStoreData.id, req).subscribe({
      next: (updated) => {
        this.editing = false;
        this.showEditModal = false;
        this.successMsg = `Đã cập nhật thành công cửa hàng "${updated.name}"!`;
        // Cập nhật lại storeName trong auth session nếu đang là shop hiện tại
        if (this.auth.storeName === this.editStoreData.name || this.auth.storeName === updated.name) {
          this.auth.updateSession(this.auth.token!, updated.name, this.auth.isAdmin);
        }
        this.loadStores();
        setTimeout(() => this.successMsg = '', 5000);
      },
      error: (err) => {
        this.editing = false;
        this.errorMsg = err.error?.error || 'Có lỗi xảy ra khi cập nhật cửa hàng.';
      }
    });
  }

  // --- XÓA SHOP ---
  onDeleteStore(store: AdminStore): void {
    if (this.stores.length <= 1) {
      alert('Không thể xóa cửa hàng duy nhất còn lại trên hệ thống.');
      return;
    }

    const confirmMsg = `Bạn có chắc chắn muốn xóa vĩnh viễn cửa hàng "${store.name}" (slug: ${store.slug})?\n\nLƯU Ý: Toàn bộ sản phẩm, danh mục, đơn hàng và dữ liệu của cửa hàng này sẽ bị xóa.`;
    if (!confirm(confirmMsg)) {
      return;
    }

    this.deletingStoreId = store.id;
    this.adminStoreService.delete(store.id).subscribe({
      next: () => {
        this.deletingStoreId = null;
        this.successMsg = `Đã xóa thành công cửa hàng "${store.name}".`;
        // Nếu xóa đúng shop đang active, tự động switch sang shop đầu tiên còn lại
        if (this.isCurrentStore(store.name)) {
          const remaining = this.stores.filter(s => s.id !== store.id);
          if (remaining.length > 0) {
            this.onSwitchStore(remaining[0]);
            return;
          }
        }
        this.loadStores();
        setTimeout(() => this.successMsg = '', 5000);
      },
      error: (err) => {
        this.deletingStoreId = null;
        this.errorMsg = err.error?.error || 'Có lỗi xảy ra khi xóa cửa hàng.';
      }
    });
  }

  // --- CHUYỂN SHOP (SWITCH STORE) ---
  onSwitchStore(store: AdminStore): void {
    this.switchingStoreId = store.id;
    this.adminStoreService.switchStore(store.id).subscribe({
      next: (res) => {
        this.auth.updateSession(res.accessToken, res.storeName, true);
        this.successMsg = `Đã chuyển ngữ cảnh làm việc sang: ${res.storeName}`;
        this.switchingStoreId = null;
        setTimeout(() => {
          window.location.href = '/';
        }, 600);
      },
      error: (err) => {
        this.switchingStoreId = null;
        this.errorMsg = err.error?.error || 'Không thể chuyển đổi cửa hàng.';
      }
    });
  }

  get totalStoresCount(): number {
    return this.stores.length;
  }

  get totalPlatformRevenue(): number {
    return this.stores.reduce((sum, s) => sum + s.totalRevenue, 0);
  }

  get totalPlatformOrders(): number {
    return this.stores.reduce((sum, s) => sum + s.totalOrders, 0);
  }

  get totalPlatformProducts(): number {
    return this.stores.reduce((sum, s) => sum + s.totalProducts, 0);
  }

  isCurrentStore(storeName: string): boolean {
    return this.auth.storeName === storeName;
  }
}
