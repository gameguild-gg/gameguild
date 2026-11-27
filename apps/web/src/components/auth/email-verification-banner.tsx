"use client";

import { useState, useSyncExternalStore } from "react";
import { useTranslations } from "next-intl";
import { MailWarning, X } from "lucide-react";
import { Link } from "@/i18n/navigation";
import { buttonVariants } from "@game-guild/ui/components/button";
import { cn } from "@/lib/utils";

/** sessionStorage flag: the reminder stays hidden for the rest of the browser session. */
export const EMAIL_VERIFICATION_BANNER_STORAGE_KEY = "gg.email-verification-banner";

// No-op subscribe: dismissal only changes from this component's own button.
const subscribeToDismissal = () => () => undefined;

function isDismissed(): boolean {
  try {
    return window.sessionStorage.getItem(EMAIL_VERIFICATION_BANNER_STORAGE_KEY) === "1";
  } catch {
    // sessionStorage can throw (privacy mode, disabled storage) — treat as "not dismissed".
    return false;
  }
}

function rememberDismissal(): void {
  try {
    window.sessionStorage.setItem(EMAIL_VERIFICATION_BANNER_STORAGE_KEY, "1");
  } catch {
    // Best-effort: a failed write only means the reminder returns in a new tab.
  }
}

/**
 * Reminder for a signed-in user whose email the backend reports as
 * unverified (#1298 J1). It is informational in this phase — nothing is
 * blocked — and the layouts only mount it when the session carries
 * `emailVerified === false`. Dismissal lasts for the browser session, so the
 * reminder comes back the next time rather than being lost for good.
 */
export function EmailVerificationBanner({
  email,
  className,
}: {
  email: string;
  className?: string;
}) {
  const t = useTranslations("emailVerification.banner");
  const [manualDismiss, setManualDismiss] = useState(false);

  // The server snapshot returns true (hidden) so the server markup and the
  // first client render agree; React reads the browser's flag after hydration.
  const dismissed = useSyncExternalStore(
    subscribeToDismissal,
    isDismissed,
    () => true,
  );

  if (dismissed || manualDismiss) return null;

  return (
    <div
      role="status"
      data-testid="email-verification-banner"
      className={cn(
        "flex flex-wrap items-center justify-between gap-3 rounded-lg border border-sky-200 bg-sky-50 p-4 text-sky-900 dark:border-sky-900 dark:bg-sky-950/50 dark:text-sky-200",
        className,
      )}
    >
      <div className="flex items-start gap-3">
        <MailWarning className="mt-0.5 size-5 shrink-0" aria-hidden="true" />
        <p className="text-sm font-medium">{t("title", { email })}</p>
      </div>
      <div className="flex items-center gap-2">
        <Link
          href={`/verify?email=${encodeURIComponent(email)}`}
          className={cn(buttonVariants({ size: "sm" }))}
          data-testid="email-verification-banner-action"
        >
          {t("action")}
        </Link>
        <button
          type="button"
          onClick={() => {
            rememberDismissal();
            setManualDismiss(true);
          }}
          aria-label={t("dismiss")}
          data-testid="email-verification-banner-dismiss"
          className="rounded-sm p-1 transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
        >
          <X className="size-4" aria-hidden="true" />
        </button>
      </div>
    </div>
  );
}
