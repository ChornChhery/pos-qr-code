"use client";

import { useSyncExternalStore } from "react";
import { useCart } from "@/store/cart";

/** True once the saved cart has been read from the browser. Until then the cart looks empty. */
export function useCartHydrated(): boolean {
  return useSyncExternalStore(
    (onChange) => useCart.persist.onFinishHydration(onChange),
    () => useCart.persist.hasHydrated(),
    () => false,
  );
}
