import { Header } from "@/components/Header";
import { MenuView } from "@/components/MenuView";
import { getMenu } from "@/lib/api";
import { parseTable } from "@/lib/table";
import type { MenuData } from "@/lib/types";

/** Best effort. If the API is down the page still renders and the browser tries again. */
async function loadMenu(): Promise<MenuData | null> {
  try {
    return await getMenu({ next: { revalidate: 30 } });
  } catch {
    return null;
  }
}

export default async function MenuPage({ searchParams }: { searchParams: Promise<{ table?: string | string[] }> }) {
  const [{ table }, menu] = await Promise.all([searchParams, loadMenu()]);

  return (
    <>
      <Header />
      <main id="menu">
        <MenuView initialMenu={menu} initialTable={parseTable(table)} />
      </main>
    </>
  );
}
