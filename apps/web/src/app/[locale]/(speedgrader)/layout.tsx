import { auth } from "@/auth";
import { LearningAuthRedirect } from "@/components/learning/learning-auth-redirect";
import type { Metadata } from "next";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  robots: {
    follow: false,
    index: false,
  },
};

/**
 * SpeedGrader full-viewport shell — no dashboard chrome.
 *
 * This repo has no middleware.ts, so auth is enforced per-layout: the gate
 * below mirrors learn/layout.tsx verbatim (LearningAuthRedirect preserves the
 * destination across sign-in).
 */
export default async function SpeedgraderLayout({
  children,
}: {
  children: ReactNode;
}) {
  const session = await auth();

  if (!session?.user) {
    return <LearningAuthRedirect />;
  }

  return <div className="flex h-dvh min-h-dvh flex-col overflow-hidden">{children}</div>;
}
