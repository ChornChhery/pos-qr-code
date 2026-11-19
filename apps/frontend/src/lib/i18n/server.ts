import { cookies, headers } from "next/headers";
import { DEFAULT_LOCALE, isLocale, LOCALE_COOKIE_NAME, type Locale } from "@/lib/i18n/dictionaries";

/** Chosen language (cookie), else the browser's language, else English. */
export async function getLocale(): Promise<Locale> {
  const saved = (await cookies()).get(LOCALE_COOKIE_NAME)?.value;
  if (isLocale(saved)) {
    return saved;
  }

  const accept = (await headers()).get("accept-language") ?? "";
  return /(^|,)\s*km\b/i.test(accept) ? "km" : DEFAULT_LOCALE;
}
