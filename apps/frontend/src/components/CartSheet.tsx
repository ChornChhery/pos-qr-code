"use client";

import { useMutation, useQuery } from "@tanstack/react-query";
import { Minus, Plus, Trash2 } from "lucide-react";
import { useRouter } from "next/navigation";
import { useMemo, useRef, useState } from "react";
import { EmptyState } from "@/components/States";
import { Price } from "@/components/Price";
import { Button } from "@/components/ui/button";
import { Sheet } from "@/components/ui/sheet";
import { Skeleton } from "@/components/ui/skeleton";
import { describeCheckoutError } from "@/hooks/useCheckoutErrorMessage";
import { ApiError, checkout, getTables, priceCart } from "@/lib/api";
import { useT } from "@/lib/i18n/LocaleProvider";
import { MAX_LINE_QUANTITY, estimateTotals, toRequestItems, useCart } from "@/store/cart";

const MAX_ORDER_NOTE = 300;

export function CartSheet({ open, onOpenChange }: { open: boolean; onOpenChange: (open: boolean) => void }) {
  const t = useT();
  const router = useRouter();
  const lines = useCart((state) => state.lines);
  const tableNumber = useCart((state) => state.tableNumber);
  const setTable = useCart((state) => state.setTable);
  const setQuantity = useCart((state) => state.setQuantity);
  const removeLine = useCart((state) => state.removeLine);
  const clearCart = useCart((state) => state.clearCart);
  const setLastOrder = useCart((state) => state.setLastOrder);

  const [notes, setNotes] = useState("");
  const submitting = useRef(false);

  const items = useMemo(() => toRequestItems(lines), [lines]);
  const hasLines = lines.length > 0;

  const pricing = useQuery({
    queryKey: ["cart-price", items],
    queryFn: () => priceCart(items),
    enabled: open && hasLines,
    staleTime: 0,
    retry: false,
  });

  const tables = useQuery({
    queryKey: ["tables"],
    queryFn: getTables,
    enabled: open && hasLines && tableNumber === null,
  });

  const order = useMutation({
    mutationFn: () =>
      checkout({
        tableNumber: tableNumber ?? 0,
        items,
        ...(notes.trim() ? { notes: notes.trim() } : {}),
      }),
    onSuccess: (created) => {
      clearCart();
      setLastOrder(created.id);
      setNotes("");
      onOpenChange(false);
      router.push(`/order/${created.id}`);
    },
    onSettled: () => {
      submitting.current = false;
    },
  });

  const problem = order.isError ? describeCheckoutError(order.error, t) : null;
  const pricingProblem =
    pricing.error instanceof ApiError && pricing.error.status === 400 ? pricing.error.errors : [];
  const estimate = estimateTotals(lines);

  const canOrder =
    hasLines && tableNumber !== null && pricing.isSuccess && !order.isPending;

  function placeOrder() {
    // A second tap while the first request is running must not create a second order.
    if (!canOrder || submitting.current) {
      return;
    }
    submitting.current = true;
    order.mutate();
  }

  return (
    <Sheet
      open={open}
      onOpenChange={onOpenChange}
      title={t("cart.title")}
      footer={
        hasLines ? (
          <div className="space-y-3">
            <div className="flex items-end justify-between gap-3">
              <span className="text-lg font-bold">{t("cart.total")}</span>
              {pricing.isSuccess ? (
                <Price usd={pricing.data.total} khr={pricing.data.totalKhr} stacked className="text-xl" />
              ) : (
                <Price usd={estimate.usd} khr={estimate.khr} prefix="≈ " stacked className="text-xl" />
              )}
            </div>
            <Button size="lg" className="w-full" disabled={!canOrder} onClick={placeOrder}>
              {order.isPending ? t("cart.placing") : t("cart.placeOrder")}
            </Button>
          </div>
        ) : null
      }
    >
      {!hasLines ? (
        <EmptyState title={t("cart.empty")} hint={t("cart.emptyHint")} />
      ) : (
        <div className="space-y-5">
          <ul className="divide-y divide-line">
            {lines.map((line, index) => {
              // The server returns lines in the order they were sent.
              const candidate = pricing.data?.lines[index];
              const serverLine =
                candidate && candidate.productId === line.productId && candidate.quantity === line.quantity
                  ? candidate
                  : undefined;

              return (
                <li key={line.key} className="py-3">
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="font-semibold">{line.name}</p>
                      {line.optionLabels.length > 0 ? (
                        <p className="text-sm text-muted">{line.optionLabels.join(", ")}</p>
                      ) : null}
                      {line.notes ? <p className="text-sm text-muted italic">{line.notes}</p> : null}
                    </div>
                    <Price
                      usd={serverLine?.lineTotal ?? line.unitUsd * line.quantity}
                      khr={serverLine?.lineTotalKhr ?? line.unitKhr * line.quantity}
                      stacked
                      className="shrink-0"
                    />
                  </div>
                  <div className="mt-2 flex items-center justify-between">
                    <div className="flex items-center rounded-lg border border-line">
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label={t("product.decrease")}
                        onClick={() => setQuantity(line.key, line.quantity - 1)}
                      >
                        <Minus aria-hidden="true" className="size-5" />
                      </Button>
                      <span aria-live="polite" className="min-w-8 text-center text-lg font-bold tabular-nums">
                        {line.quantity}
                      </span>
                      <Button
                        variant="ghost"
                        size="icon"
                        aria-label={t("product.increase")}
                        disabled={line.quantity >= MAX_LINE_QUANTITY}
                        onClick={() => setQuantity(line.key, line.quantity + 1)}
                      >
                        <Plus aria-hidden="true" className="size-5" />
                      </Button>
                    </div>
                    <Button
                      variant="ghost"
                      size="icon"
                      aria-label={t("cart.remove", { name: line.name })}
                      onClick={() => removeLine(line.key)}
                    >
                      <Trash2 aria-hidden="true" className="size-5" />
                    </Button>
                  </div>
                </li>
              );
            })}
          </ul>

          {pricing.isFetching && !pricing.isSuccess ? (
            <div role="status" className="space-y-2">
              <p className="text-sm text-muted">{t("cart.pricing")}</p>
              <Skeleton className="h-5 w-full" />
            </div>
          ) : null}

          {pricing.isError ? (
            <div role="alert" className="rounded-lg border border-danger/40 bg-red-50 p-3 text-danger-text">
              <p className="font-semibold">{pricingProblem.length > 0 ? t("cart.fixItems") : t("cart.priceError")}</p>
              {pricingProblem.length > 0 ? (
                <ul className="mt-1 list-disc pl-5">
                  {pricingProblem.map((message) => (
                    <li key={message}>{message}</li>
                  ))}
                </ul>
              ) : (
                <Button variant="secondary" className="mt-2" onClick={() => void pricing.refetch()}>
                  {t("app.retry")}
                </Button>
              )}
            </div>
          ) : null}

          {pricing.isSuccess ? null : <p className="text-sm text-muted">{t("cart.estimate")}</p>}

          {tableNumber === null ? (
            <div>
              <label htmlFor="table-select" className="mb-1 block text-lg font-bold">
                {t("cart.chooseTable")}
              </label>
              {tables.isError ? (
                <p role="alert" className="text-danger-text">
                  {t("cart.tablesError")}
                </p>
              ) : (
                <select
                  id="table-select"
                  value=""
                  disabled={tables.isPending}
                  onChange={(event) => setTable(Number(event.target.value))}
                  className="min-h-11 w-full rounded-lg border border-line bg-white px-3 text-base"
                >
                  <option value="" disabled>
                    {t("cart.tableRequired")}
                  </option>
                  {(tables.data ?? []).map((table) => (
                    <option key={table.id} value={table.tableNumber}>
                      {t("app.table", { number: table.tableNumber })}
                    </option>
                  ))}
                </select>
              )}
            </div>
          ) : (
            <p className="font-semibold">
              {t("cart.table")}: {t("app.table", { number: tableNumber })}
            </p>
          )}

          <label className="block">
            <span className="mb-1 block text-lg font-bold">{t("cart.notes")}</span>
            <textarea
              value={notes}
              maxLength={MAX_ORDER_NOTE}
              rows={2}
              onChange={(event) => setNotes(event.target.value)}
              className="w-full rounded-lg border border-line p-3 text-base"
            />
          </label>

          {problem ? (
            <div role="alert" className="rounded-lg border border-danger/40 bg-red-50 p-3 text-danger-text">
              <p className="font-semibold">{problem.message}</p>
              {problem.details.length > 0 ? (
                <ul className="mt-1 list-disc pl-5">
                  {problem.details.map((message) => (
                    <li key={message}>{message}</li>
                  ))}
                </ul>
              ) : null}
            </div>
          ) : null}
        </div>
      )}
    </Sheet>
  );
}
