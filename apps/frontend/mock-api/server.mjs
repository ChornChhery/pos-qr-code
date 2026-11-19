// A tiny stand-in for the POS gateway, for working on the web menu without the backend.
//   npm run dev:mock          (listens on port 5000, same as the real gateway)
// It mimics the response envelope, riel prices, cart rules, and order progress. It is a development tool only.

import { createServer } from "node:http";
import { randomUUID } from "node:crypto";

const PORT = Number(process.env.MOCK_PORT ?? 5000);
const STEP_MS = Number(process.env.MOCK_STEP_MS ?? 4000);
const RATE = Number(process.env.MOCK_USD_TO_KHR ?? 4000);

const khr = (usd) => (usd <= 0 ? 0 : Math.round((usd * RATE) / 100) * 100);
const round2 = (n) => Math.round((n + Number.EPSILON) * 100) / 100;
const id = (n) => `00000000-0000-4000-8000-${String(n).padStart(12, "0")}`;

const option = (n, name, priceAdd, isAvailable = true) => ({ id: id(n), name, priceAdd, priceAddKhr: khr(priceAdd), displayOrder: n % 10, isAvailable });

const categories = [
  { id: id(1), name: "Drinks", description: null, displayOrder: 1 },
  { id: id(2), name: "Mains", description: null, displayOrder: 2 },
  { id: id(3), name: "Desserts", description: null, displayOrder: 3 },
];

const product = (n, categoryId, name, description, price, modifiers = [], isAvailable = true, stock = null) => ({
  id: id(n), categoryId, name, description, price, priceKhr: khr(price), imageUrl: null, isAvailable, stock, modifiers,
});

const products = [
  product(101, id(1), "Iced Latte", "Cold coffee with milk", 2.5, [
    { id: id(201), groupName: "Size", isRequired: true, minSelections: 1, maxSelections: 1, displayOrder: 1, options: [option(301, "Regular", 0), option(302, "Large", 0.5)] },
    { id: id(202), groupName: "Extras", isRequired: false, minSelections: 0, maxSelections: 2, displayOrder: 2, options: [option(303, "Extra shot", 0.5), option(304, "Oat milk", 0.6), option(305, "Vanilla syrup", 0.3, false)] },
  ]),
  product(102, id(1), "Lemon Tea", "Fresh lemon, served cold", 1.5, [], true, 50),
  product(103, id(2), "Beef Lok Lak", "Stir-fried beef with pepper dip and rice", 5.5, [
    { id: id(203), groupName: "Egg", isRequired: false, minSelections: 0, maxSelections: 1, displayOrder: 1, options: [option(306, "Fried egg", 0.5)] },
  ]),
  product(104, id(2), "Chicken Fried Rice", "Wok-fried rice with chicken and vegetables", 4, [
    { id: id(204), groupName: "Spice level", isRequired: true, minSelections: 1, maxSelections: 1, displayOrder: 1, options: [option(307, "Mild", 0), option(308, "Medium", 0), option(309, "Hot", 0)] },
  ]),
  product(105, id(3), "Mango Sticky Rice", "Sweet coconut sticky rice with ripe mango", 3, [], true, 20),
  product(106, id(3), "Coconut Ice Cream", "Two scoops", 2, [], false, 0),
];

const tables = Array.from({ length: 10 }, (_, i) => ({ id: id(900 + i), tableNumber: i + 1, capacity: 4, status: "AVAILABLE" }));
const orders = new Map();

const publicProduct = ({ stock: _stock, ...rest }) => rest;

function ok(res, status, data) {
  send(res, status, { success: true, data, message: "", errors: [] });
}

function fail(res, status, message, errors = []) {
  send(res, status, { success: false, data: null, message, errors });
}

function send(res, status, body) {
  res.writeHead(status, { "Content-Type": "application/json" });
  res.end(JSON.stringify(body));
}

function priceItems(items) {
  const errors = [];
  const lines = [];
  if (!Array.isArray(items) || items.length === 0) return { errors: ["The cart is empty."], lines };

  for (const item of items) {
    const p = products.find((x) => x.id === item.productId);
    if (!p) { errors.push(`Product ${item.productId} was not found.`); continue; }
    if (!p.isAvailable) { errors.push(`${p.name} is sold out.`); continue; }
    if (p.stock !== null && item.quantity > p.stock) { errors.push(`Only ${p.stock} of ${p.name} left.`); continue; }
    if (!Number.isInteger(item.quantity) || item.quantity < 1 || item.quantity > 99) { errors.push("Quantity must be between 1 and 99."); continue; }

    const chosen = new Set(item.modifierOptionIds ?? []);
    const picked = [];
    for (const group of p.modifiers) {
      const selected = group.options.filter((o) => chosen.has(o.id));
      const min = group.isRequired ? Math.max(group.minSelections, 1) : group.minSelections;
      if (selected.length < min) errors.push(`Choose at least ${min} for ${group.groupName} on ${p.name}.`);
      if (group.maxSelections > 0 && selected.length > group.maxSelections) errors.push(`Choose at most ${group.maxSelections} for ${group.groupName} on ${p.name}.`);
      for (const o of selected) picked.push({ optionId: o.id, groupName: group.groupName, name: o.name, priceAdd: o.priceAdd });
    }
    const unit = round2(p.price + picked.reduce((s, m) => s + m.priceAdd, 0));
    const lineTotal = round2(unit * item.quantity);
    lines.push({ productId: p.id, productName: p.name, quantity: item.quantity, unitPrice: unit, unitPriceKhr: khr(unit), modifiers: picked, notes: item.notes?.trim() || null, lineTotal, lineTotalKhr: khr(lineTotal) });
  }
  return { errors, lines };
}

