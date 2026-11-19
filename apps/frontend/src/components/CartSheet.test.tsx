import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { CartSheet } from "@/components/CartSheet";
import * as api from "@/lib/api";
import type { CartDto, OrderDto } from "@/lib/types";
import { useCart } from "@/store/cart";
import { renderWithProviders } from "@/test-utils";

const push = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => ({ push, refresh: vi.fn() }) }));
vi.mock("@/lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("@/lib/api")>();
  return { ...original, priceCart: vi.fn(), checkout: vi.fn(), getTables: vi.fn() };
});

const priced: CartDto = {
  lines: [
    {
      productId: "latte",
      productName: "Iced Latte",
      quantity: 2,
      unitPrice: 3.5,
      unitPriceKhr: 14000,
      modifiers: [],
      notes: null,
      lineTotal: 7,
      lineTotalKhr: 28000,
    },
  ],
  subtotal: 7,
  tax: 0,
  discount: 0,
  total: 7,
  totalKhr: 28000,
  usdToKhrRate: 4000,
};

const created = { id: "11111111-2222-4333-8444-555555555555", status: "PENDING" } as OrderDto;

function fillCart(tableNumber: number | null = 4) {
  useCart.setState({
    tableNumber,
    lastOrderId: null,
    addedCount: 0,
    lines: [
      {
        key: "latte|large|",
        productId: "latte",
        name: "Iced Latte",
        quantity: 2,
        optionIds: ["large"],
        optionLabels: ["Large"],
        notes: "",
        unitUsd: 3.5,
        unitKhr: 14000,
      },
    ],
  });
}

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(api.priceCart).mockResolvedValue(priced);
  vi.mocked(api.getTables).mockResolvedValue([{ id: "t1", tableNumber: 1, capacity: 4, status: "AVAILABLE" }]);
});

describe("CartSheet", () => {
  it("shows an empty state when the cart is empty", () => {
    useCart.setState({ lines: [], tableNumber: 4 });
    renderWithProviders(<CartSheet open onOpenChange={() => {}} />);

    expect(screen.getByText("Your cart is empty")).toBeInTheDocument();
  });

  it("shows the server total once pricing finishes and enables ordering", async () => {
    fillCart();
    renderWithProviders(<CartSheet open onOpenChange={() => {}} />);

    const order = screen.getByRole("button", { name: "Place order" });
    expect(order).toBeDisabled();

    await waitFor(() => expect(order).toBeEnabled());
    expect(screen.getAllByText("$7.00").length).toBeGreaterThan(0);
    expect(screen.getAllByText("28,000៛").length).toBeGreaterThan(0);
  });

  it("sends the cart to the server and opens the order page", async () => {
    const user = userEvent.setup();
    fillCart();
    vi.mocked(api.checkout).mockResolvedValue(created);
    renderWithProviders(<CartSheet open onOpenChange={() => {}} />);
    await waitFor(() => expect(screen.getByRole("button", { name: "Place order" })).toBeEnabled());

    await user.click(screen.getByRole("button", { name: "Place order" }));

    await waitFor(() => expect(push).toHaveBeenCalledWith(`/order/${created.id}`));
    expect(api.checkout).toHaveBeenCalledWith({
      tableNumber: 4,
      items: [{ productId: "latte", quantity: 2, modifierOptionIds: ["large"] }],
    });
    expect(useCart.getState().lines).toHaveLength(0);
    expect(useCart.getState().lastOrderId).toBe(created.id);
  });

  it("creates only one order when the button is tapped twice quickly", async () => {
    const user = userEvent.setup();
    fillCart();
    vi.mocked(api.checkout).mockImplementation(() => new Promise((resolve) => setTimeout(() => resolve(created), 50)));
    renderWithProviders(<CartSheet open onOpenChange={() => {}} />);
    await waitFor(() => expect(screen.getByRole("button", { name: "Place order" })).toBeEnabled());

    const button = screen.getByRole("button", { name: "Place order" });
    await user.dblClick(button);

    await waitFor(() => expect(push).toHaveBeenCalled());
    expect(api.checkout).toHaveBeenCalledTimes(1);
  });

  it("explains a stock conflict and keeps the cart", async () => {
    const user = userEvent.setup();
    fillCart();
    vi.mocked(api.checkout).mockRejectedValue(new api.ApiError(409, "conflict"));
    renderWithProviders(<CartSheet open onOpenChange={() => {}} />);
    await waitFor(() => expect(screen.getByRole("button", { name: "Place order" })).toBeEnabled());

    await user.click(screen.getByRole("button", { name: "Place order" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Stock just changed");
    expect(useCart.getState().lines).toHaveLength(1);
    expect(push).not.toHaveBeenCalled();
  });

  it("shows the server's cart problems and blocks ordering", async () => {
    fillCart();
    vi.mocked(api.priceCart).mockRejectedValue(new api.ApiError(400, "The cart has problems.", ["Only 1 of Iced Latte left."]));
    renderWithProviders(<CartSheet open onOpenChange={() => {}} />);

    expect(await screen.findByText("Only 1 of Iced Latte left.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Place order" })).toBeDisabled();
  });

  it("offers a retry when prices cannot be checked", async () => {
    fillCart();
    vi.mocked(api.priceCart).mockRejectedValue(new api.ApiError(0, "Network error"));
    renderWithProviders(<CartSheet open onOpenChange={() => {}} />);

    expect(await screen.findByText("We could not check the prices")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Try again" })).toBeInTheDocument();
  });

  it("asks for a table when the guest did not scan one", async () => {
    const user = userEvent.setup();
    fillCart(null);
    renderWithProviders(<CartSheet open onOpenChange={() => {}} />);

    const select = await screen.findByRole("combobox", { name: "Choose your table" });
    await waitFor(() => expect(select).toBeEnabled());
    expect(screen.getByRole("button", { name: "Place order" })).toBeDisabled();

    await user.selectOptions(select, "1");

    expect(useCart.getState().tableNumber).toBe(1);
  });

  it("lets the guest change a quantity and remove a line", async () => {
    const user = userEvent.setup();
    fillCart();
    renderWithProviders(<CartSheet open onOpenChange={() => {}} />);

    await user.click(screen.getByRole("button", { name: "Add one more" }));
    expect(useCart.getState().lines[0]?.quantity).toBe(3);

    await user.click(screen.getByRole("button", { name: "Remove Iced Latte" }));
    expect(useCart.getState().lines).toHaveLength(0);
  });
});
