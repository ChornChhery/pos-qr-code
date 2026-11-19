import { beforeEach, describe, expect, it } from "vitest";
import { cartItemCount, estimateTotals, lineKey, MAX_LINE_QUANTITY, toRequestItems, useCart, type NewCartLine } from "@/store/cart";

const latte = (overrides: Partial<NewCartLine> = {}): NewCartLine => ({
  productId: "latte",
  name: "Iced Latte",
  quantity: 1,
  optionIds: ["large"],
  optionLabels: ["Large"],
  notes: "",
  unitUsd: 3,
  unitKhr: 12000,
  ...overrides,
});

beforeEach(() => {
  useCart.setState({ tableNumber: null, lines: [], lastOrderId: null, addedCount: 0 });
});

describe("cart store", () => {
  it("adds a line", () => {
    useCart.getState().addLine(latte());

    expect(useCart.getState().lines).toHaveLength(1);
    expect(useCart.getState().lines[0]).toMatchObject({ productId: "latte", quantity: 1 });
  });

  it("merges the same product, options, and note into one line", () => {
    useCart.getState().addLine(latte({ quantity: 2 }));
    useCart.getState().addLine(latte({ quantity: 3 }));

    expect(useCart.getState().lines).toHaveLength(1);
    expect(useCart.getState().lines[0]?.quantity).toBe(5);
  });

  it("keeps different options or notes as separate lines", () => {
    useCart.getState().addLine(latte());
    useCart.getState().addLine(latte({ optionIds: ["regular"], optionLabels: ["Regular"] }));
    useCart.getState().addLine(latte({ notes: "no ice" }));

    expect(useCart.getState().lines).toHaveLength(3);
  });

  it("ignores option order and note case when merging", () => {
    expect(lineKey("p", ["b", "a"], " No Ice ")).toBe(lineKey("p", ["a", "b"], "no ice"));
  });

  it("caps the quantity of a line", () => {
    useCart.getState().addLine(latte({ quantity: 90 }));
    useCart.getState().addLine(latte({ quantity: 90 }));

    expect(useCart.getState().lines[0]?.quantity).toBe(MAX_LINE_QUANTITY);
  });

  it("changes a quantity and removes the line at zero", () => {
    useCart.getState().addLine(latte());
    const key = useCart.getState().lines[0]!.key;

    useCart.getState().setQuantity(key, 4);
    expect(useCart.getState().lines[0]?.quantity).toBe(4);

    useCart.getState().setQuantity(key, 0);
    expect(useCart.getState().lines).toHaveLength(0);
  });

  it("removes a line and clears the cart", () => {
    useCart.getState().addLine(latte());
    useCart.getState().addLine(latte({ productId: "tea", optionIds: [] }));
    useCart.getState().removeLine(useCart.getState().lines[0]!.key);
    expect(useCart.getState().lines).toHaveLength(1);

    useCart.getState().clearCart();
    expect(useCart.getState().lines).toHaveLength(0);
  });

  it("keeps the table and the last order when the cart is cleared", () => {
    useCart.getState().setTable(7);
    useCart.getState().setLastOrder("order-1");
    useCart.getState().addLine(latte());
    useCart.getState().clearCart();

    expect(useCart.getState().tableNumber).toBe(7);
    expect(useCart.getState().lastOrderId).toBe("order-1");
  });

  it("counts adds so the cart bar can react", () => {
    useCart.getState().addLine(latte());
    useCart.getState().addLine(latte());

    expect(useCart.getState().addedCount).toBe(2);
  });

  it("trims and limits notes", () => {
    useCart.getState().addLine(latte({ notes: `  ${"x".repeat(500)}  ` }));

    expect(useCart.getState().lines[0]?.notes).toHaveLength(200);
  });
});

describe("cart helpers", () => {
  it("counts items and estimates totals in both currencies", () => {
    useCart.getState().addLine(latte({ quantity: 2 }));
    useCart.getState().addLine(latte({ productId: "tea", optionIds: [], unitUsd: 1.5, unitKhr: 6000, quantity: 3 }));
    const { lines } = useCart.getState();

    expect(cartItemCount(lines)).toBe(5);
    expect(estimateTotals(lines)).toEqual({ usd: 10.5, khr: 42000 });
  });

  it("builds the request the server expects", () => {
    useCart.getState().addLine(latte({ notes: "no ice" }));
    useCart.getState().addLine(latte({ productId: "tea", optionIds: [], optionLabels: [] }));

    expect(toRequestItems(useCart.getState().lines)).toEqual([
      { productId: "latte", quantity: 1, modifierOptionIds: ["large"], notes: "no ice" },
      { productId: "tea", quantity: 1, modifierOptionIds: [] },
    ]);
  });
});
