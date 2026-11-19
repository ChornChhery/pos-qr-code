import { notFound } from "next/navigation";
import { Header } from "@/components/Header";
import { OrderTracker } from "@/components/OrderTracker";

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export default async function OrderPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  if (!UUID.test(id)) {
    notFound();
  }

  return (
    <>
      <Header />
      <main className="mx-auto max-w-xl px-4 py-6">
        <OrderTracker id={id} />
      </main>
    </>
  );
}
