"use client";

import { AlertTriangle, ShoppingBag } from "lucide-react";
import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { useT } from "@/lib/i18n/LocaleProvider";

export function ErrorState({ title, hint, onRetry }: { title: string; hint?: string; onRetry?: () => void }) {
  const t = useT();

  return (
    <div role="alert" className="mx-auto flex max-w-sm flex-col items-center gap-3 px-6 py-14 text-center">
      <AlertTriangle aria-hidden="true" className="size-10 text-warning" />
      <p className="text-lg font-bold">{title}</p>
      {hint ? <p className="text-muted">{hint}</p> : null}
      {onRetry ? <Button onClick={onRetry}>{t("app.retry")}</Button> : null}
    </div>
  );
}

export function EmptyState({ title, hint, action }: { title: string; hint?: string; action?: ReactNode }) {
  return (
    <div className="mx-auto flex max-w-sm flex-col items-center gap-3 px-6 py-14 text-center">
      <ShoppingBag aria-hidden="true" className="size-10 text-brand-600" />
      <p className="text-lg font-bold">{title}</p>
      {hint ? <p className="text-muted">{hint}</p> : null}
      {action}
    </div>
  );
}