function cartFrom(lines) {
  const subtotal = round2(lines.reduce((s, l) => s + l.lineTotal, 0));
  return { lines, subtotal, tax: 0, discount: 0, total: subtotal, totalKhr: khr(subtotal), usdToKhrRate: RATE };
}

function orderView(order) {
  const age = Date.now() - order.createdAtMs;
  const status = age < STEP_MS ? "PENDING" : age < STEP_MS * 2 ? "COOKING" : age < STEP_MS * 3 ? "READY" : "COMPLETED";
  return { ...order.dto, status, updatedAt: new Date().toISOString(), completedAt: status === "COMPLETED" ? new Date().toISOString() : null };
}

async function readBody(req) {
  const chunks = [];
  for await (const chunk of req) chunks.push(chunk);
  try { return JSON.parse(Buffer.concat(chunks).toString("utf8") || "{}"); } catch { return null; }
}

const server = createServer(async (req, res) => {
  const origin = req.headers.origin;
  if (origin) {
    res.setHeader("Access-Control-Allow-Origin", origin);
    res.setHeader("Access-Control-Allow-Credentials", "true");
    res.setHeader("Vary", "Origin");
  }
  if (req.method === "OPTIONS") {
    res.setHeader("Access-Control-Allow-Headers", "Authorization, Content-Type");
    res.setHeader("Access-Control-Allow-Methods", "GET, POST, PUT, PATCH, DELETE");
    res.writeHead(204);
    return res.end();
  }

  const url = new URL(req.url ?? "/", `http://${req.headers.host}`);
  const path = url.pathname;

  if (req.method === "GET" && path === "/health") return send(res, 200, { status: "healthy", service: "mock" });
  if (req.method === "GET" && path === "/api/catalog/categories") return ok(res, 200, categories);
  if (req.method === "GET" && path === "/api/catalog/products") {
    const includeSoldOut = url.searchParams.get("includeSoldOut") === "true";
    return ok(res, 200, products.filter((p) => includeSoldOut || p.isAvailable).map(publicProduct));
  }
  if (req.method === "GET" && path === "/api/orders/tables") return ok(res, 200, tables);

  if (req.method === "POST" && path === "/api/orders/cart") {
    const body = await readBody(req);
    if (!body) return fail(res, 400, "Invalid request.");
    const { errors, lines } = priceItems(body.items);
    return errors.length ? fail(res, 400, "The cart has problems.", [...new Set(errors)]) : ok(res, 200, cartFrom(lines));
  }

  if (req.method === "POST" && path === "/api/orders/checkout") {
    const body = await readBody(req);
    if (!body) return fail(res, 400, "Invalid request.");
    const table = tables.find((t) => t.tableNumber === body.tableNumber);
    if (!table) return fail(res, 404, "Table not found.");
    const { errors, lines } = priceItems(body.items);
    if (errors.length) return fail(res, 400, "The cart has problems.", [...new Set(errors)]);
    const cart = cartFrom(lines);
    for (const l of lines) {
      const p = products.find((x) => x.id === l.productId);
      if (p && p.stock !== null) p.stock -= l.quantity;
    }
    const orderId = randomUUID();
    const now = new Date().toISOString();
    const dto = {
      id: orderId, tableNumber: table.tableNumber, status: "PENDING", notes: body.notes?.trim() || null,
      subtotal: cart.subtotal, tax: 0, discount: 0, total: cart.total, totalKhr: cart.totalKhr, createdAt: now, updatedAt: now, completedAt: null,
      items: lines.map((l) => ({ id: randomUUID(), productId: l.productId, productName: l.productName, quantity: l.quantity, unitPrice: l.unitPrice, unitPriceKhr: l.unitPriceKhr, modifiers: l.modifiers, notes: l.notes, totalPrice: l.lineTotal, totalPriceKhr: l.lineTotalKhr })),
    };
    orders.set(orderId, { dto, createdAtMs: Date.now() });
    return ok(res, 201, dto);
  }

  const orderMatch = path.match(/^\/api\/orders\/([0-9a-f-]{36})$/i);
  if (req.method === "GET" && orderMatch) {
    const order = orders.get(orderMatch[1].toLowerCase());
    return order ? ok(res, 200, orderView(order)) : fail(res, 404, "Order not found.");
  }

  return fail(res, 404, "Not found.");
});

server.listen(PORT, () => console.log(`Mock POS API listening on http://localhost:${PORT} (order step ${STEP_MS} ms, ${RATE} KHR per USD)`));
