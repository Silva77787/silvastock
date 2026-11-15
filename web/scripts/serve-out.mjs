// Minimal local preview of the static export in out/ (run `npm run build` first).
// Development only: production is served by Cloudflare Pages.
import { createReadStream } from "node:fs";
import { stat } from "node:fs/promises";
import { createServer } from "node:http";
import path from "node:path";
import { fileURLToPath } from "node:url";

const outDir = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..", "out");
const port = Number(process.env.PORT ?? 3000);

const contentTypes = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".txt": "text/plain; charset=utf-8",
  ".svg": "image/svg+xml",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".webp": "image/webp",
  ".ico": "image/x-icon",
  ".woff2": "font/woff2",
};

async function resolveFile(urlPath) {
  const decoded = decodeURIComponent(urlPath.split("?")[0]);
  const candidate = path.resolve(outDir, "." + decoded);
  if (candidate !== outDir && !candidate.startsWith(outDir + path.sep)) return null;
  try {
    const info = await stat(candidate);
    if (info.isDirectory()) {
      const index = path.join(candidate, "index.html");
      await stat(index);
      return index;
    }
    return candidate;
  } catch {
    return null;
  }
}

createServer(async (req, res) => {
  const file = await resolveFile(req.url ?? "/");
  const notFound = file ?? (await resolveFile("/404.html"));
  if (!notFound) {
    res.writeHead(404, { "Content-Type": "text/plain; charset=utf-8" }).end("404");
    return;
  }
  res.writeHead(file ? 200 : 404, {
    "Content-Type": contentTypes[path.extname(notFound)] ?? "application/octet-stream",
  });
  createReadStream(notFound).pipe(res);
}).listen(port, () => {
  console.log(`Serving ${outDir} at http://localhost:${port}`);
});
