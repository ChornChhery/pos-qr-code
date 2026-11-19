import { MAX_TABLE_NUMBER } from "@/lib/config";

/** Table numbers come from the QR code link, so anything that is not a plain positive whole number is ignored. */
export function parseTable(value: string | string[] | undefined): number | null {
  const text = Array.isArray(value) ? value[0] : value;
  if (!text || !/^\d{1,5}$/.test(text)) {
    return null;
  }
  const number = Number(text);
  return number >= 1 && number <= MAX_TABLE_NUMBER ? number : null;
}
