import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, checkout, getMenu, getOrder, priceCart } from "@/lib/api";

function reply(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { "Content-Type": "application/json" } });
}

const ok = <T>(data: T) => ({ success: true, data, message: "", errors: [] });

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("api client", () => {
  it("returns the data of a successful response", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(reply(200, ok({ id: "o1", status: "PENDING" }))));

    await expect(getOrder("o1")).resolves.toMatchObject({ id: "o1", status: "PENDING" });
  });

  it("calls the API base URL with the right path and body", async () => {
    const fetchMock = vi.fn().mockResolvedValue(reply(200, ok({ lines: [] })));
    vi.stubGlobal("fetch", fetchMock);

    await priceCart([{ productId: "p1", quantity: 2, modifierOptionIds: [] }]);

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("http://localhost:5000/api/orders/cart");
    expect(init.method).toBe("POST");
    expect(JSON.parse(init.body as string)).toEqual({ items: [{ productId: "p1", quantity: 2, modifierOptionIds: [] }] });
  });

  it("loads categories and products together for the menu", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(reply(200, ok([{ id: "c1", name: "Drinks" }])))
      .mockResolvedValueOnce(reply(200, ok([{ id: "p1", name: "Tea" }])));
    vi.stubGlobal("fetch", fetchMock);

    const menu = await getMenu();

    expect(menu.categories).toHaveLength(1);
    expect(menu.products).toHaveLength(1);
    expect(fetchMock.mock.calls.map((call) => call[0])).toEqual([
      "http://localhost:5000/api/catalog/categories",
      "http://localhost:5000/api/catalog/products?includeSoldOut=true",
    ]);
  });

  it("turns a 400 response into an ApiError with the server messages", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(reply(400, { success: false, data: null, message: "The cart has problems.", errors: ["Only 3 of Lemon Tea left."] })),
    );

    const error = await checkout({ tableNumber: 1, items: [] }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 400, errors: ["Only 3 of Lemon Tea left."] });
  });

  it("reports a 404 with its status", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(reply(404, { success: false, data: null, message: "Order not found.", errors: [] })));

    await expect(getOrder("x")).rejects.toMatchObject({ status: 404, message: "Order not found." });
  });

  it("reports a network failure as status 0", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("Failed to fetch")));

    const error = (await getOrder("x").catch((e: unknown) => e)) as ApiError;

    expect(error.status).toBe(0);
    expect(error.isNetwork).toBe(true);
  });

  it("handles a response that is not JSON", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(new Response("<html>bad gateway</html>", { status: 502 })));

    await expect(getOrder("x")).rejects.toMatchObject({ status: 502 });
  });

  it("encodes the order id in the path", async () => {
    const fetchMock = vi.fn().mockResolvedValue(reply(200, ok({})));
    vi.stubGlobal("fetch", fetchMock);

    await getOrder("a/b?c");

    expect(fetchMock.mock.calls[0]?.[0]).toBe("http://localhost:5000/api/orders/a%2Fb%3Fc");
  });
});
