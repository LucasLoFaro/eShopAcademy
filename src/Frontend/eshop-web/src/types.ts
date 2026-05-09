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
basketClientId: string;
items: { productID: string; quantity: number; price: number }[];
  shippingAddress: {
    street: string;
    number: string;
    additionalInformation: string;
    zipCode: string;
    city: string;
  };
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

// Notifications
export interface NotificationMessage {
  id: string;
  subject: string;
  body: string;
  type: string;
  orderId: string;
  isRead: boolean;
  createdAt: string;
}

// Search
export interface ProductSearchFilter {
  searchText?: string;
  category?: string;
  minPrice?: number;
  maxPrice?: number;
  deals?: boolean;
  inStock?: boolean;
  minRating?: number;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// Customer
export interface Customer {
  id: string;
  name: string;
  mail: string;
  phone: string;
  address: Address; // Legacy - for backward compatibility
  savedAddresses: SavedAddress[];
  status: string;
}

export interface SavedAddress {
  id: string;
  customerId: string;
  description: string; // e.g., "Home", "Work", "Office"
  address: Address;
  isDefault: boolean;
}

export interface Address {
  street: string;
  number: string;
  additionalInformation: string;
  zipCode: string;
  city: string;
  state?: string;
  country: string;
}

// Payment Methods
export interface PaymentMethod {
  id: string;
  name: string;
  icon: string;
  enabled: boolean;
}

// Checkout State
export interface CheckoutData {
  address?: Address;
  paymentMethod?: string;
}
