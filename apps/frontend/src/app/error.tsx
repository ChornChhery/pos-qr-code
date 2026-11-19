"use client";

import { Header } from "@/components/Header";
import { ErrorState } from "@/components/States";
import { useT } from "@/lib/i18n/LocaleProvider";

export default function ErrorPage({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  const t = useT();

  return (
    <>
      <Header />
      <ErrorState title={t("error.title")} hint={t("error.hint")} onRetry={reset} />
    </>
  );
}
