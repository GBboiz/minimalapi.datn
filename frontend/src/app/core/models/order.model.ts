export interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  unitPrice: number;
  currency: string;
  quantity: number;
  totalPrice: number;
  isGift?: boolean;
  unitPriceDisplay?: string;
}

export interface OrderSummary {
  id: string;
  customerId: string;
  customerName: string;
  customerPhone: string;
  code: string;
  status: string;
  subTotal?: number;
  discountPercent?: number;
  discountAmount?: number;
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
  subTotal?: number;
  discountPercent?: number;
  discountAmount?: number;
  totalAmount: number;
  currency: string;
  createdAt: string;
  items: OrderItem[];
}

export interface CreateOrderItemRequest {
  productId: string;
  quantity: number;
  isGift?: boolean;
  unitPrice?: number;
}

export interface CreateOrderRequest {
  customerId: string;
  items: CreateOrderItemRequest[];
  discountPercent?: number;
  discountAmount?: number;
  promotionCode?: string;
}

export interface UpdateOrderItemRequest {
  productId: string;
  quantity: number;
  isGift?: boolean;
  unitPrice?: number;
}

export interface UpdateOrderRequest {
  customerName?: string;
  customerPhone?: string;
  customerAddress?: string;
  items?: UpdateOrderItemRequest[];
  discountPercent?: number;
  discountAmount?: number;
}

