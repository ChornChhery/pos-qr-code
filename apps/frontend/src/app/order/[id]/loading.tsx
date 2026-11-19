import { Header } from "@/components/Header";
import { OrderSkeleton } from "@/components/OrderSkeleton";

export default function Loading() {
  return (
    <>
      <Header />
      <main className="mx-auto max-w-xl px-4 py-6">
        <OrderSkeleton />
      </main>
    </>
  );
}
