export interface AuthResponse {
  accessToken: string;
  storeId: string;
  storeName: string;
  email?: string;
  fullName?: string;
  isAdmin?: boolean;
}
