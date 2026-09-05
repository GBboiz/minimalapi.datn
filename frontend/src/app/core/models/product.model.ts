export interface Product {
  id: string;
  name: string;
  sku: string;
  stockQuantity: number;
  price: number;
  currency: string;
  categoryId: string;
  categoryName: string;
  description?: string;
  isActive: boolean;
  createdAt: Date;
}

export interface CreateProductRequest {
  sku: string;
  stockQuantity: number;
  name: string;
  price: number;
  currency: string;
  categoryId: string;
  description?: string;
}

export interface UpdateProductRequest {
  sku: string;
  stockQuantity: number;
  name: string;
  price: number;
  currency: string;
  categoryId: string;
  description?: string;
  isActive: boolean;
}
