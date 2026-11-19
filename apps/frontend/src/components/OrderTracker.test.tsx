import { screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { OrderTracker } from "@/components/OrderTracker";
import * as api from "@/lib/api";
import type { OrderDto, OrderStatus } from "@/lib/types";
import { renderWithProviders } from "@/test-utils";

vi.mock("@/lib/api", async (importOriginal) => {
  const original = await importOriginal<typeof import("@/lib/api")>();
  return { ...original, getOrder: vi.fn() };
});

function order(status: OrderStatus): OrderDto {
  return {
    id: "11111111-2222-4333-8444-555555555555",
    tableNumber: 4,
    status,
    notes: null,
    subtotal: 7,
    tax: 0,
    discount: 0,
    total: 7,
    totalKhr: 28000,
    createdAt: "2026-11-18T10:00:00Z",
    updatedAt: "2026-11-18T10:00:00Z",
    completedAt: null,
    items: [
      {
        id: "i1",
        productId: "latte",
        productName: "Iced Latte",
        quantity: 2,
        unitPrice: 3.5,
        unitPriceKhr: 14000,
        modifiers: [{ optionId: "large", groupName: "Size", name: "Large", priceAdd: 0.5 }],
        notes: "no ice",
        totalPrice: 7,
        totalPriceKhr: 28000,
      },
    ],
  };
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe("OrderTracker", () => {
  it("shows a skeleton first", () => {
    vi.mocked(api.getOrder).mockReturnValue(new Promise(() => {}));
    renderWithProviders(<OrderTracker id="o1" />);

    expect(screen.getByRole("status")).toHaveAttribute("aria-busy", "true");
  });

  it("shows the items, both totals, and the current step", async () => {
    vi.mocked(api.getOrder).mockResolvedValue(order("COOKING"));
    renderWithProviders(<OrderTracker id="o1" />);

    expect(await screen.findByText("2 × Iced Latte")).toBeInTheDocument();
    expect(screen.getByText("Large")).toBeInTheDocument();
    expect(screen.getByText("no ice")).toBeInTheDocument();
    expect(screen.getAllByText("$7.00").length).toBeGreaterThan(0);
    expect(screen.getAllByText("28,000៛").length).toBeGreaterThan(0);
    expect(screen.getByText("The kitchen is preparing your food.")).toBeInTheDocument();

    const current = screen.getAllByRole("listitem").find((li) => li.getAttribute("aria-current") === "step");
    expect(current).toHaveTextContent("Cooking");
  });

  it.each<[OrderStatus, string]>([
    ["PENDING", "The kitchen has your order."],
    ["READY", "Your food is ready."],
    ["COMPLETED", "Thank you. Enjoy your meal!"],
  ])("explains the %s status", async (status, hint) => {
    vi.mocked(api.getOrder).mockResolvedValue(order(status));
    renderWithProviders(<OrderTracker id="o1" />);

    expect(await screen.findByText(hint)).toBeInTheDocument();
  });

  it("shows a cancelled order clearly", async () => {
    vi.mocked(api.getOrder).mockResolvedValue(order("CANCELLED"));
    renderWithProviders(<OrderTracker id="o1" />);

    expect(await screen.findByText("This order was cancelled. Please ask a staff member.")).toBeInTheDocument();
  });

  it("says when the order does not exist, without a retry button", async () => {
    vi.mocked(api.getOrder).mockRejectedValue(new api.ApiError(404, "Order not found."));
    renderWithProviders(<OrderTracker id="o1" />);

    expect(await screen.findByText("We could not find this order")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Try again" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Back to the menu" })).toHaveAttribute("href", "/");
  });

  it("offers a retry when the server cannot be reached", async () => {
    vi.mocked(api.getOrder).mockRejectedValue(new api.ApiError(0, "Network error"));
    renderWithProviders(<OrderTracker id="o1" />);

    expect(await screen.findByText("We could not load your order status", {}, { timeout: 5000 })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Try again" })).toBeInTheDocument();
  });

  it("is shown in Khmer", async () => {
    vi.mocked(api.getOrder).mockResolvedValue(order("READY"));
    renderWithProviders(<OrderTracker id="o1" />, "km");

    expect(await screen.findByText("អាហាររបស់អ្នករួចរាល់ហើយ។")).toBeInTheDocument();
  });
});
