import { describe, expect, it } from "vitest";
import { createTranslator, dictionaries, isLocale, LOCALES } from "@/lib/i18n/dictionaries";

const placeholders = (text: string) => [...text.matchAll(/\{(\w+)\}/g)].map((m) => m[1]).sort();

describe("dictionaries", () => {
  const english = dictionaries.en;
  const khmer = dictionaries.km;

  it("have the same keys in every language", () => {
    expect(Object.keys(khmer).sort()).toEqual(Object.keys(english).sort());
  });

  it("have no empty text", () => {
    for (const locale of LOCALES) {
      for (const [key, text] of Object.entries(dictionaries[locale])) {
        expect(text.trim(), `${locale}:${key}`).not.toBe("");
      }
    }
  });

  it("use the same placeholders in every language", () => {
    for (const key of Object.keys(english) as Array<keyof typeof english>) {
      expect(placeholders(khmer[key]), key).toEqual(placeholders(english[key]));
    }
  });

  it("contain real Khmer script in the Khmer dictionary", () => {
    expect(khmer["cart.placeOrder"]).toMatch(/[\u1780-\u17FF]/);
    expect(khmer["order.status.COOKING"]).toMatch(/[\u1780-\u17FF]/);
  });
});

describe("createTranslator", () => {
  it("fills placeholders", () => {
    expect(createTranslator("en")("app.table", { number: 12 })).toBe("Table 12");
    expect(createTranslator("km")("app.table", { number: 12 })).toContain("12");
  });

  it("leaves unknown placeholders visible instead of failing", () => {
    expect(createTranslator("en")("app.table", {})).toBe("Table {number}");
  });
});

describe("isLocale", () => {
  it("accepts only supported languages", () => {
    expect(isLocale("km")).toBe(true);
    expect(isLocale("en")).toBe(true);
    expect(isLocale("fr")).toBe(false);
    expect(isLocale(undefined)).toBe(false);
  });
});
