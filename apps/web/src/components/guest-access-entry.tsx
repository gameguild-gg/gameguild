"use client";

import { useState } from "react";
import { useRouter } from "@/i18n/navigation";
import {
  clearGuestSession,
  saveGuestSession,
  type GuestSessionRecord,
} from "@/lib/auth/guest-session";
import { Button } from "@game-guild/ui/components/button";

export interface GuestAccessEntryLabels {
  continueAsGuest: string;
  busy: string;
  disabledMessage: string;
  errorMessage: string;
}

/**
 * "Continue as guest" affordance for the sign-in page (issue #213 web entry).
 *
 * Rendered only when the guest-access capability is enabled (server-side gate
 * in the sign-in page, mirroring the backend's Authentication:Guest:IsEnabled
 * posture). Issuance goes through the scoped /api/auth/guest-session proxy;
 * the returned guest session (own audience, no refresh token) is stored in the
 * browser's guest slot — never in the auth cookies.
 */
export function GuestAccessEntry({
  redirectTo = "/",
  labels,
}: {
  redirectTo?: string;
  labels: GuestAccessEntryLabels;
}) {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleContinueAsGuest() {
    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch("/api/auth/guest-session", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({}),
      });

      if (!response.ok) {
        const data = (await response.json().catch(() => ({}))) as {
          error?: string;
        };
        setError(
          data.error === "guest_access_disabled"
            ? labels.disabledMessage
            : labels.errorMessage,
        );
        return;
      }

      const data = (await response.json()) as Partial<GuestSessionRecord>;
      if (
        typeof data.guestSessionId !== "string" ||
        typeof data.accessToken !== "string" ||
        typeof data.expiresAt !== "string"
      ) {
        setError(labels.errorMessage);
        return;
      }

      // Only a freshly issued session replaces the stored one: stale guest
      // state is dropped so an old token never shadows the new credential.
      clearGuestSession();
      saveGuestSession({
        guestSessionId: data.guestSessionId,
        accessToken: data.accessToken,
        expiresAt: data.expiresAt,
        permissions: Array.isArray(data.permissions) ? data.permissions : [],
      });

      router.replace(redirectTo);
    } catch {
      setError(labels.errorMessage);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="flex flex-col gap-2" data-testid="guest-access-entry">
      <Button
        type="button"
        variant="outline"
        size="lg"
        disabled={isLoading}
        onClick={() => {
          void handleContinueAsGuest();
        }}
        className="h-10 border-white/10 bg-white/5 text-white hover:bg-white/10"
      >
        {isLoading ? labels.busy : labels.continueAsGuest}
      </Button>
      {error ? (
        <p role="alert" className="text-sm text-red-300">
          {error}
        </p>
      ) : null}
    </div>
  );
}
