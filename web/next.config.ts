import type { NextConfig } from "next";

// Static export only (Cloudflare Pages). Do not add features that need a server:
// no API routes, middleware/proxy, server actions, headers()/cookies() or dynamic rendering.
const nextConfig: NextConfig = {
  output: "export",
  trailingSlash: true,
  images: {
    unoptimized: true,
  },
};

export default nextConfig;
