import { cn } from "@/lib/cn";
import { formatKhr, formatUsd } from "@/lib/money";

/** Dollars first, riel next to it. Both values come from the server. */
export function Price({
  usd,
  khr,
  prefix,
  className,
  stacked = false,
}: {
  usd: number;
  khr: number;
  prefix?: string;
  className?: string;
  stacked?: boolean;
}) {
  return (
    <span className={cn("tabular-nums", stacked ? "flex flex-col items-end leading-tight" : "inline-flex items-baseline gap-2", className)}>
      <span className="font-bold">
        {prefix}
        {formatUsd(usd)}
      </span>
      <span className="text-sm text-muted">
        {prefix}
        {formatKhr(khr)}
      </span>
    </span>
  );
}
