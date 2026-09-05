export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  unitPrice: number;
  currency: string;
  quantity: number;
  totalPrice: number;
}

export interface OrderSummary {
  id: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  code: string;
  status: string;
  totalAmount: number;
  currency: string;
  totalItems: number;
  createdAt: string;
}

export interface OrderDetail {
  id: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  customerAddress?: string;
  code: string;
  status: string;
  totalAmount: number;
  currency: string;
  createdAt: string;
  items: OrderItem[];
}

export interface CreateOrderItemRequest {
  productId: string;
  quantity: number;
}

export interface CreateOrderRequest {
  customerId: string;
  items: CreateOrderItemRequest[];
}
