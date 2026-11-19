"use client";

import { cn } from "@/lib/cn";
import { useT } from "@/lib/i18n/LocaleProvider";
import { Price } from "@/components/Price";
import type { ProductDto } from "@/lib/types";

export function ProductRow({ product, onSelect }: { product: ProductDto; onSelect: (product: ProductDto) => void }) {
  const t = useT();
  const soldOut = !product.isAvailable;

  return (
    <li className="border-b border-line">
      <button
        type="button"
        disabled={soldOut}
        onClick={() => onSelect(product)}
        className={cn(
          "flex min-h-20 w-full items-center gap-4 py-4 text-left",
          soldOut ? "cursor-not-allowed opacity-60" : "hover:bg-surface",
        )}
      >
        <span className="min-w-0 flex-1">
          <span className="block text-lg font-semibold">{product.name}</span>
          {product.description ? <span className="mt-0.5 line-clamp-2 block text-muted">{product.description}</span> : null}
          {soldOut ? (
            <span className="mt-1 inline-block rounded bg-line px-2 py-0.5 text-sm font-semibold text-muted">
              {t("menu.soldOut")}
            </span>
          ) : null}
        </span>
        {product.imageUrl ? (
          <img
            src={product.imageUrl}
            alt=""
            loading="lazy"
            decoding="async"
            className="size-20 shrink-0 rounded-lg object-cover"
          />
        ) : null}
        <Price usd={product.price} khr={product.priceKhr} stacked className="shrink-0" />
      </button>
    </li>
  );
}
