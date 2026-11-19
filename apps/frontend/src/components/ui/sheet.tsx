"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { X } from "lucide-react";
import type { ReactNode } from "react";
import { cn } from "@/lib/cn";
import { useT } from "@/lib/i18n/LocaleProvider";

interface SheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  children: ReactNode;
  footer?: ReactNode;
  className?: string;
}

/** Bottom sheet. Radix handles the focus trap, Escape key, and screen-reader roles. */
export function Sheet({ open, onOpenChange, title, children, footer, className }: SheetProps) {
  const t = useT();

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-40 bg-black/40" />
        <Dialog.Content
          aria-describedby={undefined}
          className={cn(
            "fixed inset-x-0 bottom-0 z-50 mx-auto flex max-h-[92dvh] w-full max-w-xl flex-col rounded-t-2xl bg-white shadow-xl",
            className,
          )}
        >
          <div className="flex items-start justify-between gap-3 border-b border-line px-4 py-3">
            <Dialog.Title className="text-xl font-bold">{title}</Dialog.Title>
            <Dialog.Close
              aria-label={t("app.close")}
              className="inline-flex size-11 shrink-0 items-center justify-center rounded-lg text-muted hover:bg-surface"
            >
              <X aria-hidden="true" className="size-6" />
            </Dialog.Close>
          </div>
          <div className="overflow-y-auto px-4 py-4">{children}</div>
          {footer ? (
            <div className="border-t border-line px-4 pt-3 pb-[max(0.75rem,env(safe-area-inset-bottom))]">{footer}</div>
          ) : null}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
