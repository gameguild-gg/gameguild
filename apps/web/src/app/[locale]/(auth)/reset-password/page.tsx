import { ResetPasswordForm } from '@/components/reset-password-form';
import { completePasswordResetAction } from '@/lib/auth/password-actions';

interface Props {
  searchParams?: Promise<{ token?: string }>;
}

export default async function ResetPasswordPage({ searchParams }: Props) {
  const query = await searchParams;
  return <ResetPasswordForm token={query?.token ?? ''} onReset={completePasswordResetAction} />;
}
