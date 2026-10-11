import type { Metadata, Viewport } from "next";
import { Figtree, Source_Sans_3 } from "next/font/google";
import "./globals.css";

const bodyFont = Source_Sans_3({ subsets: ["latin"], variable: "--font-body", display: "swap" });
// Figtree (Google Fonts) for titles and figures: open and round, readable with Portuguese accents. Replaces the condensed Barlow.
const displayFont = Figtree({ subsets: ["latin"], weight: ["500", "600", "700"], variable: "--font-display", display: "swap" });

export const metadata: Metadata = {
  title: "Organizali Finanças",
  description: "Gestão financeira da casa",
  applicationName: "Organizali Finanças",
};

export const viewport: Viewport = { width: "device-width", initialScale: 1, viewportFit: "cover" };

/** Theme and sidebar state applied before hydration, as in RevendaPRO: no light flash for dark users, no scrim with the wrong width. */
const applyTheme = `
(function(){try{
  var root=document.documentElement;
  var dark=localStorage.getItem("organizali-theme")==="dark";
  if(dark){root.classList.add("dark");}
  var m=document.querySelector("meta[name=theme-color]");
  if(!m){m=document.createElement("meta");m.name="theme-color";document.head.appendChild(m);}
  m.content=dark?"#1a1c1f":"#edf1f4";
  root.dataset.sidebar=localStorage.getItem("organizali-sidebar")==="1"?"collapsed":"expanded";
}catch(e){}})();
`;

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return <html lang="pt-BR" className={`${bodyFont.variable} ${displayFont.variable}`} suppressHydrationWarning>
    <head><script dangerouslySetInnerHTML={{ __html: applyTheme }} /></head>
    <body>{children}</body>
  </html>;
}
