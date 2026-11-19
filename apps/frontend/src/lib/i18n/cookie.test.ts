import { describe, expect, it } from "vitest";
import { saveLocale } from "@/lib/i18n/cookie";

describe("saveLocale", () => {
  it("stores the language in the cookie the server reads", () => {
    saveLocale("km");

    expect(document.cookie).toContain("lang=km");
  });
});
