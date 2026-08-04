"use client"

import { useEffect } from "react"
import { useAuth } from "@game-guild/client/react"
import { useGoogleIdentityService } from "./use-google-identity-service"

export interface GoogleOneTapProps {
  /**
   * Suppress the One Tap prompt when the user is already authenticated.
   * The sign-in page (the only public mount point today) leaves this false;
   * mount points that may render for logged-in users pass true.
   */
  authenticated?: boolean
  /** URL to redirect to after successful sign-in (e.g. "/dashboard"). */
  redirectTo?: string
}

/**
 * Triggers Google's One Tap prompt. The GIS credential callback routes
 * the (untrusted) Google ID token to signIn("google", { idToken }), which
 * exchanges it server-side via GoogleProvider.
 *
 * The underlying GIS hook (useGoogleIdentityService) is idempotent: the
 * GIS script and google.accounts.id.initialize run once per page even if
 * multiple consumers (e.g. <GoogleSignInButton/>) reuse the hook.
 */
export function GoogleOneTap({
  authenticated = false,
  redirectTo,
}: GoogleOneTapProps) {
  const { signIn } = useAuth()
  const { status, prompt } = useGoogleIdentityService({
    onCredential: (credential) => {
      // credential is an untrusted ID token — backend verifies it.
      // .catch() swallows the re-thrown error (error state is already set by useAuth)
      signIn("google", { idToken: credential, redirectTo }).catch((e) => {console.warn(e)})
    },
  })

  useEffect(() => {
    if (status === "ready" && !authenticated) {
      prompt()
    }
  }, [status, prompt, authenticated])

  // ponytail: One Tap renders into GIS's own iframe; no DOM output here.
  return null
}
