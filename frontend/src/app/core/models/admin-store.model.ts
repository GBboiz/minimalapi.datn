export interface AdminStore {
  id: string;
  name: string;
  slug: string;
  createdAt: string;
  ownerId: string;
  ownerName: string;
  ownerEmail: string;
  totalProducts: number;
  totalOrders: number;
  totalRevenue: number;
  currency: string;
  totalCustomers: number;
  totalMembers: number;
}

export interface CreateStoreRequest {
  name: string;
  ownerFullName: string;
  ownerEmail: string;
  password?: string;
}

export interface UpdateStoreRequest {
  name: string;
  slug?: string;
  ownerFullName?: string;
}

export interface SwitchStoreResponse {
  accessToken: string;
  storeId: string;
  storeName: string;
}
