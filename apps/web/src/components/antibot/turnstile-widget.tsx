"use client";

import { Turnstile } from "@marsidev/react-turnstile";
import { useEffect, useRef } from "react";

export interface TurnstileWidgetProps {
  /** Receives the Turnstile token once solved, or null when the widget errors, expires, or is disabled. */
  onToken: (token: string | null) => void;
  /** Optional Turnstile `action` (validated server-side against the expected value). */
  action?: string;
  /** Optional DOM id for the widget container (test/automation anchoring). */
  id?: string;
}

/**
 * Cloudflare Turnstile challenge widget for sign-in/sign-up forms.
 *
 * Behavior:
 * - `NEXT_PUBLIC_TURNSTILE_SITE_KEY` unset/empty → renders nothing and reports
 *   `onToken(null)` exactly once, so forms keep working without the challenge
 *   (dev/e2e path; the server-side gate fails open through verifyTurnstileToken).
 * - Key set → renders the challenge; every terminal outcome (success, error,
 *   expire) is reported through `onToken`.
 */
export function TurnstileWidget({ onToken, action, id }: TurnstileWidgetProps) {
  const siteKey = process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY;
  const notifiedDisabled = useRef(false);

  useEffect(() => {
    if (!siteKey && !notifiedDisabled.current) {
      notifiedDisabled.current = true;
      onToken(null);
    }
  }, [onToken, siteKey]);

  if (!siteKey) {
    return null;
  }

  return (
    <div id={id} role="group" aria-label="Bot verification challenge">
      <span className="sr-only">Complete the bot verification challenge to continue.</span>
      <Turnstile
        siteKey={siteKey}
        onSuccess={(token) => onToken(token)}
        onError={() => onToken(null)}
        onExpire={() => onToken(null)}
        {...(action ? { options: { action } } : {})}
      />
    </div>
  );
}
