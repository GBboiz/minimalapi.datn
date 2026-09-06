export enum DiscountType {
  Percentage = 1,
  FixedAmount = 2
}

export interface Promotion {
  id: string;
  code: string;
  name: string;
  type: string;
  discountType: DiscountType;
  value: number;
  description?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreatePromotionRequest {
  code: string;
  name: string;
  discountType: DiscountType;
  value: number;
  description?: string;
}

export interface UpdatePromotionRequest {
  name: string;
  discountType: DiscountType;
  value: number;
  description?: string;
  isActive: boolean;
}
