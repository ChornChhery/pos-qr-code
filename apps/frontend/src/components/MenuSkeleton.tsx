import { Skeleton } from "@/components/ui/skeleton";

/** Placeholder rows that match the real menu layout, so nothing jumps when data arrives. */
export function MenuSkeleton() {
  return (
    <div role="status" aria-busy="true" className="mx-auto max-w-3xl px-4 py-4">
      <span className="sr-only">Loading…</span>
      <div className="mb-6 flex gap-2 overflow-hidden">
        {[0, 1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-11 w-24 shrink-0 rounded-full" />
        ))}
      </div>
      {[0, 1].map((group) => (
        <div key={group} className="mb-8">
          <Skeleton className="mb-3 h-7 w-40" />
          {[0, 1, 2].map((row) => (
            <div key={row} className="flex items-center justify-between gap-4 border-b border-line py-4">
              <div className="flex-1 space-y-2">
                <Skeleton className="h-5 w-3/5" />
                <Skeleton className="h-4 w-4/5" />
              </div>
              <Skeleton className="h-10 w-20" />
            </div>
          ))}
        </div>
      ))}
    </div>
  );
}
