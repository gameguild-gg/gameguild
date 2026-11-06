"use client";

import { useCallback, useMemo, useRef, useState } from "react";
import { useAuth } from "@game-guild/client/react";
import { TurnstileWidget } from "@/components/antibot/turnstile-widget";
import {
  Web3Provider,
  createWalletAdapters,
  shortenAddress,
  useWeb3,
  useWalletAddress,
} from "@game-guild/web3";
import type { SiweTransport, WalletAdapter } from "@game-guild/web3";

/**
 * Wallet sign-in button group (issues #16/#17/#18/#38/#39 under umbrella
 * #291).
 *
 * Renders one button per available wallet from the wallet-adapter registry:
 * MetaMask whenever the injected provider is detected, Trust Wallet through
 * EIP-6963/injected detection with a mobile deep-link fallback (#17), and
 * WalletConnect behind NEXT_PUBLIC_WALLETCONNECT_PROJECT_ID (#18, adapter
 * additionally fails closed when the SDK is not installed). Torus stays
 * behind NEXT_PUBLIC_WALLET_TORUS_ENABLED. Clicking runs the full SIWE
 * journey against the existing backend endpoints: same-origin
 * /api/auth/web3/challenge proxy → personal_sign → signIn("credentials", …)
 * which the server-side web3 credentials wrapper verifies via
 * /v1/auth/web3:verify.
 */

const CHALLENGE_ENDPOINT = "/api/auth/web3/challenge";

const TORUS_ENABLED = process.env.NEXT_PUBLIC_WALLET_TORUS_ENABLED === "true";
const WALLETCONNECT_PROJECT_ID = process.env.NEXT_PUBLIC_WALLETCONNECT_PROJECT_ID;

export interface WalletSignInLabels {
  continueWithWallet: string;
  connecting: string;
  signing: string;
  connected: string;
  disconnect: string;
}

const ERROR_MESSAGES: Record<string, string> = {
  user_rejected: "You rejected the request in your wallet. Nothing was signed.",
  wallet_not_found: "This wallet was not detected in your browser.",
  challenge_failed: "Could not start wallet sign-in. Please try again.",
  "bot-check-failed": "Bot check failed. Please refresh and try again.",
  verification_failed: "Wallet sign-in failed. The challenge may have expired — please try again.",
  unauthorized: "Connect your wallet first.",
  disconnected: "Your wallet disconnected.",
};

const friendlyError = (code: string, detail: string): string => {
  const base = ERROR_MESSAGES[code] ?? "Wallet sign-in failed.";
  return detail && !ERROR_MESSAGES[code] ? `${base} ${detail}` : base;
};

const walletIcon = (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" aria-hidden="true" className="size-5">
    <path d="M3 7.5A2.5 2.5 0 0 1 5.5 5H18a2 2 0 0 1 2 2v1" strokeLinecap="round" />
    <path d="M3 7.5V17a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-3" strokeLinecap="round" />
    <path d="M21 10h-4.5a2.5 2.5 0 0 0 0 5H21v-5Z" strokeLinecap="round" />
  </svg>
);

/**
 * Wallet adapters registered for the sign-in buttons: the shared registry
 * (MetaMask + Trust Wallet by default, WalletConnect/Torus behind their
 * deployment gates). Exported for tests and host apps that render the same
 * wallet set elsewhere.
 */
export const createSignInWalletAdapters = (): WalletAdapter[] =>
  createWalletAdapters({
    ...(TORUS_ENABLED ? { torus: { enabled: true, network: { host: "mainnet" } } } : {}),
    // Config-gated fail-closed: without a project id the adapter reports
    // unavailable and the button stays hidden (#18).
    ...(WALLETCONNECT_PROJECT_ID
      ? { walletConnect: { enabled: true, projectId: WALLETCONNECT_PROJECT_ID } }
      : {}),
  });

