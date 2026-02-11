"use client"

import { useSearchParams, useRouter } from "next/navigation"
import { InputOTPForm } from "@/components/input-otp-form"

export function VerifyPageContent() {
  const searchParams = useSearchParams()
  const router = useRouter()
  const email = searchParams.get("email") ?? ""

  if (!email) {
    router.replace("/sign-in")
    return null
  }

  async function handleVerify(code: string) {
    const response = await fetch("/api/auth/email/verify", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token: code }),
    })

    if (!response.ok) {
      const data = await response.json().catch(() => ({}))
      throw new Error(
        (data as Record<string, string>).message ||
          "Verification failed. Please try again."
      )
    }

    router.push("/dashboard")
  }

  async function handleResend() {
    const response = await fetch("/api/auth/email/send-verification", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email }),
    })

    if (!response.ok) {
      const data = await response.json().catch(() => ({}))
      throw new Error(
        (data as Record<string, string>).message ||
          "Failed to resend verification email."
      )
    }
  }

  return (
    <InputOTPForm
      email={email}
      onVerify={handleVerify}
      onResend={handleResend}
    />
  )
}
