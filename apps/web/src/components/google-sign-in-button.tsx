"use client"

import { useEffect, useRef } from "react"
import { useAuth } from "@game-guild/client/react"
import {
  useGoogleIdentityService,
  type GisRenderButtonOptions,
} from "./use-google-identity-service"

const DEFAULT_BUTTON_OPTIONS: GisRenderButtonOptions = {
  type: "standard",
  theme: "outline",
  size: "large",
  text: "signin_with",
  shape: "rectangular",
  // ponytail: 400 is GIS max-width. Card content is ~400px (max-w-md 448px
  // minus padding), so this matches the Sign in button width.
  width: 400,
}

export interface GoogleSignInButtonProps {
  className?: string
  /** Override the default GIS button rendering options. */
  options?: GisRenderButtonOptions
  /** URL to redirect to after successful sign-in (e.g. "/dashboard"). */
  redirectTo?: string
}

/**
 * Renders Google's branded Sign-In button. The GIS credential callback
 * routes the (untrusted) Google ID token to signIn("google", { idToken }),
 * which exchanges it server-side via GoogleProvider (T2/T3).
 *
 * The underlying GIS hook (useGoogleIdentityService) is idempotent: the
 * GIS script and google.accounts.id.initialize run once per page even if
 * multiple consumers (e.g. <GoogleOneTap/> in T6) reuse the hook.
 */
export function GoogleSignInButton({
  className,
  options,
  redirectTo,
}: GoogleSignInButtonProps) {
  const { signIn } = useAuth()
  const containerRef = useRef<HTMLDivElement>(null)
  const { status, renderButton } = useGoogleIdentityService({
    onCredential: (credential) => {
      // credential is an untrusted ID token — backend verifies it.
      // .catch() swallows the re-thrown error (error state is already set by useAuth)
      signIn("google", { idToken: credential, redirectTo }).catch((e) => {console.warn(e)})
    },
  })

  useEffect(() => {
    if (status === "ready" && containerRef.current) {
      renderButton(containerRef.current, { ...DEFAULT_BUTTON_OPTIONS, ...options })
    }
  }, [status, renderButton, options])

  if (status === "error") {
    return (
      <div role="alert" className={className}>
        Google sign-in is unavailable.
      </div>
    )
  }

  return (
    <div
      ref={containerRef}
      data-testid="google-sign-in-button"
      className={className}
      // Reserve layout while GIS hydrates the button to prevent CLS.
      style={{ minHeight: 40, width: "100%", display: "flex", justifyContent: "center" }}
    />
  )
}
