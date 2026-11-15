import { afterEach, describe, expect, it, vi } from "vitest";
import { getApiBaseUrl } from "./config";

describe("getApiBaseUrl", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("returns the configured URL", () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "http://localhost:5080");
    expect(getApiBaseUrl()).toBe("http://localhost:5080");
  });

  it("removes trailing slashes", () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "https://api.example.test//");
    expect(getApiBaseUrl()).toBe("https://api.example.test");
  });

  it("throws when the variable is missing or empty", () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "");
    expect(() => getApiBaseUrl()).toThrow(/is not set/);
  });

  it("throws when the value is not a URL", () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "not a url");
    expect(() => getApiBaseUrl()).toThrow(/not a valid URL/);
  });

  it("throws for non-http protocols", () => {
    vi.stubEnv("NEXT_PUBLIC_API_BASE_URL", "ftp://api.example.test");
    expect(() => getApiBaseUrl()).toThrow(/http or https/);
  });
});
