import { expect, test } from "@playwright/test";

// Runs against the mock API (see playwright.config.ts), so it needs no backend.
// Mock menu: Iced Latte $2.50 (Size required, Extras up to 2), Coconut Ice Cream is sold out.

test("a guest scans a table link, orders a latte, and follows the order", async ({ page }) => {
  await page.goto("/?table=3");

  await expect(page.getByRole("heading", { name: "Drinks" })).toBeVisible();
  await expect(page.getByText("Table 3").first()).toBeVisible();
  await expect(page.getByText("10,000៛").first()).toBeVisible();
  await expect(page.getByRole("button", { name: /Coconut Ice Cream/ })).toBeDisabled();

  await page.getByRole("button", { name: /Iced Latte/ }).click();
  const product = page.getByRole("dialog", { name: "Iced Latte" });
  const add = product.getByRole("button", { name: /Add to cart/ });

  await expect(add).toBeDisabled();
  await product.getByRole("radio", { name: /Large/ }).check();
  await product.getByRole("checkbox", { name: /Extra shot/ }).check();
  await expect(add).toBeEnabled();
  await add.click();

  await page.getByRole("button", { name: /View cart \(1\)/ }).click();
  const cart = page.getByRole("dialog", { name: "Your cart" });
  await expect(cart.getByText("$3.50").first()).toBeVisible();
  await expect(cart.getByText("14,000៛").first()).toBeVisible();

  const placeOrder = cart.getByRole("button", { name: "Place order" });
  await expect(placeOrder).toBeEnabled();
  await placeOrder.click();

  await expect(page).toHaveURL(/\/order\/[0-9a-f-]{36}$/);
  await expect(page.getByText("The kitchen has your order.")).toBeVisible();
  await expect(page.getByText("1 × Iced Latte")).toBeVisible();

  // The page polls every 5 seconds. The mock finishes the order after about 4.5 seconds.
  await expect(page.getByText("Thank you. Enjoy your meal!")).toBeVisible({ timeout: 30_000 });
});

test("the cart survives a page reload", async ({ page }) => {
  await page.goto("/?table=5");
  await page.getByRole("button", { name: /Lemon Tea/ }).click();
  await page.getByRole("dialog", { name: "Lemon Tea" }).getByRole("button", { name: /Add to cart/ }).click();

  await expect(page.getByRole("button", { name: /View cart \(1\)/ })).toBeVisible();
  await page.reload();

  await expect(page.getByRole("button", { name: /View cart \(1\)/ })).toBeVisible();
  await expect(page.getByText("Table 5").first()).toBeVisible();
});

test("the guest can switch the menu to Khmer", async ({ page }) => {
  await page.goto("/");
  await page.getByRole("button", { name: "ភាសាខ្មែរ" }).click();

  await expect(page.locator("html")).toHaveAttribute("lang", "km");
  await expect(page.getByRole("navigation", { name: "ប្រភេទម្ហូប" })).toBeVisible();
  await expect(page.getByText("អស់ពីស្តុក")).toBeVisible();
});
