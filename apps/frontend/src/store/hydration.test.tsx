import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useCart } from "@/store/cart";
import { useCartHydrated } from "@/store/hydration";

describe("useCartHydrated", () => {
  it("turns true after the saved cart is loaded, and restores the saved table", async () => {
    localStorage.setItem("pos-cart", JSON.stringify({ state: { tableNumber: 8, lines: [], lastOrderId: null }, version: 1 }));
    const { result } = renderHook(() => useCartHydrated());
    expect(result.current).toBe(false);

    await act(async () => {
      await useCart.persist.rehydrate();
    });

    expect(result.current).toBe(true);
    expect(useCart.getState().tableNumber).toBe(8);
  });
});
