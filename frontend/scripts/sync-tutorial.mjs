// Copies docs/product/tutorial.md into content/tutorial.ts so the Ajuda page ships inside the frontend image
// (the docs folder is not part of the build context). Run after editing the tutorial: node scripts/sync-tutorial.mjs
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const source = resolve(root, "../docs/product/tutorial.md");
const target = resolve(root, "content/tutorial.ts");
const markdown = readFileSync(source, "utf8").replace(/\r\n/g, "\n");
const banner = "// Gerado por scripts/sync-tutorial.mjs a partir de docs/product/tutorial.md. Não edite à mão.\n";
writeFileSync(target, `${banner}export const tutorial = ${JSON.stringify(markdown)};\n`, "utf8");
console.log(`tutorial.ts atualizado (${markdown.length} caracteres)`);
