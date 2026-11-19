export const restaurant = {
  name: process.env.NEXT_PUBLIC_RESTAURANT_NAME?.trim() || "My Restaurant",
  logoUrl: process.env.NEXT_PUBLIC_LOGO_URL?.trim() || null,
};

const DEFAULT_API_URL = "http://localhost:5000";

/** The server renders pages with API_URL (can be an internal host); the browser uses NEXT_PUBLIC_API_URL. */
export function apiBaseUrl(): string {
  const url =
    typeof window === "undefined"
      ? process.env.API_URL || process.env.NEXT_PUBLIC_API_URL
      : process.env.NEXT_PUBLIC_API_URL;

  return (url && url.length > 0 ? url : DEFAULT_API_URL).replace(/\/+$/, "");
}

export const MAX_TABLE_NUMBER = 10_000;
