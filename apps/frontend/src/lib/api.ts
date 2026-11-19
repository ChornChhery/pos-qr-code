import { apiBaseUrl } from "@/lib/config";
import type {
  ApiEnvelope,
  CartDto,
  CartItemRequest,
  CategoryDto,
  CheckoutRequest,
  MenuData,
  OrderDto,
  ProductDto,
  TableDto,
} from "@/lib/types";

const REQUEST_TIMEOUT_MS = 15_000;

export class ApiError extends Error {
  readonly status: number;
  readonly errors: string[];

  constructor(status: number, message: string, errors: string[] = []) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errors = errors;
  }

  /** True when the server could not be reached at all. */
  get isNetwork(): boolean {
    return this.status === 0;
  }
}

function withTimeout(ms: number): { signal: AbortSignal; done: () => void } {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), ms);
  return { signal: controller.signal, done: () => clearTimeout(timer) };
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const timeout = withTimeout(REQUEST_TIMEOUT_MS);
  let response: Response;

  try {
    response = await fetch(`${apiBaseUrl()}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        ...(init.body ? { "Content-Type": "application/json" } : {}),
        ...init.headers,
      },
      signal: timeout.signal,
    });
  } catch {
    throw new ApiError(0, "Network error");
  } finally {
    timeout.done();
  }

  let body: ApiEnvelope<T> | null = null;
  try {
    body = (await response.json()) as ApiEnvelope<T>;
  } catch {
    body = null;
  }

  if (!response.ok || !body || !body.success || body.data === null) {
    throw new ApiError(response.status, body?.message || "Request failed", body?.errors ?? []);
  }

  return body.data;
}

export function getCategories(init?: RequestInit): Promise<CategoryDto[]> {
  return request<CategoryDto[]>("/api/catalog/categories", init);
}

/** Sold-out products are included so the menu can show them greyed out. */
export function getProducts(init?: RequestInit): Promise<ProductDto[]> {
  return request<ProductDto[]>("/api/catalog/products?includeSoldOut=true", init);
}

export async function getMenu(init?: RequestInit): Promise<MenuData> {
  const [categories, products] = await Promise.all([getCategories(init), getProducts(init)]);
  return { categories, products };
}

export function getTables(): Promise<TableDto[]> {
  return request<TableDto[]>("/api/orders/tables");
}

/** Asks the server to price a cart. The server total is the one that counts. */
export function priceCart(items: CartItemRequest[]): Promise<CartDto> {
  return request<CartDto>("/api/orders/cart", { method: "POST", body: JSON.stringify({ items }) });
}

export function checkout(payload: CheckoutRequest): Promise<OrderDto> {
  return request<OrderDto>("/api/orders/checkout", { method: "POST", body: JSON.stringify(payload) });
}

export function getOrder(id: string): Promise<OrderDto> {
  return request<OrderDto>(`/api/orders/${encodeURIComponent(id)}`);
}
