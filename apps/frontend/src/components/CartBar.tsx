"use client";

import { ShoppingBag } from "lucide-react";
import { Price } from "@/components/Price";
import { useT } from "@/lib/i18n/LocaleProvider";
import { cartItemCount, estimateTotals, useCart } from "@/store/cart";
import { useCartHydrated } from "@/store/hydration";

/** Sticky bar at the bottom of the menu. Shows the count and an estimated total. */
export function CartBar({ onOpen }: { onOpen: () => void }) {
  const t = useT();
  const hydrated = useCartHydrated();
  const lines = useCart((state) => state.lines);
  const addedCount = useCart((state) => state.addedCount);

  const count = cartItemCount(lines);
  if (!hydrated || count === 0) {
    return null;
  }

  const totals = estimateTotals(lines);

  return (
    <div className="fixed inset-x-0 bottom-0 z-30 px-4 pb-[max(0.75rem,env(safe-area-inset-bottom))]">
      <button
        key={addedCount}
        type="button"
        onClick={onOpen}
        className="animate-bump mx-auto flex min-h-14 w-full max-w-3xl items-center justify-between gap-3 rounded-xl bg-brand-600 px-4 text-white shadow-lg hover:bg-brand-700"
      >
        <span className="flex items-center gap-2 text-lg font-semibold">
          <ShoppingBag aria-hidden="true" className="size-6" />
          {t("cart.view", { count })}
        </span>
        <Price usd={totals.usd} khr={totals.khr} prefix="≈ " className="[&>span]:!text-white" />
      </button>
      <span role="status" className="sr-only">
        {t("product.added")}
      </span>
    </div>
  );
}
