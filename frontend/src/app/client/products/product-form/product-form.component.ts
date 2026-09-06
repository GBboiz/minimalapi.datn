import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { Category } from '../../../core/models/category.model';
import { Product } from '../../../core/models/product.model';

@Component({
  selector: 'app-product-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss'
})
export class ProductFormComponent implements OnInit {
  private fb = inject(FormBuilder);
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  productForm: FormGroup;
  categories: Category[] = [];
  allProducts: Product[] = [];
  isEditMode = false;
  productId: string | null = null;
  loading = false;
  submitting = false;

  currencies = ['VND', 'USD'];

  constructor() {
    this.productForm = this.fb.group({
      sku: ['', [Validators.required, Validators.maxLength(64)]],
      stockQuantity: [0, [Validators.required, Validators.min(0)]],
      name: ['', [Validators.required, Validators.maxLength(200)]],
      price: [0, [Validators.required, Validators.min(0)]],
      currency: ['VND', Validators.required],
      categoryId: ['', Validators.required],
      description: [''],
      giftProductId: [null],
      isActive: [true]
    });
  }

  ngOnInit(): void {
    this.loadCategories();
    this.loadAllProducts();

    this.productId = this.route.snapshot.paramMap.get('id');
    if (this.productId) {
      this.isEditMode = true;
      this.loadProduct(this.productId);
    }
  }

  loadCategories(): void {
    this.categoryService.getAll(1, 100).subscribe({
      next: (result) => {
        this.categories = result.items;
      }
    });
  }

  loadAllProducts(): void {
    this.productService.getAll(1, 100).subscribe({
      next: (result) => {
        this.allProducts = result.items;
      }
    });
  }

  get availableGiftProducts(): Product[] {
    return this.allProducts.filter(p => !this.productId || p.id !== this.productId);
  }

  loadProduct(id: string): void {
    this.loading = true;
    this.productService.getById(id).subscribe({
      next: (product) => {
        this.productForm.patchValue({
          name: product.name,
          sku: product.sku,
          stockQuantity: product.stockQuantity,
          price: product.price,
          currency: product.currency,
          categoryId: product.categoryId,
          description: product.description,
          giftProductId: product.giftProductId || null,
          isActive: product.isActive
        });
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.router.navigate(['/products']);
      }
    });
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }

    this.submitting = true;
    const formValue = this.productForm.value;

    if (this.isEditMode && this.productId) {
      this.productService.update(this.productId, formValue).subscribe({
        next: () => {
          alert('Cập nhật sản phẩm thành công');
          this.router.navigate(['/products']);
        },
        error: () => {
          this.submitting = false;
        }
      });
    } else {
      this.productService.create(formValue).subscribe({
        next: () => {
          alert('Tạo sản phẩm thành công');
          this.router.navigate(['/products']);
        },
        error: () => {
          this.submitting = false;
        }
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/products']);
  }

  get f() {
    return this.productForm.controls;
  }
}
