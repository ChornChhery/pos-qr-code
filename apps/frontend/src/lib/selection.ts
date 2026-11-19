import { roundUsd } from "@/lib/money";
import type { ModifierDto, ProductDto } from "@/lib/types";

export interface SelectionIssue {
  modifierId: string;
  groupName: string;
  kind: "min" | "max";
  min: number;
  max: number;
}

/** A required group needs at least one choice, even if MinSelections says 0. */
export function requiredMinimum(modifier: ModifierDto): number {
  return modifier.isRequired ? Math.max(modifier.minSelections, 1) : modifier.minSelections;
}

/** Exactly one choice allowed: shown as radio buttons. */
export function isSingleChoice(modifier: ModifierDto): boolean {
  return modifier.maxSelections === 1;
}

/**
 * Mirrors the server's option rules so the button can be disabled early.
 * The server checks the same rules again at checkout and always has the last word.
 */
export function validateSelection(modifiers: ModifierDto[], selected: ReadonlySet<string>): SelectionIssue[] {
  const issues: SelectionIssue[] = [];

  for (const modifier of modifiers) {
    const count = modifier.options.filter((option) => selected.has(option.id)).length;
    const min = requiredMinimum(modifier);

    if (count < min) {
      issues.push({ modifierId: modifier.id, groupName: modifier.groupName, kind: "min", min, max: modifier.maxSelections });
    } else if (modifier.maxSelections > 0 && count > modifier.maxSelections) {
      issues.push({ modifierId: modifier.id, groupName: modifier.groupName, kind: "max", min, max: modifier.maxSelections });
    }
  }

  return issues;
}

export interface UnitPrice {
  usd: number;
  khr: number;
}

/** Display-only estimate: product price plus the chosen options. The server recalculates at checkout. */
export function estimateUnitPrice(product: ProductDto, selected: ReadonlySet<string>): UnitPrice {
  let usd = product.price;
  let khr = product.priceKhr;

  for (const modifier of product.modifiers) {
    for (const option of modifier.options) {
      if (selected.has(option.id)) {
        usd += option.priceAdd;
        khr += option.priceAddKhr;
      }
    }
  }

  return { usd: roundUsd(usd), khr };
}

export function selectedOptionLabels(product: ProductDto, selected: ReadonlySet<string>): string[] {
  return product.modifiers.flatMap((modifier) =>
    modifier.options.filter((option) => selected.has(option.id)).map((option) => option.name),
  );
}
