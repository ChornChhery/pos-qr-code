"use client";

import { useRouter } from "next/navigation";
import { cn } from "@/lib/cn";
import { saveLocale } from "@/lib/i18n/cookie";
import { useLocale } from "@/lib/i18n/LocaleProvider";
import type { Locale } from "@/lib/i18n/dictionaries";

const OPTIONS: ReadonlyArray<{ code: Locale; label: string; name: string }> = [
  { code: "en", label: "EN", name: "English" },
  { code: "km", label: "ខ្មែរ", name: "ភាសាខ្មែរ" },
];

export function LanguageToggle() {
  const { locale, t } = useLocale();
  const router = useRouter();

  function choose(next: Locale) {
    if (next === locale) {
      return;
    }
    saveLocale(next);
    router.refresh();
  }

  return (
    <div role="group" aria-label={t("app.language")} className="flex rounded-full border border-line bg-white p-0.5">
      {OPTIONS.map((option) => (
        <button
          key={option.code}
          type="button"
          lang={option.code}
          aria-label={option.name}
          aria-pressed={locale === option.code}
          onClick={() => choose(option.code)}
          className={cn(
            "min-h-11 min-w-11 rounded-full px-3 text-sm font-semibold",
            locale === option.code ? "bg-brand-600 text-white" : "text-muted hover:bg-surface",
          )}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