export function WalletSignInButtons({
  redirectTo,
  labels,
  adapters,
}: {
  redirectTo?: string;
  labels: WalletSignInLabels;
  /** Overrides the default registry composition (tests, host reuse). */
  adapters?: readonly WalletAdapter[];
}) {
  const registered = useMemo<WalletAdapter[]>(
    () => (adapters ? [...adapters] : createSignInWalletAdapters()),
    [adapters],
  );

  return (
    <Web3Provider adapters={registered}>
      <WalletButtonGroup redirectTo={redirectTo} labels={labels} />
    </Web3Provider>
  );
}

function WalletButtonGroup({
  redirectTo,
  labels,
}: {
  redirectTo?: string;
  labels: WalletSignInLabels;
}) {
  const { signIn } = useAuth();
  // Lazy anti-bot (hotfix: duplicate Turnstile on /sign-in): the wallet widget
  // mounts only after the user actually starts the wallet flow, so the initial
  // /sign-in render shows exactly one challenge (the email form's). The token
  // is only needed for /api/auth/web3/challenge, so deferring the render is
  // safe — the SIWE journey below is gated on `tokenResolved`, which the
  // widget sets on its first onToken callback (a real token when the site key
  // is set, or null immediately when the challenge is disabled in dev/e2e).
  const [flowStarted, setFlowStarted] = useState(false);
  const [pendingWalletId, setPendingWalletId] = useState<string | null>(null);
  const [tokenResolved, setTokenResolved] = useState(false);
  // Fresh-at-call-time token (review finding b): the transport useMemo
  // captures state by value, so the first token committed by handleToken
  // would be invisible to the `transport` instance already captured by the
  // in-flight handleConnect closure — the first solved challenge would send
  // `turnstileToken: null` and 403 in prod. Reading through a ref at
  // challenge-call time fixes that without rebuilding the transport mid-flow.
  const turnstileTokenRef = useRef<string | null>(null);
  // Set by onError before the widget's paired onToken(null) arrives, so
  // handleToken can tell "errored" nulls from the challenge-disabled null.
  const widgetFailedRef = useRef(false);
  // Widget mount epoch (review finding c): bumping it remounts the widget so
  // Cloudflare re-issues a fresh challenge after an error, expiry, or a
  // failed SIWE attempt.
  const [widgetEpoch, setWidgetEpoch] = useState(0);
  // Accessed through the context object (not destructured): the linter's
  // unbound-method rule fires on functions separated from their owning
  // object. `useWeb3` (not the `useWallet` snapshot) exposes `authenticate`
  // alongside the connection lifecycle.
  const web3 = useWeb3();
  const isPending =
    web3.state.status === "connecting" || web3.state.status === "signing";
  // The account lives on the full context state, not the wallet snapshot.
  const account = useWalletAddress() ?? null;

  // Reset the anti-bot state so the next wallet click remounts a fresh
  // widget (findings c.i/c.ii): unmounts the widget, releases any pending
  // wallet lock, drops the possibly-stale/expired token, and clears the
  // resolved flag — the user's next click re-arms the lazy widget and waits
  // for a new token. Without this, an errored widget leaves every wallet
  // button permanently disabled and a >5-minute-old token is replayed.
  const resetTurnstile = useCallback(() => {
    setFlowStarted(false);
    setPendingWalletId(null);
    setTokenResolved(false);
    turnstileTokenRef.current = null;
    widgetFailedRef.current = false;
    setWidgetEpoch((epoch) => epoch + 1);
  }, []);

  const transport = useMemo<SiweTransport>(
    () => ({
      getChallenge: async (request) => {
        const response = await fetch(CHALLENGE_ENDPOINT, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            walletAddress: request.walletAddress,
            chainId: request.chainId,
            turnstileToken: turnstileTokenRef.current,
          }),
        }).catch((error) => {
          resetTurnstile();
          throw error;
        });
        const data = (await response.json().catch(() => null)) as {
          challenge?: string;
          nonce?: string;
          expiresAt?: string;
          message?: string;
          error?: string;
        } | null;
        if (!response.ok || !data?.challenge || !data.nonce) {
          resetTurnstile();
          throw new Error(data?.message ?? data?.error ?? "challenge_failed");
        }
        return { challenge: data.challenge, nonce: data.nonce, expiresAt: data.expiresAt };
      },
      verify: async (request) => {
        // The credentials server action verifies the signature against
        // /v1/auth/web3:verify and establishes the session cookie. Any
        // failure is rethrown so the context lands in verification_failed.
        await signIn("credentials", {
          siweWalletAddress: request.walletAddress,
          siweChallenge: request.challenge,
          siweSignature: request.signature,
          siweNonce: request.nonce,
          siweChainId: request.chainId,
          rememberMe: true,
          redirectTo,
        });
      },
    }),
    // The token is read through turnstileTokenRef at challenge-call time
    // (finding b), so no token dependency here; resetTurnstile is stable.
    [signIn, redirectTo, resetTurnstile],
  );

  async function handleConnect(walletId: string, proceed: boolean) {
    web3.clearError();
    try {
      if (!proceed) {
        setFlowStarted(true);
        setPendingWalletId(walletId);
        return;
      }
      await web3.connect(walletId);
      await web3.authenticate(transport);
    } catch {
      // Tokens are single-use; a failed attempt must not reuse the old proof.
      resetTurnstile();
    }
  }

  function handleToken(token: string | null) {
    const failed = widgetFailedRef.current;
    widgetFailedRef.current = false;
    if (token === null && failed) {
      // Errored/expired widget (finding c): recover instead of running the
      // SIWE flow without proof.
      resetTurnstile();
      return;
    }
    turnstileTokenRef.current = token;
    setTokenResolved(true);
    const walletId = pendingWalletId;
    setPendingWalletId(null);
    if (walletId) void handleConnect(walletId, true);
  }

  function handleWidgetError() {
    widgetFailedRef.current = true;
  }

  const availableWallets = web3.wallets.filter((wallet) => wallet.available);

  if (availableWallets.length === 0) return null;

  if (account) {
    return (
      <div className="flex flex-col gap-2" data-testid="wallet-connected">
        <div className="flex h-10 w-full items-center justify-center gap-2 rounded-md border border-white/10 bg-white/5 text-sm font-medium text-white">
          {walletIcon}
          {labels.connected.replace("{address}", shortenAddress(account))}
        </div>
        <button
          type="button"
          data-testid="wallet-disconnect"
          onClick={() => void web3.disconnect().catch(() => undefined)}
          className="text-xs text-slate-400 underline-offset-4 hover:text-slate-200 hover:underline"
        >
          {labels.disconnect}
        </button>
        {web3.state.error ? <WalletError error={web3.state.error} /> : null}
      </div>
    );
  }

  return (
    <div className="contents" data-testid="wallet-sign-in-buttons">
      {flowStarted ? (
        // key=widgetEpoch: remount forces a fresh Cloudflare challenge after
        // a reset (stale/expired/failed token).
        <TurnstileWidget
          key={widgetEpoch}
          onToken={handleToken}
          onError={handleWidgetError}
          id="wallet-sign-in-turnstile"
        />
      ) : null}
      {availableWallets.map((wallet) => (
        <button
          key={wallet.id}
          type="button"
          data-testid={`${wallet.id}-sign-in-button`}
          disabled={isPending || pendingWalletId !== null}
          onClick={() => void handleConnect(wallet.id, tokenResolved)}
          className="flex h-10 w-full items-center justify-center gap-2 rounded-md border border-white/10 bg-white/10 text-sm font-medium text-white shadow-xs transition hover:bg-white/15 focus-visible:outline-none focus-visible:ring-3 focus-visible:ring-ring/50 active:translate-y-px disabled:cursor-not-allowed disabled:opacity-60"
        >
          {walletIcon}
          {web3.state.status === "signing"
            ? labels.signing
            : web3.state.status === "connecting"
              ? labels.connecting
              : labels.continueWithWallet.replace("{wallet}", wallet.name)}
        </button>
      ))}
      {web3.state.error ? <WalletError error={web3.state.error} /> : null}
    </div>
  );
}

function WalletError({ error }: { error: { code: string; message: string } }) {
  return (
    <p role="alert" data-testid="wallet-error" className="text-xs text-red-300">
      {friendlyError(error.code, error.message)}
    </p>
  );
}
