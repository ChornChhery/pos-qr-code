"use client";

import { Check } from "lucide-react";
import { cn } from "@/lib/cn";
import { useT } from "@/lib/i18n/LocaleProvider";
import type { OrderStatus } from "@/lib/types";

export const STEPS: readonly OrderStatus[] = ["PENDING", "COOKING", "READY", "COMPLETED"];

export function StatusStepper({ status }: { status: OrderStatus }) {
  const t = useT();
  const current = STEPS.indexOf(status);

  return (
    <ol className="flex items-start justify-between gap-1">
      {STEPS.map((step, index) => {
        const done = index < current || status === "COMPLETED";
        const active = index === current && status !== "COMPLETED";

        return (
          <li key={step} aria-current={active ? "step" : undefined} className="flex min-w-0 flex-1 flex-col items-center gap-1 text-center">
            <span
              className={cn(
                "flex size-9 items-center justify-center rounded-full border-2 text-sm font-bold",
                done && "border-success bg-success text-white",
                active && "border-brand-600 bg-brand-600 text-white",
                !done && !active && "border-line text-muted",
              )}
            >
              {done ? <Check aria-hidden="true" className="size-5" /> : index + 1}
            </span>
            <span className={cn("text-sm", active ? "font-bold" : "text-muted")}>{t(`order.status.${step}`)}</span>
          </li>
        );
      })}
    </ol>
  );
}
