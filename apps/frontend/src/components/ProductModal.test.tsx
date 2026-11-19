import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { ProductModal, ruleHint } from "@/components/ProductModal";
import { createTranslator } from "@/lib/i18n/dictionaries";
import { useCart } from "@/store/cart";
import { extrasGroup, latte, renderWithProviders, sizeGroup } from "@/test-utils";

beforeEach(() => {
  useCart.setState({ tableNumber: null, lines: [], lastOrderId: null, addedCount: 0 });
});

describe("ProductModal", () => {
  it("shows the product, its groups, and the rules", () => {
    renderWithProviders(<ProductModal product={latte} onClose={() => {}} />);

    expect(screen.getByRole("dialog", { name: "Iced Latte" })).toBeInTheDocument();
    expect(screen.getByText("Size")).toBeInTheDocument();
    expect(screen.getByText("Required")).toBeInTheDocument();
    expect(screen.getByText("Choose 1")).toBeInTheDocument();
    expect(screen.getByText("Choose up to 2")).toBeInTheDocument();
    // Large and Extra shot both add $0.50, so two options show this price.
    expect(screen.getAllByText("+$0.50 · +2,000៛")).toHaveLength(2);
  });

  it("keeps Add to cart disabled until the required size is chosen", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductModal product={latte} onClose={() => {}} />);
    const add = screen.getByRole("button", { name: /Add to cart/ });

    expect(add).toBeDisabled();
    await user.click(screen.getByRole("radio", { name: /Regular/ }));

    expect(add).toBeEnabled();
  });

  it("updates the price with options and quantity", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductModal product={latte} onClose={() => {}} />);

    await user.click(screen.getByRole("radio", { name: /Large/ }));
    expect(screen.getByRole("button", { name: /Add to cart/ })).toHaveTextContent("$3.00");

    await user.click(screen.getByRole("button", { name: "Add one more" }));
    expect(screen.getByRole("button", { name: /Add to cart/ })).toHaveTextContent("$6.00");
  });

  it("swaps the size instead of adding a second one", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductModal product={latte} onClose={() => {}} />);

    await user.click(screen.getByRole("radio", { name: /Regular/ }));
    await user.click(screen.getByRole("radio", { name: /Large/ }));

    expect(screen.getByRole("radio", { name: /Regular/ })).not.toBeChecked();
    expect(screen.getByRole("radio", { name: /Large/ })).toBeChecked();
  });

  it("stops offering extras once the group maximum is reached", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ProductModal product={latte} onClose={() => {}} />);

    await user.click(screen.getByRole("checkbox", { name: /Extra shot/ }));
    await user.click(screen.getByRole("checkbox", { name: /Oat milk/ }));

    expect(screen.getByRole("checkbox", { name: /Vanilla syrup/ })).toBeDisabled();
  });

  it("adds the line to the cart and closes", async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderWithProviders(<ProductModal product={latte} onClose={onClose} />);

    await user.click(screen.getByRole("radio", { name: /Large/ }));
    await user.click(screen.getByRole("checkbox", { name: /Extra shot/ }));
    await user.type(screen.getByRole("textbox", { name: /Note for the kitchen/ }), "no ice");
    await user.click(screen.getByRole("button", { name: /Add to cart/ }));

    const [line] = useCart.getState().lines;
    expect(line).toMatchObject({
      productId: "prod-latte",
      quantity: 1,
      notes: "no ice",
      unitUsd: 3.5,
      unitKhr: 14000,
      optionLabels: ["Large", "Extra shot"],
    });
    expect([...(line?.optionIds ?? [])].sort()).toEqual(["opt-large", "opt-shot"]);
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("cannot add a sold-out product", () => {
    renderWithProviders(<ProductModal product={{ ...latte, isAvailable: false }} onClose={() => {}} />);

    expect(screen.getByRole("button", { name: /Add to cart/ })).toBeDisabled();
  });

  it("disables an option that is sold out", () => {
    const product = {
      ...latte,
      modifiers: [{ ...sizeGroup, options: [{ ...sizeGroup.options[0]!, isAvailable: false }, sizeGroup.options[1]!] }],
    };
    renderWithProviders(<ProductModal product={product} onClose={() => {}} />);

    expect(screen.getByRole("radio", { name: /Regular/ })).toBeDisabled();
  });

  it("is shown in Khmer when the language is Khmer", () => {
    renderWithProviders(<ProductModal product={latte} onClose={() => {}} />, "km");

    expect(screen.getByRole("button", { name: /បន្ថែមទៅកន្ត្រក/ })).toBeInTheDocument();
    expect(screen.getByText("ត្រូវជ្រើសរើស")).toBeInTheDocument();
  });
});

describe("ruleHint", () => {
  const t = createTranslator("en");

  it("describes each kind of rule", () => {
    expect(ruleHint(sizeGroup, t)).toBe("Choose 1");
    expect(ruleHint(extrasGroup, t)).toBe("Choose up to 2");
    expect(ruleHint({ ...extrasGroup, isRequired: true, minSelections: 1, maxSelections: 3 }, t)).toBe("Choose 1 to 3");
    expect(ruleHint({ ...extrasGroup, isRequired: true, minSelections: 2, maxSelections: 2 }, t)).toBe("Choose 2");
    expect(ruleHint({ ...extrasGroup, isRequired: true, minSelections: 1, maxSelections: 0 }, t)).toBe("Choose at least 1");
    expect(ruleHint({ ...extrasGroup, maxSelections: 0 }, t)).toBeNull();
  });
});
