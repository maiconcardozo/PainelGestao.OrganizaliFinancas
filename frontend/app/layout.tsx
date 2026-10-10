import type { Metadata } from "next";
import { Barlow_Condensed, Source_Sans_3 } from "next/font/google";
import "./globals.css";

const bodyFont = Source_Sans_3({ subsets: ["latin"], variable: "--font-body", display: "swap" });
const displayFont = Barlow_Condensed({ subsets: ["latin"], weight: ["600", "700"], variable: "--font-display", display: "swap" });

export const metadata: Metadata = {
  title: "Organizali Finanças",
  description: "Gestão financeira da casa",
  applicationName: "Organizali Finanças",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return <html lang="pt-BR" className={`${bodyFont.variable} ${displayFont.variable}`}><body>{children}</body></html>;
}
