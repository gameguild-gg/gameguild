'use client';

import { useSearchParams, useRouter } from 'next/navigation';
import { InputOTPForm } from '@/components/input-otp-form';
import { verifyEmailAction, resendVerificationEmailAction } from '../actions';

export function VerifyPageContent() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const email = searchParams.get('email') ?? '';

  if (!email) {
    router.replace('/sign-in');
    return null;
  }

  async function handleVerify(code: string) {
    const result = await verifyEmailAction(code);
    if (!result.success) {
      throw new Error(result.error);
    }
    router.push('/my');
  }

  async function handleResend() {
    const result = await resendVerificationEmailAction(email);
    if (!result.success) {
      throw new Error(result.error);
    }
  }

  return <InputOTPForm email={email} onVerify={handleVerify} onResend={handleResend} />;
}
