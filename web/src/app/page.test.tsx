import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import HomePage from "./page";

describe("HomePage", () => {
  it("renders the main landmark with a Portuguese heading", () => {
    render(<HomePage />);

    expect(screen.getByRole("main")).toBeInTheDocument();
    expect(screen.getByRole("heading", { level: 1, name: "SilvaStock" })).toBeInTheDocument();
    expect(screen.getByText("Site em construção.")).toBeInTheDocument();
  });
});
