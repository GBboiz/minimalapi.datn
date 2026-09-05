export interface ScannedInvoiceItem {
  productName: string;
  quantity: number;
  unitPrice: number;
  total: number;
  matchedProductId?: string | null;
  matchedProductSku?: string | null;
  currentStock: number;
  isMatched: boolean;
}

export interface ScannedInvoice {
  customerName: string;
  phone: string;
  address: string;
  invoiceCode?: string | null;
  items: ScannedInvoiceItem[];
  totalAmount: number;
  aiConfidence: number;
  aiSource: string;
}

export interface ConfirmScannedOrderItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface ConfirmScannedOrderRequest {
  customerName: string;
  phone: string;
  address: string;
  items: ConfirmScannedOrderItem[];
}

export interface ConfirmScannedOrderResponse {
  orderId: string;
  orderCode: string;
  totalAmount: number;
  status: string;
  customerName: string;
  syncedToOutbox: boolean;
}
