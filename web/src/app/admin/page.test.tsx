import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import AdminPage, { metadata } from "./page";

describe("AdminPage", () => {
  it("renders the back-office heading in Portuguese", () => {
    render(<AdminPage />);

    expect(screen.getByRole("main")).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 1, name: "Área de gestão" })).toBeInTheDocument();
  });

  it("asks search engines not to index the back office", () => {
    expect(metadata.robots).toEqual({ index: false, follow: false });
  });
});
