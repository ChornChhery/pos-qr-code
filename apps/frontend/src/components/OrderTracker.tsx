"use client";

import { useQuery } from "@tanstack/react-query";
import Link from "next/link";
import { ErrorState } from "@/components/States";
import { Price } from "@/components/Price";
import { StatusStepper } from "@/components/StatusStepper";
import { OrderSkeleton } from "@/components/OrderSkeleton";
import { ApiError, getOrder } from "@/lib/api";
import { useT } from "@/lib/i18n/LocaleProvider";

const POLL_MS = 5_000;

/** Shows an order's progress. Guests have no account, so the page asks the server every few seconds. */
export function OrderTracker({ id }: { id: string }) {
  const t = useT();

  const order = useQuery({
    queryKey: ["order", id],
    queryFn: () => getOrder(id),
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status === "COMPLETED" || status === "CANCELLED" ? false : POLL_MS;
    },
    retry: (failureCount, error) => !(error instanceof ApiError && error.status === 404) && failureCount < 2,
  });

  const backLink = (
    <Link href="/" className="inline-flex min-h-11 items-center font-semibold text-brand-700 underline">
      {t("order.backToMenu")}
    </Link>
  );

  if (order.isPending) {
    return <OrderSkeleton />;
  }

  if (order.isError && !order.data) {
    const notFound = order.error instanceof ApiError && order.error.status === 404;
    return (
      <div>
        <ErrorState
          title={notFound ? t("order.notFound") : t("order.loadError")}
          onRetry={notFound ? undefined : () => void order.refetch()}
        />
        <div className="text-center">{backLink}</div>
      </div>
    );
  }

  const data = order.data;
  if (!data) {
    return null;
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold">{t("order.title")}</h1>
        <p className="text-muted">{t("order.table", { number: data.tableNumber })}</p>
      </div>

      {data.status === "CANCELLED" ? (
        <div role="status" className="rounded-lg border border-danger/40 bg-red-50 p-4 text-danger-text">
          <p className="text-lg font-bold">{t("order.status.CANCELLED")}</p>
          <p>{t("order.hint.CANCELLED")}</p>
        </div>
      ) : (
        <>
          <StatusStepper status={data.status} />
          <p role="status" aria-live="polite" className="text-center text-lg font-semibold">
            {t(`order.hint.${data.status}`)}
          </p>
        </>
      )}

      <section aria-labelledby="order-items">
        <h2 id="order-items" className="mb-2 text-lg font-bold">
          {t("order.items")}
        </h2>
        <ul className="divide-y divide-line rounded-lg border border-line">
          {data.items.map((item) => (
            <li key={item.id} className="flex items-start justify-between gap-3 p-3">
              <div className="min-w-0">
                <p className="font-semibold">
                  {item.quantity} × {item.productName}
                </p>
                {item.modifiers.length > 0 ? (
                  <p className="text-sm text-muted">{item.modifiers.map((m) => m.name).join(", ")}</p>
                ) : null}
                {item.notes ? <p className="text-sm text-muted italic">{item.notes}</p> : null}
              </div>
              <Price usd={item.totalPrice} khr={item.totalPriceKhr} stacked className="shrink-0" />
            </li>
          ))}
        </ul>
        <div className="mt-3 flex items-end justify-between gap-3">
          <span className="text-lg font-bold">{t("order.total")}</span>
          <Price usd={data.total} khr={data.totalKhr} stacked className="text-xl" />
        </div>
      </section>

      <p className="text-sm text-muted">{t("order.autoRefresh")}</p>
      {order.isError ? <p role="alert" className="text-sm text-danger-text">{t("order.loadError")}</p> : null}
      {backLink}
    </div>
  );
}
