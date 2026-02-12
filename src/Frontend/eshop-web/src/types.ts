// Products
export interface Product {
  id: string;
  name: string;
  price: number;
  description: string;
  imageUrl: string;
  categoryId: string;
  category?: Category;
  additionalImages?: string[];
  aboutHtml?: string;
  rating?: number;
  reviewCount?: number;
  isBestSeller?: boolean;
  isDeal?: boolean;
  dealPrice?: number | null;
  isNewRelease?: boolean;
  createdAt?: string;
  specs?: ProductSpec[];
  faqs?: ProductFaq[];
}

export interface ProductSpec {
  label: string;
  value: string;
}

export interface ProductFaq {
  question: string;
  answer: string;
}

export interface Category {
  id: string;
  name: string;
}

// Basket
export interface BasketItem {
  productID: string;
  quantity: number;
}

export interface BasketItemWithDetails {
  product: {
    id: string;
    name: string;
    price: number;
    stock: number;
  };
  quantity: number;
}

export interface BasketWithDetails {
  clientID: string;
  items: BasketItemWithDetails[];
}

// Orders
export interface OrderRequest {
  customerId: string;
  items: { productID: string; quantity: number; price: number }[];
}

export interface PlaceOrderResponse {
  orderId: string;
  paymentUrl: string;
  status: string;
}

export interface Order {
  id: string;
  status: OrderStatus;
  totalPrice: number;
  customerId: string;
  customer: OrderCustomerInfo;
  items: OrderItem[];
  payment: OrderPaymentInfo;
  shipping: OrderShippingInfo;
}

export interface OrderCustomerInfo {
  name: string;
  mail: string;
}

export interface OrderItem {
  productID: string;
  quantity: number;
  price: number;
  product: { name: string; imageUrl: string };
}

export interface OrderPaymentInfo {
  id: string;
  status: string;
  providerTransactionId: string;
  amount: number;
  paidAt?: string;
}

export interface OrderShippingInfo {
  status: string;
  destinationAddress: string;
  trackingNumber: string;
  carrier: string;
  trackingUrl: string;
  shippedAt?: string;
  deliveredAt?: string;
}

export type OrderStatus =
  | "Created"
  | "Paid"
  | "Confirmed"
  | "Processing"
  | "ReadyForPickup"
  | "Shipped"
  | "Delivered"
  | "Cancelled"
  | "Error";

// Customer
export interface Customer {
  id: string;
  name: string;
  mail: string;
  phone: string;
  address: Address;
  status: string;
}

export interface Address {
  street: string;
  number: string;
  additionalInformation: string;
  zipCode: string;
  city: string;
}
