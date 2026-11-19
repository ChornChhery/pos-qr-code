import { describe, expect, it } from "vitest";
import { estimateUnitPrice, isSingleChoice, requiredMinimum, selectedOptionLabels, validateSelection } from "@/lib/selection";
import { extrasGroup, latte, sizeGroup } from "@/test-utils";

const ids = (...values: string[]) => new Set(values);

describe("validateSelection", () => {
  it("asks for a required group that has no choice", () => {
    const issues = validateSelection(latte.modifiers, ids());

    expect(issues).toHaveLength(1);
    expect(issues[0]).toMatchObject({ modifierId: "mod-size", kind: "min", min: 1 });
  });

  it("accepts one size and no extras", () => {
    expect(validateSelection(latte.modifiers, ids("opt-regular"))).toEqual([]);
  });

  it("rejects two sizes", () => {
    const issues = validateSelection(latte.modifiers, ids("opt-regular", "opt-large"));

    expect(issues).toHaveLength(1);
    expect(issues[0]).toMatchObject({ modifierId: "mod-size", kind: "max" });
  });

  it("rejects more extras than the group allows", () => {
    const issues = validateSelection(latte.modifiers, ids("opt-regular", "opt-shot", "opt-oat", "opt-syrup"));

    expect(issues).toHaveLength(1);
    expect(issues[0]).toMatchObject({ modifierId: "mod-extras", kind: "max", max: 2 });
  });

  it("treats a required group with MinSelections 0 as needing one choice", () => {
    expect(requiredMinimum({ ...sizeGroup, minSelections: 0 })).toBe(1);
  });

  it("treats MaxSelections 0 as no limit", () => {
    const unlimited = { ...extrasGroup, maxSelections: 0 };

    expect(validateSelection([unlimited], ids("opt-shot", "opt-oat", "opt-syrup"))).toEqual([]);
  });

  it("knows which groups are single choice", () => {
    expect(isSingleChoice(sizeGroup)).toBe(true);
    expect(isSingleChoice(extrasGroup)).toBe(false);
  });
});

describe("estimateUnitPrice", () => {
  it("adds the chosen options to the product price in both currencies", () => {
    const price = estimateUnitPrice(latte, ids("opt-large", "opt-shot"));

    expect(price.usd).toBe(3.5);
    expect(price.khr).toBe(14000);
  });

  it("is the product price when nothing is chosen", () => {
    expect(estimateUnitPrice(latte, ids())).toEqual({ usd: 2.5, khr: 10000 });
  });

  it("does not drift on cents", () => {
    expect(estimateUnitPrice(latte, ids("opt-regular", "opt-shot", "opt-oat")).usd).toBe(3.6);
  });
});

describe("selectedOptionLabels", () => {
  it("lists chosen option names in menu order", () => {
    expect(selectedOptionLabels(latte, ids("opt-oat", "opt-large"))).toEqual(["Large", "Oat milk"]);
  });
});
