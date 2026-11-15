import type { paths } from "./schema";

/** Every path defined in docs/api/openapi.yaml (e.g. "/api/public/vehicles/{id}"). */
export type ApiPath = keyof paths;

/**
 * Base URL of the SilvaStock API, without a trailing slash.
 *
 * Read from NEXT_PUBLIC_API_BASE_URL. Next.js inlines this value at build time,
 * so it must be accessed literally as `process.env.NEXT_PUBLIC_API_BASE_URL`.
 */
export function getApiBaseUrl(): string {
  const value = process.env.NEXT_PUBLIC_API_BASE_URL?.trim();
  if (!value) {
    throw new Error("NEXT_PUBLIC_API_BASE_URL is not set. See web/.env.example.");
  }

  let url: URL;
  try {
    url = new URL(value);
  } catch {
    throw new Error(`NEXT_PUBLIC_API_BASE_URL is not a valid URL: "${value}".`);
  }
  if (url.protocol !== "http:" && url.protocol !== "https:") {
    throw new Error(`NEXT_PUBLIC_API_BASE_URL must use http or https: "${value}".`);
  }

  return value.replace(/\/+$/, "");
}
