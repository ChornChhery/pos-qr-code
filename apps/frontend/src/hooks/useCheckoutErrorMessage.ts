import { ApiError } from "@/lib/api";
import type { TranslateFn } from "@/lib/i18n/dictionaries";

export interface CheckoutProblem {
  message: string;
  /** Server messages (English) that say exactly what to fix, for example "Only 3 of Lemon Tea left." */
  details: string[];
}

export function describeCheckoutError(error: unknown, t: TranslateFn): CheckoutProblem {
  if (error instanceof ApiError) {
    if (error.status === 409) {
      return { message: t("cart.stockChanged"), details: [] };
    }
    if (error.status === 404) {
      return { message: t("cart.tableNotFound"), details: [] };
    }
    if (error.status === 400) {
      return { message: t("cart.fixItems"), details: error.errors };
    }
  }
  return { message: t("cart.orderFailed"), details: [] };
}
