import type { Metadata, Viewport } from "next";
import "@fontsource-variable/noto-sans-khmer";
import "./globals.css";
import { Providers } from "@/components/Providers";
import { restaurant } from "@/lib/config";
import { getLocale } from "@/lib/i18n/server";

export const metadata: Metadata = {
  title: { default: restaurant.name, template: `%s · ${restaurant.name}` },
  description: `${restaurant.name} menu`,
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  themeColor: "#c2410c",
};

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const locale = await getLocale();

  return (
    <html lang={locale}>
      <body className="min-h-dvh antialiased">
        <Providers locale={locale}>{children}</Providers>
      </body>
    </html>
  );
}
