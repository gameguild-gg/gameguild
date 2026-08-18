import { ResetPasswordForm } from '@/components/reset-password-form';
import { completePasswordResetAction } from '@/lib/auth/password-actions';
import { AuthErrorNotice } from '@/components/auth/auth-error-notice';

interface Props {
  searchParams?: Promise<{ token?: string; error?: string }>;
}

export default async function ResetPasswordPage({ searchParams }: Props) {
  const query = await searchParams;
  return (
    <>
      <AuthErrorNotice errorCode={query?.error} />
      <ResetPasswordForm token={query?.token ?? ''} onReset={completePasswordResetAction} />
    </>
  );
}
