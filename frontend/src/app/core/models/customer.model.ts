export interface Customer {
  id: string;
  name: string;
  phone: string;
  email?: string;
  address?: string;
  createdAt: string;
}

export interface CreateCustomerRequest {
  name: string;
  phone: string;
  email?: string;
  address?: string;
}

export interface UpdateCustomerRequest {
  name: string;
  phone: string;
  email?: string;
  address?: string;
}
