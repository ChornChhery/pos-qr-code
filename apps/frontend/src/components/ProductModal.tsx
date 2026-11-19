"use client";

import { Minus, Plus } from "lucide-react";
import { useMemo, useState } from "react";
import { Price } from "@/components/Price";
import { Button } from "@/components/ui/button";
import { Sheet } from "@/components/ui/sheet";
import { cn } from "@/lib/cn";
import { useT } from "@/lib/i18n/LocaleProvider";
import type { TranslateFn } from "@/lib/i18n/dictionaries";
import { formatKhr, formatUsd, roundUsd } from "@/lib/money";
import {
  estimateUnitPrice,
  isSingleChoice,
  requiredMinimum,
  selectedOptionLabels,
  validateSelection,
} from "@/lib/selection";
import type { ModifierDto, ProductDto } from "@/lib/types";
import { MAX_LINE_QUANTITY, MAX_NOTE_LENGTH, useCart } from "@/store/cart";

export function ruleHint(modifier: ModifierDto, t: TranslateFn): string | null {
  const min = requiredMinimum(modifier);
  const max = modifier.maxSelections;

  if (max === 1) {
    return t("product.chooseOne");
  }
  if (min > 0 && max > 0) {
    return min === max ? t("product.chooseExactly", { count: min }) : t("product.chooseBetween", { min, max });
  }
  if (min > 0) {
    return t("product.chooseAtLeast", { min });
  }
  if (max > 0) {
    return t("product.chooseUpTo", { max });
  }
  return null;
}

export function ProductModal({ product, onClose }: { product: ProductDto; onClose: () => void }) {
  const t = useT();
  const addLine = useCart((state) => state.addLine);
  const [selected, setSelected] = useState<ReadonlySet<string>>(new Set());
  const [quantity, setQuantity] = useState(1);
  const [notes, setNotes] = useState("");

  const issues = useMemo(() => validateSelection(product.modifiers, selected), [product.modifiers, selected]);
  const unit = useMemo(() => estimateUnitPrice(product, selected), [product, selected]);
  const canAdd = issues.length === 0 && product.isAvailable;

  function toggle(modifier: ModifierDto, optionId: string) {
    setSelected((current) => {
      const next = new Set(current);
      const groupIds = modifier.options.map((option) => option.id);

      if (isSingleChoice(modifier)) {
        groupIds.forEach((id) => next.delete(id));
        next.add(optionId);
        return next;
      }

      if (next.has(optionId)) {
        next.delete(optionId);
      } else if (modifier.maxSelections === 0 || groupIds.filter((id) => next.has(id)).length < modifier.maxSelections) {
        next.add(optionId);
      }
      return next;
    });
  }

  function add() {
    if (!canAdd) {
      return;
    }
    addLine({
      productId: product.id,
      name: product.name,
      quantity,
      optionIds: [...selected],
      optionLabels: selectedOptionLabels(product, selected),
      notes,
      unitUsd: unit.usd,
      unitKhr: unit.khr,
    });
    onClose();
  }

  return (
    <Sheet
      open
      onOpenChange={(open) => {
        if (!open) {
          onClose();
        }
      }}
      title={product.name}
      footer={
        <div className="flex items-center gap-3">
          <div role="group" aria-label={t("product.quantity")} className="flex items-center rounded-lg border border-line">
            <Button
              variant="ghost"
              size="icon"
              aria-label={t("product.decrease")}
              disabled={quantity <= 1}
              onClick={() => setQuantity((q) => Math.max(1, q - 1))}
            >
              <Minus aria-hidden="true" className="size-5" />
            </Button>
            <span aria-live="polite" className="min-w-8 text-center text-lg font-bold tabular-nums">
              {quantity}
            </span>
            <Button
              variant="ghost"
              size="icon"
              aria-label={t("product.increase")}
              disabled={quantity >= MAX_LINE_QUANTITY}
              onClick={() => setQuantity((q) => Math.min(MAX_LINE_QUANTITY, q + 1))}
            >
              <Plus aria-hidden="true" className="size-5" />
            </Button>
          </div>
          <Button size="lg" className="flex-1" disabled={!canAdd} onClick={add}>
            <span>{t("product.add")}</span>
            <span className="tabular-nums">{formatUsd(roundUsd(unit.usd * quantity))}</span>
          </Button>
        </div>
      }
    >
      {product.description ? <p className="mb-4 text-muted">{product.description}</p> : null}
      <p className="mb-4">
        <Price usd={product.price} khr={product.priceKhr} />
      </p>

      {product.modifiers.map((modifier) => {
        const single = isSingleChoice(modifier);
        const hint = ruleHint(modifier, t);
        const missing = issues.some((issue) => issue.modifierId === modifier.id);

        return (
          <fieldset key={modifier.id} className="mb-5 min-w-0">
            <legend className="mb-1 flex w-full flex-wrap items-center gap-2 text-lg font-bold">
              {modifier.groupName}
              <span
                className={cn(
                  "rounded px-2 py-0.5 text-sm font-semibold",
                  modifier.isRequired ? "bg-brand-100 text-brand-700" : "bg-line text-muted",
                )}
              >
                {modifier.isRequired ? t("product.required") : t("product.optional")}
              </span>
            </legend>
            {hint ? (
              <p className={cn("mb-1 text-sm", missing && modifier.isRequired ? "font-semibold text-danger-text" : "text-muted")}>
                {hint}
              </p>
            ) : null}
            <div className="divide-y divide-line">
              {modifier.options.map((option) => {
                const checked = selected.has(option.id);
                const groupFull =
                  !single &&
                  modifier.maxSelections > 0 &&
                  !checked &&
                  modifier.options.filter((o) => selected.has(o.id)).length >= modifier.maxSelections;
                const disabled = !option.isAvailable || groupFull;

                return (
                  <label
                    key={option.id}
                    className={cn("flex min-h-11 cursor-pointer items-center gap-3 py-2", disabled && "cursor-not-allowed opacity-50")}
                  >
                    <input
                      type={single ? "radio" : "checkbox"}
                      name={modifier.id}
                      checked={checked}
                      disabled={disabled}
                      onChange={() => toggle(modifier, option.id)}
                      className="size-5 shrink-0 accent-brand-600"
                    />
                    <span className="flex-1">{option.name}</span>
                    {!option.isAvailable ? (
                      <span className="text-sm text-muted">{t("menu.soldOut")}</span>
                    ) : option.priceAdd > 0 ? (
                      <span className="text-sm text-muted tabular-nums">
                        +{formatUsd(option.priceAdd)} · +{formatKhr(option.priceAddKhr)}
                      </span>
                    ) : null}
                  </label>
                );
              })}
            </div>
          </fieldset>
        );
      })}

      <label className="block">
        <span className="mb-1 block text-lg font-bold">{t("product.notes")}</span>
        <textarea
          value={notes}
          maxLength={MAX_NOTE_LENGTH}
          rows={2}
          placeholder={t("product.notesPlaceholder")}
          onChange={(event) => setNotes(event.target.value)}
          className="w-full rounded-lg border border-line p-3 text-base"
        />
      </label>
    </Sheet>
  );
}
