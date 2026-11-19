"use client";

import { useT } from "@/lib/i18n/LocaleProvider";
import type { CategoryDto } from "@/lib/types";

export function sectionId(categoryId: string): string {
  return `category-${categoryId}`;
}

/** Jump links to each menu section. They are plain anchors, so they work without JavaScript. */
export function CategoryTabs({ categories }: { categories: CategoryDto[] }) {
  const t = useT();

  return (
    <nav
      aria-label={t("menu.categories")}
      className="sticky top-14 z-20 border-b border-line bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/80"
    >
      <ul className="mx-auto flex max-w-3xl gap-2 overflow-x-auto px-4 py-2">
        {categories.map((category) => (
          <li key={category.id} className="shrink-0">
            <a
              href={`#${sectionId(category.id)}`}
              className="inline-flex min-h-11 items-center rounded-full border border-line px-4 font-semibold hover:bg-brand-50"
            >
              {category.name}
            </a>
          </li>
        ))}
      </ul>
    </nav>
  );
}
