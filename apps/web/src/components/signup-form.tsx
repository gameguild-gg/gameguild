"use client"

import { type FormEvent, useState } from "react"
import { Link } from "@/i18n/navigation"
import { useLocale } from "next-intl"
import { useAuth } from "@game-guild/client/react"
import { cn } from "@/lib/utils"
import { Button } from "@game-guild/ui/components/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card"
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@game-guild/ui/components/field"
import { Input } from "@game-guild/ui/components/input"

export function SignupForm({
  className,
  redirectTo = "/my",
  providers,
  ...props
}: React.ComponentProps<"div"> & {
  redirectTo?: string
  providers?: React.ReactNode
}) {
  const { signUp, isLoading, error, clearError } = useAuth()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const locale = useLocale()

  function clearFieldError(field: string) {
    setFieldErrors((prev) => {
      if (!prev[field]) return prev
      const next = { ...prev }
      delete next[field]
      return next
    })
  }

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    clearError()
    setFieldErrors({})

    const formData = new FormData(e.currentTarget)
    const name = (formData.get("name") as string).trim()
    const email = (formData.get("email") as string).trim()
    const password = formData.get("password") as string
    const confirmPassword = formData.get("confirm-password") as string

    const errors: Record<string, string> = {}

    if (!name) errors.name = "Full name is required."
    if (!email) errors.email = "Email is required."
    if (!password) errors.password = "Password is required."
    else if (password.length < 8)
      errors.password = "Password must be at least 8 characters."
    if (!confirmPassword)
      errors["confirm-password"] = "Please confirm your password."
    else if (password && password !== confirmPassword)
      errors["confirm-password"] = "Passwords do not match."

    if (Object.keys(errors).length > 0) {
      setFieldErrors(errors)
      return
    }

    try {
      await signUp({
        username: email.split("@")[0],
        email,
        password,
        firstName: name.split(" ")[0],
        lastName: name.split(" ").slice(1).join(" ") || undefined,
        redirectTo,
      })
    } catch {
      // error state is set by useAuth
    }
  }

  return (
    <div className={cn("flex flex-col gap-6", className)} {...props}>
      <Card className="border-white/10 bg-slate-900/85 text-white shadow-2xl shadow-sky-950/30 backdrop-blur">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">
            <h1>Create your GameGuild account</h1>
          </CardTitle>
          <CardDescription className="text-slate-300">
            Join the community to learn, share projects, and collect useful feedback.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {providers && (
            <>
              <div className="mb-6">{providers}</div>
              <div className="flex w-full items-center gap-3 text-xs text-slate-400">
                <div className="h-px flex-1 bg-white/10" />
                <span>or with email</span>
                <div className="h-px flex-1 bg-white/10" />
              </div>
            </>
          )}
          <form onSubmit={handleSubmit} noValidate>
            <FieldGroup>
              <Field>
                <FieldLabel htmlFor="name">Full Name</FieldLabel>
                <Input
                  id="name"
                  name="name"
                  type="text"
                  placeholder="John Doe"
                  autoComplete="name"
                  required
                  disabled={isLoading}
                  className="border-white/10 bg-white/5 text-white placeholder:text-slate-500"
                  aria-invalid={!!fieldErrors.name}
                  onChange={() => clearFieldError("name")}
                />
                {fieldErrors.name && (
                  <FieldError>{fieldErrors.name}</FieldError>
                )}
              </Field>
              <Field>
                <FieldLabel htmlFor="email">Email</FieldLabel>
                <Input
                  id="email"
                  name="email"
                  type="email"
                  placeholder="m@example.com"
                  autoComplete="email"
                  required
                  disabled={isLoading}
                  className="border-white/10 bg-white/5 text-white placeholder:text-slate-500"
                  aria-invalid={!!fieldErrors.email}
                  onChange={() => clearFieldError("email")}
                />
                {fieldErrors.email && (
                  <FieldError>{fieldErrors.email}</FieldError>
                )}
              </Field>
              <Field>
                <Field className="grid grid-cols-2 gap-4">
                  <Field>
                    <FieldLabel htmlFor="password">Password</FieldLabel>
                    <Input
                      id="password"
                      name="password"
                      type="password"
                      autoComplete="new-password"
                      required
                      disabled={isLoading}
                      className="border-white/10 bg-white/5 text-white"
                      aria-invalid={!!fieldErrors.password}
                      onChange={() => clearFieldError("password")}
                    />
                    {fieldErrors.password && (
                      <FieldError>{fieldErrors.password}</FieldError>
                    )}
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="confirm-password">
                      Confirm Password
                    </FieldLabel>
                    <Input
                      id="confirm-password"
                      name="confirm-password"
                      type="password"
                      autoComplete="new-password"
                      required
                      disabled={isLoading}
                      className="border-white/10 bg-white/5 text-white"
                      aria-invalid={!!fieldErrors["confirm-password"]}
                      onChange={() => clearFieldError("confirm-password")}
                    />
                    {fieldErrors["confirm-password"] && (
                      <FieldError>{fieldErrors["confirm-password"]}</FieldError>
                    )}
                  </Field>
                </Field>
                <FieldDescription className="text-slate-400">
                  Must be at least 8 characters long.
                </FieldDescription>
              </Field>
              {error && <FieldError>{error.message}</FieldError>}
              <Field>
                <Button type="submit" disabled={isLoading}>
                  {isLoading ? "Creating account..." : "Create Account"}
                </Button>
                <FieldDescription className="text-center text-slate-300">
                  Already have an account?{" "}
                  <Link
                    href={redirectTo ? `/sign-in?redirectTo=${encodeURIComponent(redirectTo)}` : "/sign-in"}
                    locale={locale}
                    className="text-sky-200 underline-offset-4 hover:underline"
                  >
                    Sign in
                  </Link>
                </FieldDescription>
              </Field>
            </FieldGroup>
          </form>
        </CardContent>
      </Card>
      <FieldDescription className="px-6 text-center text-slate-400">
        By clicking continue, you agree to our{" "}
        <Link href="/terms-of-service" locale={locale} className="text-sky-200 underline-offset-4 hover:underline">Terms of Service</Link> and{" "}
        <Link href="/polices/privacy" locale={locale} className="text-sky-200 underline-offset-4 hover:underline">Privacy Policy</Link>.
      </FieldDescription>
    </div>
  )
}
