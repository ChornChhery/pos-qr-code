import { LOCALE_COOKIE_NAME, type Locale } from "@/lib/i18n/dictionaries";

const ONE_YEAR_SECONDS = 60 * 60 * 24 * 365;

/** Remembers the chosen language. The server reads this cookie when it renders the next page. */
export function saveLocale(locale: Locale): void {
  document.cookie = `${LOCALE_COOKIE_NAME}=${locale}; path=/; max-age=${ONE_YEAR_SECONDS}; samesite=lax`;
}
