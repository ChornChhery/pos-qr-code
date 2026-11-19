// Response shapes of the POS API (see AI_CONTEXT.md section 5 and docs/PHASE_2_NOTES.md).

export interface ApiEnvelope<T> {
  success: boolean;
  data: T | null;
  message: string;
  errors: string[];
}

export interface CategoryDto {
  id: string;
  name: string;
  description: string | null;
  displayOrder: number;
}

export interface ModifierOptionDto {
  id: string;
  name: string;
  priceAdd: number;
  priceAddKhr: number;
  displayOrder: number;
  isAvailable: boolean;
}

export interface ModifierDto {
  id: string;
  groupName: string;
  isRequired: boolean;
  minSelections: number;
  /** 0 means no upper limit. */
  maxSelections: number;
  displayOrder: number;
  options: ModifierOptionDto[];
}

export interface ProductDto {
  id: string;
  categoryId: string;
  name: string;
  description: string | null;
  price: number;
  priceKhr: number;
  imageUrl: string | null;
  /** False when switched off or out of stock. */
  isAvailable: boolean;
  modifiers: ModifierDto[];
}

export interface TableDto {
  id: string;
  tableNumber: number;
  capacity: number;
  status: string;
}

export interface SelectedModifierDto {
  optionId: string;
  groupName: string;
  name: string;
  priceAdd: number;
}

export interface CartLineDto {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  unitPriceKhr: number;
  modifiers: SelectedModifierDto[];
  notes: string | null;
  lineTotal: number;
  lineTotalKhr: number;
}

export interface CartDto {
  lines: CartLineDto[];
  subtotal: number;
  tax: number;
  discount: number;
  total: number;
  totalKhr: number;
  usdToKhrRate: number;
}

export type OrderStatus = "PENDING" | "COOKING" | "READY" | "COMPLETED" | "CANCELLED";

export interface OrderItemDto {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  unitPriceKhr: number;
  modifiers: SelectedModifierDto[];
  notes: string | null;
  totalPrice: number;
  totalPriceKhr: number;
}

export interface OrderDto {
  id: string;
  tableNumber: number;
  status: OrderStatus;
  notes: string | null;
  subtotal: number;
  tax: number;
  discount: number;
  total: number;
  totalKhr: number;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
  items: OrderItemDto[];
}

export interface CartItemRequest {
  productId: string;
  quantity: number;
  modifierOptionIds: string[];
  notes?: string;
}

export interface CheckoutRequest {
  tableNumber: number;
  notes?: string;
  items: CartItemRequest[];
}

export interface MenuData {
  categories: CategoryDto[];
  products: ProductDto[];
}
