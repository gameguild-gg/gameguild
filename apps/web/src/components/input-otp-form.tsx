"use client"

import { type FormEvent, useState, useCallback } from "react"
import Link from "next/link"
import { Button } from "@game-guild/ui/components/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card"
import {
  Field,
  FieldDescription,
  FieldError,
  FieldLabel,
} from "@game-guild/ui/components/field"
import {
  InputOTP,
  InputOTPGroup,
  InputOTPSeparator,
  InputOTPSlot,
} from "@game-guild/ui/components/input-otp"
import { RefreshCwIcon } from "lucide-react"

interface InputOTPFormProps {
  /** The email address the OTP was sent to */
  email: string
  /** Called when the user submits the OTP code */
  onVerify: (code: string) => Promise<void>
  /** Called when the user requests a new code */
  onResend?: () => Promise<void>
}

export function InputOTPForm({ email, onVerify, onResend }: InputOTPFormProps) {
  const [code, setCode] = useState("")
  const [isVerifying, setIsVerifying] = useState(false)
  const [isResending, setIsResending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = useCallback(
    async (e: FormEvent<HTMLFormElement>) => {
      e.preventDefault()
      setError(null)

      if (code.length !== 6) {
        setError("Please enter the full 6-digit code.")
        return
      }

      setIsVerifying(true)
      try {
        await onVerify(code)
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Verification failed. Please try again."
        )
      } finally {
        setIsVerifying(false)
      }
    },
    [code, onVerify]
  )

  const handleResend = useCallback(async () => {
    if (!onResend) return
    setIsResending(true)
    setError(null)
    try {
      await onResend()
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to resend code."
      )
    } finally {
      setIsResending(false)
    }
  }, [onResend])

  return (
    <form onSubmit={handleSubmit}>
      <Card className="mx-auto max-w-md">
        <CardHeader>
          <CardTitle>Verify your login</CardTitle>
          <CardDescription>
            Enter the verification code we sent to your email address:{" "}
            <span className="font-medium">{email}</span>.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Field>
            <div className="flex items-center justify-between">
              <FieldLabel htmlFor="otp-verification">
                Verification code
              </FieldLabel>
              {onResend && (
                <Button
                  variant="outline"
                  size="sm"
                  type="button"
                  disabled={isResending || isVerifying}
                  onClick={handleResend}
                >
                  <RefreshCwIcon />
                  {isResending ? "Sending..." : "Resend Code"}
                </Button>
              )}
            </div>
            <InputOTP
              maxLength={6}
              id="otp-verification"
              value={code}
              onChange={(value) => {
                setCode(value)
                if (error) setError(null)
              }}
              disabled={isVerifying}
              required
            >
              <InputOTPGroup className="*:data-[slot=input-otp-slot]:h-12 *:data-[slot=input-otp-slot]:w-11 *:data-[slot=input-otp-slot]:text-xl">
                <InputOTPSlot index={0} />
                <InputOTPSlot index={1} />
                <InputOTPSlot index={2} />
              </InputOTPGroup>
              <InputOTPSeparator className="mx-2" />
              <InputOTPGroup className="*:data-[slot=input-otp-slot]:h-12 *:data-[slot=input-otp-slot]:w-11 *:data-[slot=input-otp-slot]:text-xl">
                <InputOTPSlot index={3} />
                <InputOTPSlot index={4} />
                <InputOTPSlot index={5} />
              </InputOTPGroup>
            </InputOTP>
            {error && <FieldError>{error}</FieldError>}
            <FieldDescription>
              <Link href="/support">
                I no longer have access to this email address.
              </Link>
            </FieldDescription>
          </Field>
        </CardContent>
        <CardFooter>
          <Field>
            <Button type="submit" className="w-full" disabled={isVerifying || code.length !== 6}>
              {isVerifying ? "Verifying..." : "Verify"}
            </Button>
            <div className="text-muted-foreground text-sm">
              Having trouble signing in?{" "}
              <Link
                href="/support"
                className="hover:text-primary underline underline-offset-4 transition-colors"
              >
                Contact support
              </Link>
            </div>
          </Field>
        </CardFooter>
      </Card>
    </form>
  )
}
