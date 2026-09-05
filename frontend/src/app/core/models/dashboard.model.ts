export interface LowStockProduct {
  id: string;
  name: string;
  sku: string;
  stockQuantity: number;
  price: number;
  currency: string;
}

export interface RecentOrder {
  id: string;
  code: string;
  customerName: string;
  totalAmount: number;
  currency: string;
  status: string;
  createdAt: string;
}

export interface DashboardStats {
  todayRevenue: number;
  todayOrders: number;
  todayCustomers: number;
  totalRevenue: number;
  currency: string;
  totalOrders: number;
  pendingOrders: number;
  confirmedOrders: number;
  completedOrders: number;
  cancelledOrders: number;
  totalCustomers: number;
  totalProducts: number;
  lowStockProductsCount: number;
  lowStockProducts: LowStockProduct[];
  recentOrders: RecentOrder[];
}
