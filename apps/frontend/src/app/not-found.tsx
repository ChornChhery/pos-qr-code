import Link from "next/link";
import { Header } from "@/components/Header";

export default function NotFound() {
  return (
    <>
      <Header />
      <main className="mx-auto max-w-sm px-6 py-14 text-center">
        <p className="text-2xl font-bold">404</p>
        <Link href="/" className="mt-4 inline-flex min-h-11 items-center font-semibold text-brand-700 underline">
          Menu
        </Link>
      </main>
    </>
  );
}
