// Amounts come from the server in USD and riel. The browser never converts between them.
// Western digits are used for both currencies, as on most Cambodian price lists.

const usd = new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" });
const khr = new Intl.NumberFormat("en-US", { maximumFractionDigits: 0 });

export function formatUsd(value: number): string {
  return usd.format(value);
}

export function formatKhr(value: number): string {
  return `${khr.format(value)}៛`;
}

export function roundUsd(value: number): number {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}
