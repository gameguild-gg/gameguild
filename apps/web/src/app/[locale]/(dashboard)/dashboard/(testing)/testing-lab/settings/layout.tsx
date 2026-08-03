import { TestingLabSettingsNav } from "@/components/testing-lab/testing-lab-settings-nav";
import type { ReactNode } from "react";

export default function TestingLabSettingsLayout({
  children,
}: {
  children: ReactNode;
}) {
  return (
    <div className="grid min-w-0 lg:grid-cols-[13rem_minmax(0,1fr)]">
      <aside className="border-b p-3 lg:min-h-[calc(100dvh-4rem)] lg:border-b-0 lg:border-r lg:p-4">
        <TestingLabSettingsNav />
      </aside>
      <div className="min-w-0">{children}</div>
    </div>
  );
}
