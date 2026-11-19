import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";
import { roundUsd } from "@/lib/money";
import type { CartItemRequest } from "@/lib/types";

export const MAX_LINE_QUANTITY = 99;
export const MAX_LINES = 50;
export const MAX_NOTE_LENGTH = 200;

export interface CartLine {
  /** Same product, options, and note share a key, so adding them again raises the quantity. */
  key: string;
  productId: string;
  name: string;
  quantity: number;
  optionIds: string[];
  optionLabels: string[];
  notes: string;
  /** Display-only estimate. The server prices the cart. */
  unitUsd: number;
  unitKhr: number;
}

export type NewCartLine = Omit<CartLine, "key">;

export function lineKey(productId: string, optionIds: readonly string[], notes: string): string {
  return `${productId}|${[...optionIds].sort().join(",")}|${notes.trim().toLowerCase()}`;
}

export interface CartState {
  tableNumber: number | null;
  lines: CartLine[];
  /** Id of the most recent order, so a guest can find it again. */
  lastOrderId: string | null;
  /** Bumped on every add, so the cart bar can confirm it to the guest. */
  addedCount: number;
  setTable: (tableNumber: number | null) => void;
  addLine: (line: NewCartLine) => void;
  setQuantity: (key: string, quantity: number) => void;
  removeLine: (key: string) => void;
  clearCart: () => void;
  setLastOrder: (orderId: string) => void;
}

export const useCart = create<CartState>()(
  persist(
    (set) => ({
      tableNumber: null,
      lines: [],
      lastOrderId: null,
      addedCount: 0,

      setTable: (tableNumber) => set({ tableNumber }),

      addLine: (line) =>
        set((state) => {
          const notes = line.notes.trim().slice(0, MAX_NOTE_LENGTH);
          const key = lineKey(line.productId, line.optionIds, notes);
          const existing = state.lines.find((l) => l.key === key);

          if (existing) {
            return {
              lines: state.lines.map((l) =>
                l.key === key ? { ...l, quantity: Math.min(l.quantity + line.quantity, MAX_LINE_QUANTITY) } : l,
              ),
              addedCount: state.addedCount + 1,
            };
          }

          if (state.lines.length >= MAX_LINES) {
            return state;
          }

          return {
            lines: [
              ...state.lines,
              { ...line, notes, key, quantity: Math.min(Math.max(line.quantity, 1), MAX_LINE_QUANTITY) },
            ],
            addedCount: state.addedCount + 1,
          };
        }),

      setQuantity: (key, quantity) =>
        set((state) => ({
          lines:
            quantity < 1
              ? state.lines.filter((l) => l.key !== key)
              : state.lines.map((l) => (l.key === key ? { ...l, quantity: Math.min(quantity, MAX_LINE_QUANTITY) } : l)),
        })),

      removeLine: (key) => set((state) => ({ lines: state.lines.filter((l) => l.key !== key) })),

      clearCart: () => set({ lines: [] }),

      setLastOrder: (orderId) => set({ lastOrderId: orderId }),
    }),
    {
      name: "pos-cart",
      version: 1,
      storage: createJSONStorage(() => localStorage),
      // Rehydrated in Providers after the first render, so server and browser HTML match.
      skipHydration: true,
      partialize: (state) => ({
        tableNumber: state.tableNumber,
        lines: state.lines,
        lastOrderId: state.lastOrderId,
      }),
    },
  ),
);

export function cartItemCount(lines: readonly CartLine[]): number {
  return lines.reduce((sum, line) => sum + line.quantity, 0);
}

/** Display-only total in USD and riel. */
export function estimateTotals(lines: readonly CartLine[]): { usd: number; khr: number } {
  return {
    usd: roundUsd(lines.reduce((sum, line) => sum + line.unitUsd * line.quantity, 0)),
    khr: lines.reduce((sum, line) => sum + line.unitKhr * line.quantity, 0),
  };
}

export function toRequestItems(lines: readonly CartLine[]): CartItemRequest[] {
  return lines.map((line) => ({
    productId: line.productId,
    quantity: line.quantity,
    modifierOptionIds: line.optionIds,
    ...(line.notes ? { notes: line.notes } : {}),
  }));
}
