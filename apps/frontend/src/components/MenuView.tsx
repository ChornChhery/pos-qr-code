"use client";

import { useQuery } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { CartBar } from "@/components/CartBar";
import { CartSheet } from "@/components/CartSheet";
import { CategoryTabs, sectionId } from "@/components/CategoryTabs";
import { MenuSkeleton } from "@/components/MenuSkeleton";
import { ProductModal } from "@/components/ProductModal";
import { ProductRow } from "@/components/ProductRow";
import { EmptyState, ErrorState } from "@/components/States";
import { getMenu } from "@/lib/api";
import { useT } from "@/lib/i18n/LocaleProvider";
import type { MenuData, ProductDto } from "@/lib/types";
import { useCart } from "@/store/cart";
import { useCartHydrated } from "@/store/hydration";

interface MenuViewProps {
  /** Menu rendered on the server. Null when the server could not reach the API; the browser then fetches it. */
  initialMenu: MenuData | null;
  /** Table number from the QR code link (?table=12), already checked on the server. */
  initialTable: number | null;
}

export function MenuView({ initialMenu, initialTable }: MenuViewProps) {
  const t = useT();
  const hydrated = useCartHydrated();
  const setTable = useCart((state) => state.setTable);
  const [selected, setSelected] = useState<ProductDto | null>(null);
  const [cartOpen, setCartOpen] = useState(false);

  // Apply the QR code's table after the saved cart has loaded, so the saved value cannot overwrite it.
  useEffect(() => {
    if (hydrated && initialTable !== null) {
      setTable(initialTable);
    }
  }, [hydrated, initialTable, setTable]);

  const menu = useQuery({
    queryKey: ["menu"],
    queryFn: () => getMenu(),
    initialData: initialMenu ?? undefined,
    staleTime: 30_000,
    refetchInterval: 60_000,
  });

  const sections = useMemo(() => {
    const data = menu.data;
    if (!data) {
      return [];
    }
    return [...data.categories]
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .map((category) => ({
        category,
        products: data.products.filter((product) => product.categoryId === category.id),
      }))
      .filter((section) => section.products.length > 0);
  }, [menu.data]);

  if (menu.isPending) {
    return <MenuSkeleton />;
  }

  if (menu.isError && !menu.data) {
    return <ErrorState title={t("menu.loadError")} hint={t("menu.loadErrorHint")} onRetry={() => void menu.refetch()} />;
  }

  if (sections.length === 0) {
    return <EmptyState title={t("menu.empty")} />;
  }

  return (
    <>
      <CategoryTabs categories={sections.map((section) => section.category)} />
      <div className="mx-auto max-w-3xl px-4 pb-28">
        {sections.map(({ category, products }) => (
          <section key={category.id} id={sectionId(category.id)} aria-labelledby={`${sectionId(category.id)}-title`} className="scroll-mt-32 pt-6">
            <h2 id={`${sectionId(category.id)}-title`} className="text-2xl font-bold">
              {category.name}
            </h2>
            {category.description ? <p className="text-muted">{category.description}</p> : null}
            <ul>
              {products.map((product) => (
                <ProductRow key={product.id} product={product} onSelect={setSelected} />
              ))}
            </ul>
          </section>
        ))}
      </div>

      {selected ? <ProductModal key={selected.id} product={selected} onClose={() => setSelected(null)} /> : null}
      <CartBar onOpen={() => setCartOpen(true)} />
      <CartSheet open={cartOpen} onOpenChange={setCartOpen} />
    </>
  );
}
