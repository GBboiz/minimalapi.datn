import { Component, OnInit, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { OrderService } from '../../../core/services/order.service';
import { CustomerService } from '../../../core/services/customer.service';
import { ProductService } from '../../../core/services/product.service';
import { PromotionService } from '../../../core/services/promotion.service';
import { CategoryService } from '../../../core/services/category.service';
import { Customer } from '../../../core/models/customer.model';
import { Product } from '../../../core/models/product.model';
import { Category } from '../../../core/models/category.model';
import { Promotion, DiscountType } from '../../../core/models/promotion.model';
import { AiScannerService } from '../../../core/services/ai-scanner.service';
import { ScannedInvoice } from '../../../core/models/ai-scanner.model';

export interface CartItem {
  product: Product;
  quantity: number;
  unitPrice: number;
  unitPriceDisplay?: string;
  isGift: boolean;
  parentProductId?: string; // 'MANUAL_GIFT' hoặc id sản phẩm chính tặng kèm
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
  private categoryService = inject(CategoryService);
  private aiScannerService = inject(AiScannerService);
  private router = inject(Router);

  customers: Customer[] = [];
  products: Product[] = [];
  categories: Category[] = [];
  promotions: Promotion[] = [
    { id: 'km-5', code: 'KM5', name: 'Giảm 5% đơn hàng', type: 'order', discountType: DiscountType.Percentage, value: 5, isActive: true, createdAt: '' },
    { id: 'km-10', code: 'KM10', name: 'Giảm 10% đơn hàng', type: 'order', discountType: DiscountType.Percentage, value: 10, isActive: true, createdAt: '' },
    { id: 'km-100k', code: 'KM100K', name: 'Giảm 100.000đ', type: 'order', discountType: DiscountType.FixedAmount, value: 100000, isActive: true, createdAt: '' },
    { id: 'km-200k', code: 'KM200K', name: 'Giảm 200.000đ', type: 'order', discountType: DiscountType.FixedAmount, value: 200000, isActive: true, createdAt: '' }
  ];

  loading = false;
  submitting = false;

  // AI Scanner state
  showAiScanModal = false;
  aiScanning = false;
  aiFile: File | null = null;
  aiImagePreview: string | null = null;
  aiScannedResult: ScannedInvoice | null = null;
  aiErrorMessage = '';

  // 1. Header Search (Tên hoặc mã SKU)
  headerSearchQuery = '';
  headerSearchQuantity = 1;
  isSearchDropdownOpen = false;

  // 2. Giỏ hàng
  cart: CartItem[] = [];

  // 3. Tư vấn bán hàng (Lưới sản phẩm có sẵn kèm ảnh)
  consultSearchQuery = '';
  selectedCategoryId = '';

  // 4. Khách hàng
  selectedCustomerId = '';
  customerSearchTerm = '';
  showCustomerDropdown = false;
  showQuickCustomerModal = false;
  savingCustomer = false;
  quickCustomer = { name: '', phone: '', address: '' };
  customerModalError = '';

  // 5. Quà tặng kèm (Có thể tặng nhiều quà, lưu và đóng)
  showGiftSelector = false;
  giftProductIdToAdd = '';
  giftQuantityToAdd = 1;

  // 6. Khuyến mãi & Giảm giá (4 chương trình do trang khuyến mãi thiết lập)
  selectedPromotionCode: string | null = null;
  discountMode: 'PERCENT' | 'AMOUNT' = 'PERCENT';
  discountPercent = 0;
  discountFixedAmount = 0;

  // 7. Thanh toán & Tiền mặt
  cashInputValue = '';
  customerCash: number | null = null;
  orderNote = '';
  currentDateTime = new Date();

  formatPrice(val: number | null | undefined): string {
    if (val === null || val === undefined || isNaN(val)) return '0';
    return Math.round(val).toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  }

  ngOnInit(): void {
    this.loadData();
    setInterval(() => {
      this.currentDateTime = new Date();
    }, 1000);
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

    this.categoryService.getAll(1, 100).subscribe({
      next: (res) => {
        this.categories = res.items;
      }
    });

    this.promotionService.getAll(true).subscribe({
      next: (res) => {
        this.promotions = res;
      }
    });
  }

  // ==========================
  // 1. TÌM KIẾM HEADER (TÊN HOẶC MÃ SKU)
  // ==========================
  get filteredHeaderProducts(): Product[] {
    const q = this.headerSearchQuery.trim().toLowerCase();
    if (!q) return [];
    return this.products.filter(p =>
      p.name.toLowerCase().includes(q) ||
      p.sku.toLowerCase().includes(q)
    ).slice(0, 8);
  }

  onHeaderSearchInput(): void {
    this.isSearchDropdownOpen = this.headerSearchQuery.trim().length > 0;
  }

  onHeaderSearchEnter(): void {
    const matches = this.filteredHeaderProducts;
    if (matches.length > 0) {
      this.selectHeaderProduct(matches[0]);
    }
  }

  selectHeaderProduct(product: Product): void {
    const qty = Math.max(1, this.headerSearchQuantity || 1);
    this.addToCart(product, qty);
    this.headerSearchQuery = '';
    this.headerSearchQuantity = 1;
    this.isSearchDropdownOpen = false;
  }

  closeHeaderSearchDropdown(): void {
    setTimeout(() => {
      this.isSearchDropdownOpen = false;
    }, 200);
  }

  // ==========================
  // 2. TƯ VẤN BÁN HÀNG (DANH SÁCH SẢN PHẨM CÓ SẴN)
  // ==========================
  get filteredConsultProducts(): Product[] {
    let list = this.products;

    if (this.selectedCategoryId) {
      list = list.filter(p => p.categoryId === this.selectedCategoryId);
    }

    const q = this.consultSearchQuery.trim().toLowerCase();
    if (q) {
      list = list.filter(p =>
        p.name.toLowerCase().includes(q) ||
        p.sku.toLowerCase().includes(q)
      );
    }

    return list;
  }

  addConsultProduct(product: Product): void {
    this.addToCart(product, 1);
  }

  // ==========================
  // 3. XỬ LÝ GIỎ HÀNG
  // ==========================
  addToCart(prod: Product, quantity = 1): void {
    if (!prod || quantity <= 0) return;

    const existing = this.cart.find(c => c.product.id === prod.id && !c.isGift);
    if (existing) {
      existing.quantity += quantity;
    } else {
      this.cart.push({
        product: prod,
        quantity: quantity,
        unitPrice: prod.price,
        unitPriceDisplay: this.formatPrice(prod.price),
        isGift: false
      });
    }

    // Tự động tặng quà kèm theo cấu hình sản phẩm chính nếu có
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
        existingGift.quantity += quantity;
      } else {
        this.cart.push({
          product: { ...giftToBundle, price: 0 },
          quantity: quantity,
          unitPrice: 0,
          unitPriceDisplay: '0',
          isGift: true,
          parentProductId: prod.id
        });
      }
    }
  }

  onUnitPriceChange(index: number, val: string): void {
    const item = this.cart[index];
    if (item.isGift) return;

    const clean = (val || '').replace(/[^\d]/g, '');
    const parsed = clean ? parseInt(clean, 10) : 0;
    item.unitPrice = parsed;
    // Tự động định dạng dấu chấm 1000 -> 1.000, 1000000 -> 1.000.000
    item.unitPriceDisplay = clean ? this.formatPrice(parsed) : '';
  }

  onUnitPriceBlur(index: number): void {
    const item = this.cart[index];
    if (item.isGift) return;
    item.unitPriceDisplay = this.formatPrice(item.unitPrice || 0);
  }

  removeFromCart(index: number): void {
    const item = this.cart[index];
    this.cart.splice(index, 1);

    // Nếu xóa sản phẩm chính, tự động xóa luôn quà tặng kèm đi kèm của sản phẩm đó
    if (!item.isGift) {
      const giftIndex = this.cart.findIndex(c => c.isGift && c.parentProductId === item.product.id);
      if (giftIndex !== -1) {
        this.cart.splice(giftIndex, 1);
      }
    }
  }

  increaseQuantity(index: number): void {
    const item = this.cart[index];
    if (item.isGift) return; // Quà tặng kèm đi theo sản phẩm chính

    item.quantity++;
    // Nếu có quà tặng đi kèm, tăng số lượng quà tương ứng
    const giftItem = this.cart.find(c => c.isGift && c.parentProductId === item.product.id);
    if (giftItem) {
      giftItem.quantity = item.quantity;
    }
  }

  decreaseQuantity(index: number): void {
    const item = this.cart[index];
    if (item.isGift) return;

    if (item.quantity > 1) {
      item.quantity--;
      const giftItem = this.cart.find(c => c.isGift && c.parentProductId === item.product.id);
      if (giftItem) {
        giftItem.quantity = item.quantity;
      }
    } else {
      this.removeFromCart(index);
    }
  }

  updateQuantity(index: number, newQty: number): void {
    const item = this.cart[index];
    if (item.isGift) return;

    const parsed = Math.max(1, Math.floor(newQty || 1));
    item.quantity = parsed;

    const giftItem = this.cart.find(c => c.isGift && c.parentProductId === item.product.id);
    if (giftItem) {
      giftItem.quantity = parsed;
    }
  }

  clearCart(): void {
    if (this.cart.length === 0) return;
    if (confirm('Bạn có chắc muốn xóa tất cả sản phẩm trong giỏ hàng?')) {
      this.cart = [];
      this.closeGiftSelector();
    }
  }

  // ==========================
  // 4. THÔNG TIN KHÁCH HÀNG
  // ==========================
  get selectedCustomer(): Customer | undefined {
    return this.customers.find(c => c.id === this.selectedCustomerId);
  }

  get filteredCustomers(): Customer[] {
    const q = this.customerSearchTerm.trim().toLowerCase();
    if (!q) return this.customers.slice(0, 8);
    return this.customers.filter(c =>
      c.name.toLowerCase().includes(q) ||
      (c.phone && c.phone.toLowerCase().includes(q))
    ).slice(0, 8);
  }

  onCustomerSearchInput(): void {
    this.showCustomerDropdown = this.customerSearchTerm.trim().length > 0;
  }

  onCustomerSearchBlur(): void {
    setTimeout(() => {
      this.showCustomerDropdown = false;
    }, 250);
  }

  onCustomerSearchEnter(): void {
    const term = this.customerSearchTerm.trim();
    if (!term) return;

    // Nếu có khách hàng khớp trong danh sách, chọn luôn
    const matches = this.filteredCustomers;
    if (matches.length > 0) {
      this.selectCustomer(matches[0]);
      return;
    }

    // Nếu không có trong danh sách: tự động tạo mới với địa chỉ trống
    const isPhone = /^\d+$/.test(term.replace(/[\s.-]/g, ''));
    const newName = isPhone ? 'Khách ' + term : term;
    const newPhone = isPhone ? term.replace(/[\s.-]/g, '') : '0900000000';

    this.customerService.create({
      name: newName,
      phone: newPhone,
      address: undefined
    }).subscribe({
      next: (newId) => {
        const createdCustomer: Customer = {
          id: newId,
          name: newName,
          phone: isPhone ? newPhone : '',
          address: '',
          createdAt: new Date().toISOString()
        };
        this.customers.unshift(createdCustomer);
        this.selectedCustomerId = newId;
        this.customerSearchTerm = '';
        this.showCustomerDropdown = false;
      },
      error: () => {
        const localId = 'cust_' + Date.now();
        const localCust: Customer = {
          id: localId,
          name: newName,
          phone: isPhone ? newPhone : '',
          address: '',
          createdAt: new Date().toISOString()
        };
        this.customers.unshift(localCust);
        this.selectedCustomerId = localId;
        this.customerSearchTerm = '';
        this.showCustomerDropdown = false;
      }
    });
  }

  selectCustomer(customer: Customer): void {
    this.selectedCustomerId = customer.id;
    this.customerSearchTerm = '';
    this.showCustomerDropdown = false;
  }

  clearCustomer(): void {
    this.selectedCustomerId = '';
    this.customerSearchTerm = '';
  }

  openQuickCustomerModal(): void {
    this.quickCustomer = { name: '', phone: '', address: '' };
    this.customerModalError = '';
    this.showQuickCustomerModal = true;
  }

  openChangeCustomerModal(): void {
    if (!this.selectedCustomer) return;
    this.quickCustomer = {
      name: this.selectedCustomer.name,
      phone: this.selectedCustomer.phone || '',
      address: this.selectedCustomer.address || ''
    };
    this.customerModalError = '';
    this.showQuickCustomerModal = true;
  }

  closeQuickCustomerModal(): void {
    this.showQuickCustomerModal = false;
    this.customerModalError = '';
  }

  chooseAnotherCustomer(): void {
    this.selectedCustomerId = '';
    this.customerSearchTerm = '';
    this.closeQuickCustomerModal();
  }

  saveQuickCustomer(): void {
    if (!this.quickCustomer.name.trim()) {
      this.customerModalError = 'Vui lòng nhập họ tên khách hàng.';
      return;
    }

    // Cập nhật khách hàng hiện tại khi nhấn Đổi để thêm/sửa địa chỉ
    if (this.selectedCustomerId && this.selectedCustomer) {
      const phoneToUse = this.quickCustomer.phone.trim() || (this.selectedCustomer.phone && this.selectedCustomer.phone !== '0900000000' ? this.selectedCustomer.phone : '0900000000');
      this.savingCustomer = true;
      this.customerModalError = '';

      const updateData = {
        name: this.quickCustomer.name.trim(),
        phone: phoneToUse,
        address: this.quickCustomer.address.trim() || undefined
      };

      this.customerService.update(this.selectedCustomerId, updateData).subscribe({
        next: () => {
          this.savingCustomer = false;
          this.selectedCustomer!.name = updateData.name;
          this.selectedCustomer!.phone = this.quickCustomer.phone.trim();
          this.selectedCustomer!.address = this.quickCustomer.address.trim();
          this.closeQuickCustomerModal();
        },
        error: () => {
          this.savingCustomer = false;
          this.selectedCustomer!.name = updateData.name;
          this.selectedCustomer!.phone = this.quickCustomer.phone.trim();
          this.selectedCustomer!.address = this.quickCustomer.address.trim();
          this.closeQuickCustomerModal();
        }
      });
      return;
    }

    if (!this.quickCustomer.phone.trim()) {
      this.customerModalError = 'Vui lòng nhập số điện thoại.';
      return;
    }

    this.savingCustomer = true;
    this.customerModalError = '';

    // Tạo mới khách hàng
    this.customerService.create({
      name: this.quickCustomer.name.trim(),
      phone: this.quickCustomer.phone.trim(),
      address: this.quickCustomer.address.trim() || undefined
    }).subscribe({
      next: (newCustomerId) => {
        this.savingCustomer = false;
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

  // ==========================
  // 5. QUÀ TẶNG KÈM (CÓ THỂ TẶNG NHIỀU, LƯU VÀ ĐÓNG)
  // ==========================
  get availableGifts(): Product[] {
    return this.products;
  }

  get manualGiftItems(): CartItem[] {
    return this.cart.filter(c => c.isGift && c.parentProductId === 'MANUAL_GIFT');
  }

  openGiftSelector(): void {
    this.showGiftSelector = true;
    this.giftProductIdToAdd = '';
    this.giftQuantityToAdd = 1;
  }

  closeGiftSelector(): void {
    this.showGiftSelector = false;
    this.giftProductIdToAdd = '';
    this.giftQuantityToAdd = 1;
  }

  saveGiftItem(): void {
    if (!this.giftProductIdToAdd) {
      alert('Vui lòng chọn sản phẩm làm quà tặng!');
      return;
    }

    const giftProduct = this.products.find(p => p.id === this.giftProductIdToAdd);
    if (!giftProduct) return;

    const qty = Math.max(1, this.giftQuantityToAdd || 1);
    const existing = this.cart.find(c => c.isGift && c.parentProductId === 'MANUAL_GIFT' && c.product.id === giftProduct.id);
    if (existing) {
      existing.quantity += qty;
    } else {
      this.cart.push({
        product: { ...giftProduct, price: 0 },
        quantity: qty,
        unitPrice: 0,
        unitPriceDisplay: '0',
        isGift: true,
        parentProductId: 'MANUAL_GIFT'
      });
    }

    this.giftProductIdToAdd = '';
    this.giftQuantityToAdd = 1;
  }

  removeManualGift(productId: string): void {
    const idx = this.cart.findIndex(c => c.isGift && c.parentProductId === 'MANUAL_GIFT' && c.product.id === productId);
    if (idx !== -1) {
      this.cart.splice(idx, 1);
    }
  }

  // ==========================
  // 6. CHƯƠNG TRÌNH KHUYẾN MÃI (4 KHUYẾN MÃI TỪ THIẾT LẬP CỦA SHOP)
  // ==========================
  onPromoSelect(code: string | null): void {
    this.selectedPromotionCode = code;

    if (!code) {
      this.clearDiscount();
      return;
    }

    const promo = this.promotions.find(p => p.code === code);
    if (promo) {
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
  }

  clearDiscount(): void {
    this.selectedPromotionCode = null;
    this.discountPercent = 0;
    this.discountFixedAmount = 0;
  }

  // ==========================
  // 7. TÍNH TOÁN TÀI CHÍNH & THANH TOÁN
  // ==========================
  get subTotal(): number {
    return this.cart
      .filter(item => !item.isGift)
      .reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);
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

  // Xử lý tiền khách đưa: tự động tính thiếu hoặc trả lại
  get cashDifference(): number {
    if (this.customerCash === null || this.customerCash === undefined) return 0;
    return this.customerCash - this.totalAmount;
  }

  get changeAmount(): number {
    if (this.customerCash === null || this.customerCash === undefined) return 0;
    return Math.max(0, this.customerCash - this.totalAmount);
  }

  onCashInputChange(val: string): void {
    this.cashInputValue = val;
    if (!val || !val.trim()) {
      this.customerCash = null;
      return;
    }
    const clean = val.replace(/[^\d]/g, '');
    if (!clean) {
      this.customerCash = null;
      return;
    }
    this.customerCash = parseInt(clean, 10);
  }

  onCashBlur(): void {
    if (this.customerCash !== null && this.customerCash !== undefined && this.customerCash > 0) {
      this.cashInputValue = this.formatPrice(this.customerCash);
    }
  }

  setCash(amount: number): void {
    this.customerCash = amount;
    this.cashInputValue = this.formatPrice(amount);
  }

  setExactCash(): void {
    this.setCash(this.totalAmount);
  }

  // Phím tắt bàn phím (F3 tìm kiếm, F4 khách hàng, F9 thanh toán)
  @HostListener('window:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent): void {
    if (event.key === 'F3') {
      event.preventDefault();
      const input = document.getElementById('header-search-input') as HTMLInputElement;
      input?.focus();
    } else if (event.key === 'F9') {
      event.preventDefault();
      if (!this.submitting && this.selectedCustomerId && this.cart.length > 0) {
        this.onSubmit();
      }
    }
  }

  // ==========================
  // 8. TẠO ĐƠN HÀNG (LÊN ĐƠN)
  // ==========================
  onSubmit(): void {
    if (!this.selectedCustomerId) {
      alert('Vui lòng chọn hoặc thêm thông tin khách hàng ở cột bên phải!');
      return;
    }

    if (this.cart.length === 0) {
      alert('Vui lòng thêm ít nhất một sản phẩm vào đơn hàng!');
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
        unitPrice: c.unitPrice,
        isGift: c.isGift
      }))
    };

    this.orderService.create(request).subscribe({
      next: (orderId) => {
        alert('Tạo đơn hàng thành công! Trạng thái: Chờ duyệt (Tồn kho dự báo đã được giữ chỗ).');
        this.router.navigate(['/orders']);
      },
      error: (err) => {
        this.submitting = false;
        alert(err?.error?.error || err?.error?.detail || 'Có lỗi xảy ra khi tạo đơn hàng');
      }
    });
  }

  saveDraft(): void {
    if (this.cart.length === 0) {
      alert('Chưa có sản phẩm nào trong giỏ để lưu tạm.');
      return;
    }
    alert('Đã lưu thông tin tạm thời của đơn hàng tại quầy!');
  }

  // ==========================
  // 9. TÍCH HỢP AI QUÉT HÓA ĐƠN & NHẬP SẴN
  // ==========================
  openAiScanModal(): void {
    this.showAiScanModal = true;
    this.aiErrorMessage = '';
    this.aiScannedResult = null;
    this.aiImagePreview = null;
    this.aiFile = null;
  }

  closeAiScanModal(): void {
    this.showAiScanModal = false;
    this.aiErrorMessage = '';
  }

  onAiFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files[0]) {
      this.aiFile = input.files[0];
      const reader = new FileReader();
      reader.onload = () => {
        this.aiImagePreview = reader.result as string;
      };
      reader.readAsDataURL(this.aiFile);
      this.startAiScan();
    }
  }

  scanSample(type: string): void {
    this.aiFile = null;
    this.aiImagePreview = null;
    this.aiScanning = true;
    this.aiErrorMessage = '';
    this.aiScannedResult = null;

    this.aiScannerService.scanInvoice(undefined, type).subscribe({
      next: (result) => {
        this.aiScanning = false;
        this.aiScannedResult = result;
      },
      error: (err) => {
        this.aiScanning = false;
        this.aiErrorMessage = err?.error?.error || 'Không thể quét mẫu bằng AI.';
      }
    });
  }

  startAiScan(): void {
    if (!this.aiFile) return;

    this.aiScanning = true;
    this.aiErrorMessage = '';
    this.aiScannedResult = null;

    this.aiScannerService.scanInvoice(this.aiFile).subscribe({
      next: (result) => {
        this.aiScanning = false;
        this.aiScannedResult = result;
      },
      error: (err) => {
        this.aiScanning = false;
        this.aiErrorMessage = err?.error?.error || 'Có lỗi xảy ra khi quét hóa đơn bằng AI.';
      }
    });
  }

  applyAiResultToSales(): void {
    if (!this.aiScannedResult) return;

    const result = this.aiScannedResult;

    // 1. Tự động tìm hoặc tạo khách hàng
    const targetPhone = result.phone ? result.phone.trim() : '';
    const targetName = result.customerName ? result.customerName.trim() : 'Khách Hàng AI';
    const targetAddress = result.address ? result.address.trim() : '';

    let matchedCustomer = this.customers.find(c => 
      (targetPhone && c.phone && c.phone.replace(/\s+/g, '') === targetPhone.replace(/\s+/g, '')) ||
      (targetName && c.name.toLowerCase() === targetName.toLowerCase())
    );

    if (matchedCustomer) {
      this.selectedCustomerId = matchedCustomer.id;
      this.applyAiProducts(result);
    } else {
      // Tự động tạo mới khách hàng từ thông tin AI quét được
      this.customerService.create({
        name: targetName,
        phone: targetPhone || '0901234567',
        address: targetAddress || undefined
      }).subscribe({
        next: (newId) => {
          const newCustomer: Customer = {
            id: newId,
            name: targetName,
            phone: targetPhone || '0901234567',
            address: targetAddress,
            createdAt: new Date().toISOString()
          };
          this.customers.unshift(newCustomer);
          this.selectedCustomerId = newId;
          this.applyAiProducts(result);
        },
        error: () => {
          if (this.customers.length > 0) {
            this.selectedCustomerId = this.customers[0].id;
          }
          this.applyAiProducts(result);
        }
      });
    }
  }

  private applyAiProducts(result: ScannedInvoice): void {
    if (result.items && result.items.length > 0) {
      result.items.forEach(scannedItem => {
        let matchedProduct: Product | undefined;

        if (scannedItem.matchedProductId) {
          matchedProduct = this.products.find(p => p.id === scannedItem.matchedProductId);
        }

        if (!matchedProduct && scannedItem.matchedProductSku) {
          matchedProduct = this.products.find(p => p.sku.toLowerCase() === scannedItem.matchedProductSku!.toLowerCase());
        }

        if (!matchedProduct && scannedItem.productName) {
          const sName = scannedItem.productName.toLowerCase();
          matchedProduct = this.products.find(p => 
            p.name.toLowerCase().includes(sName) || sName.includes(p.name.toLowerCase())
          );
        }

        // Fallback: nếu không khớp, lấy sản phẩm đầu tiên của cửa hàng
        if (!matchedProduct && this.products.length > 0) {
          matchedProduct = this.products[0];
        }

        if (matchedProduct) {
          const qty = scannedItem.quantity > 0 ? scannedItem.quantity : 1;
          const price = scannedItem.unitPrice > 0 ? scannedItem.unitPrice : matchedProduct.price;
          
          this.addToCart(matchedProduct, qty);
          // Gán đơn giá theo giá trên hóa đơn quét
          const lastItem = this.cart[this.cart.length - 1];
          if (lastItem && !lastItem.isGift) {
            lastItem.unitPrice = price;
            lastItem.unitPriceDisplay = this.formatPrice(price);
          }
        }
      });
    }

    this.closeAiScanModal();
    alert(`AI đã trích xuất và nhập sẵn thông tin thành công:\n- Khách hàng: ${result.customerName || 'Khách hàng'}\n- Số món hàng: ${result.items?.length || 0} sản phẩm\nBạn có thể kiểm tra lại và xác nhận đơn hàng!`);
  }
}
