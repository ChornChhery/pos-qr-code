"use client";

import Link from "next/link";
import { LanguageToggle } from "@/components/LanguageToggle";
import { restaurant } from "@/lib/config";
import { useT } from "@/lib/i18n/LocaleProvider";
import { useCart } from "@/store/cart";
import { useCartHydrated } from "@/store/hydration";

export function Header() {
  const t = useT();
  const hydrated = useCartHydrated();
  const tableNumber = useCart((state) => state.tableNumber);
  const lastOrderId = useCart((state) => state.lastOrderId);

  return (
    <header className="sticky top-0 z-30 border-b border-line bg-white">
      <div className="mx-auto flex min-h-14 max-w-3xl items-center justify-between gap-3 px-4 py-1">
        <Link href="/" className="flex min-h-11 min-w-0 items-center gap-2">
          {restaurant.logoUrl ? <img src={restaurant.logoUrl} alt="" className="size-9 rounded-md object-cover" /> : null}
          <span className="truncate text-lg font-bold">{restaurant.name}</span>
        </Link>
        <div className="flex shrink-0 items-center gap-2">
          {hydrated && tableNumber !== null ? (
            <span className="rounded-full bg-brand-50 px-3 py-1 text-sm font-semibold text-brand-700">
              {t("app.table", { number: tableNumber })}
            </span>
          ) : null}
          {hydrated && lastOrderId ? (
            <Link
              href={`/order/${lastOrderId}`}
              className="inline-flex min-h-11 items-center rounded-lg px-2 text-sm font-semibold text-brand-700 underline"
            >
              {t("order.myOrder")}
            </Link>
          ) : null}
          <LanguageToggle />
        </div>
      </div>
    </header>
  );
}
