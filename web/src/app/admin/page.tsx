import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Área de gestão",
  robots: { index: false, follow: false },
};

// Placeholder for the back office. Layout and content come from docs/design/.
export default function AdminPage() {
  return (
    <main>
      <h1>Área de gestão</h1>
      <p>Em construção.</p>
    </main>
  );
}
