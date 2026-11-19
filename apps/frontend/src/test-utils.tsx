import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, type RenderResult } from "@testing-library/react";
import type { ReactElement } from "react";
import { LocaleProvider } from "@/lib/i18n/LocaleProvider";
import type { Locale } from "@/lib/i18n/dictionaries";
import type { ModifierDto, ProductDto } from "@/lib/types";

export function renderWithProviders(ui: ReactElement, locale: Locale = "en"): RenderResult {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  return render(
    <LocaleProvider locale={locale}>
      <QueryClientProvider client={client}>{ui}</QueryClientProvider>
    </LocaleProvider>,
  );
}

export const sizeGroup: ModifierDto = {
  id: "mod-size",
  groupName: "Size",
  isRequired: true,
  minSelections: 1,
  maxSelections: 1,
  displayOrder: 1,
  options: [
    { id: "opt-regular", name: "Regular", priceAdd: 0, priceAddKhr: 0, displayOrder: 1, isAvailable: true },
    { id: "opt-large", name: "Large", priceAdd: 0.5, priceAddKhr: 2000, displayOrder: 2, isAvailable: true },
  ],
};

export const extrasGroup: ModifierDto = {
  id: "mod-extras",
  groupName: "Extras",
  isRequired: false,
  minSelections: 0,
  maxSelections: 2,
  displayOrder: 2,
  options: [
    { id: "opt-shot", name: "Extra shot", priceAdd: 0.5, priceAddKhr: 2000, displayOrder: 1, isAvailable: true },
    { id: "opt-oat", name: "Oat milk", priceAdd: 0.6, priceAddKhr: 2400, displayOrder: 2, isAvailable: true },
    { id: "opt-syrup", name: "Vanilla syrup", priceAdd: 0.3, priceAddKhr: 1200, displayOrder: 3, isAvailable: true },
  ],
};

export const latte: ProductDto = {
  id: "prod-latte",
  categoryId: "cat-drinks",
  name: "Iced Latte",
  description: "Cold coffee with milk",
  price: 2.5,
  priceKhr: 10000,
  imageUrl: null,
  isAvailable: true,
  modifiers: [sizeGroup, extrasGroup],
};
