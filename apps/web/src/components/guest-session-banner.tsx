"use client";

import { useEffect, useState } from "react";
import { Link } from "@/i18n/navigation";
import {
  clearGuestSession,
  loadGuestSession,
  type GuestSessionRecord,
} from "@/lib/auth/guest-session";
import { Button } from "@game-guild/ui/components/button";

export interface GuestSessionBannerLabels {
  title: string;
  description: string;
  upgradeCta: string;
  leaveGuestMode: string;
  sessionLabel: string;
  permissionsLabel: string;
}

/**
 * Guest-state display (issue #213 web entry). Renders only while an active
 * guest session exists in the browser's guest slot. The upgrade CTA routes
 * into sign-up carrying the guest-session id: the sign-up form then presents
 * the guest bearer to the backend, whose upgrade flow links the new account
 * to the guest session (UpgradedFromGuestSessionId) — never the reverse.
 */
export function GuestSessionBanner({
  labels,
}: {
  labels: GuestSessionBannerLabels;
}) {
  const [record, setRecord] = useState<GuestSessionRecord | null>(null);

  // Read the guest slot after mount: the store is browser-only and the first
  // server-rendered pass must stay identical for hydration.
  useEffect(() => {
    setRecord(loadGuestSession());
  }, []);

  function handleLeaveGuestMode() {
    clearGuestSession();
    setRecord(null);
  }

  if (!record) return null;

  return (
    <aside
      data-testid="guest-session-banner"
      aria-label={labels.title}
      className="mx-auto mb-6 flex w-full max-w-7xl flex-col gap-3 rounded-lg border border-sky-400/25 bg-sky-400/10 px-4 py-3 text-sm sm:flex-row sm:items-center sm:justify-between"
    >
      <div className="flex flex-col gap-1">
        <p className="font-medium text-foreground">{labels.title}</p>
        <p className="text-muted-foreground">{labels.description}</p>
        <p className="text-xs text-muted-foreground">
          {labels.sessionLabel}:{" "}
          <span data-testid="guest-session-id" className="font-mono">
            {record.guestSessionId.slice(0, 8)}
          </span>
          {" · "}
          {labels.permissionsLabel}: {record.permissions.length}
        </p>
      </div>
      <div className="flex shrink-0 items-center gap-2">
        <Link
          href={`/sign-up?guestSessionId=${encodeURIComponent(record.guestSessionId)}`}
          data-testid="guest-upgrade-cta"
          className="inline-flex items-center justify-center rounded-full bg-primary px-4 py-2 text-sm font-semibold text-primary-foreground transition hover:bg-primary/85"
        >
          {labels.upgradeCta}
        </Link>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={handleLeaveGuestMode}
          data-testid="guest-leave-button"
        >
          {labels.leaveGuestMode}
        </Button>
      </div>
    </aside>
  );
}
