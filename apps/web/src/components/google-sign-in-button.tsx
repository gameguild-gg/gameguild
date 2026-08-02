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
  width: 320,
}

export interface GoogleSignInButtonProps {
  className?: string
  /** Override the default GIS button rendering options. */
  options?: GisRenderButtonOptions
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
}: GoogleSignInButtonProps) {
  const { signIn } = useAuth()
  const containerRef = useRef<HTMLDivElement>(null)
  const { status, renderButton } = useGoogleIdentityService({
    onCredential: (credential) => {
      // credential is an untrusted ID token — backend verifies it.
      void signIn("google", { idToken: credential })
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
      style={{ minHeight: 40 }}
    />
  )
}
