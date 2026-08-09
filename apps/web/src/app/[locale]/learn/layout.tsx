import { auth } from "@/auth";
import { LearningAuthRedirect } from "@/components/learning/learning-auth-redirect";
import { LearningShell } from "@/components/learning/learning-shell";
import { getDashboardNotificationSummary } from "@/lib/dashboard-notifications";
import { getCentralSignInUrl } from "@/lib/learner/routes";
import type { Metadata } from "next";
import { headers } from "next/headers";
import Script from "next/script";
import type { ReactNode } from "react";

export const metadata: Metadata = {
  robots: {
    follow: false,
    index: false,
  },
};

export default async function LearningLayout({
  children,
}: {
  children: ReactNode;
}) {
  const session = await auth();
  const webOrigin =
    process.env.WEB_PUBLIC_URL ||
    process.env.NEXT_PUBLIC_APP_URL ||
    "https://gameguild.gg";
  const learningOrigin =
    process.env.LEARNING_PUBLIC_URL ||
    process.env.NEXT_PUBLIC_LEARNING_APP_URL ||
    "https://learning.gameguild.gg";

  if (!session?.user) {
    const requestHeaders = await headers();
    const visibleUrl = requestHeaders.get("x-gameguild-visible-url");
    const visibleRequest = visibleUrl
      ? new URL(visibleUrl)
      : new URL("/", learningOrigin);
    const returnPath = `${visibleRequest.pathname}${visibleRequest.search}`;
    return (
      <>
        <Script src="/coi-serviceworker.js" strategy="beforeInteractive" />
        <LearningAuthRedirect
          href={getCentralSignInUrl({
            learningOrigin,
            pathname: returnPath,
            webOrigin,
          })}
        />
      </>
    );
  }

  const name =
    session.user.name?.trim() ||
    session.user.email?.split("@")[0] ||
    "GameGuild learner";
  const notifications = await getDashboardNotificationSummary(session.user.id);

  return (
    <>
      <Script src="/coi-serviceworker.js" strategy="beforeInteractive" />
      <LearningShell
        notifications={notifications}
        user={{
          id: session.user.id,
          name,
          email: session.user.email || "",
          image: session.user.image,
        }}
        webOrigin={webOrigin}
      >
        {children}
      </LearningShell>
    </>
  );
}
