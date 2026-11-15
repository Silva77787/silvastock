import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import RootLayout, { metadata } from "./layout";

describe("RootLayout", () => {
  it("declares the document language as pt-PT", () => {
    const html = renderToStaticMarkup(
      <RootLayout>
        <p>conteúdo</p>
      </RootLayout>,
    );

    expect(html).toContain('<html lang="pt-PT">');
    expect(html).toContain("<p>conteúdo</p>");
  });

  it("uses SilvaStock as the default title", () => {
    expect(metadata.title).toMatchObject({ default: "SilvaStock" });
  });
});
