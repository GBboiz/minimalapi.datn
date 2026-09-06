export interface InventoryItem {
  id: string;
  sku: string;
  name: string;
  categoryId: string;
  categoryName: string;
  stockQuantity: number;    // Tồn kho thực tế
  reservedQuantity: number; // Đang giữ chỗ (chờ duyệt)
  forecastStock: number;    // Tồn kho dự báo
  price: number;
  currency: string;
  isActive: boolean;
  giftProductId?: string | null;
  giftProductName?: string | null;
}

export interface InventorySummary {
  totalProducts: number;
  totalActualStock: number;
  totalReservedStock: number;
  totalForecastStock: number;
  lowStockAlertCount: number;
  items: InventoryItem[];
}

export interface AdjustInventoryRequest {
  productId: string;
  stockChange: number;
  reason?: string;
}
