"use client";

import { useEffect, useRef } from "react";

export interface TurnstileWidgetProps {
  /** Receives the Turnstile token once solved, or null when the widget errors, expires, or is disabled. */
  onToken: (token: string | null) => void;
  /** Optional Turnstile `action` (validated server-side against the expected value). */
  action?: string;
  /** Optional DOM id for the widget container (test/automation anchoring). */
  id?: string;
}

interface TurnstileApi {
  render: (
    container: HTMLElement,
    options: Record<string, unknown>,
  ) => string;
  remove: (widgetId: string) => void;
}

declare global {
  interface Window {
    turnstile?: TurnstileApi;
  }
}

const SCRIPT_ID = "cf-turnstile-script";
const SCRIPT_SRC =
  "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";

/** Loads the Turnstile script once per document (explicit mode, no onload query param). */
function loadTurnstileScript(): void {
  if (document.getElementById(SCRIPT_ID)) return;
  const script = document.createElement("script");
  script.id = SCRIPT_ID;
  script.src = SCRIPT_SRC;
  script.async = true;
  script.defer = true;
  document.head.appendChild(script);
}

/**
 * Cloudflare Turnstile challenge widget for sign-in/sign-up forms.
 *
 * First-party explicit-mode implementation (replaces @marsidev/react-turnstile,
 * whose onload-callback race caused a doubled empty box in production).
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
  const onTokenRef = useRef(onToken);
  onTokenRef.current = onToken;
  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!siteKey && !notifiedDisabled.current) {
      notifiedDisabled.current = true;
      onTokenRef.current?.(null);
    }
  }, [siteKey]);

  useEffect(() => {
    if (!siteKey) return;
    loadTurnstileScript();

    let widgetId: string | undefined;
    let cancelled = false;
    const container = containerRef.current;
    if (!container) return;

    const render = () => {
      if (cancelled || widgetId || !window.turnstile || !containerRef.current)
        return;
      widgetId = window.turnstile.render(containerRef.current, {
        sitekey: siteKey,
        ...(action ? { action } : {}),
        callback: (token: string) => onTokenRef.current?.(token),
        "error-callback": () => onTokenRef.current?.(null),
        "expired-callback": () => onTokenRef.current?.(null),
        theme: "auto",
      });
    };

    if (window.turnstile) {
      render();
    } else {
      const poll = window.setInterval(() => {
        if (window.turnstile) {
          window.clearInterval(poll);
          render();
        }
      }, 100);
      return () => {
        cancelled = true;
        window.clearInterval(poll);
        if (widgetId && window.turnstile) window.turnstile.remove(widgetId);
      };
    }

    return () => {
      cancelled = true;
      if (widgetId && window.turnstile) window.turnstile.remove(widgetId);
    };
  }, [siteKey, action]);

  if (!siteKey) {
    return null;
  }

  return (
    <div id={id} role="group" aria-label="Bot verification challenge">
      <span className="sr-only">Complete the bot verification challenge to continue.</span>
      <div ref={containerRef} id={`${id ?? "turnstile"}-container`} />
    </div>
  );
}
