import { ForgotPasswordForm } from '@/components/forgot-password-form';
import { requestPasswordResetAction } from '@/lib/auth/password-actions';
import { AuthErrorNotice } from '@/components/auth/auth-error-notice';

interface Props {
  searchParams?: Promise<{ email?: string; error?: string }>;
}

export default async function ForgotPasswordPage({ searchParams }: Props) {
  const query = await searchParams;
  return (
    <>
      <AuthErrorNotice errorCode={query?.error} />
      <ForgotPasswordForm initialEmail={query?.email ?? ''} onRequestReset={requestPasswordResetAction} />
    </>
  );
}
