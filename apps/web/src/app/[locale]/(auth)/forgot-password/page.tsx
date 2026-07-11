import { ForgotPasswordForm } from '@/components/forgot-password-form';
import { requestPasswordResetAction } from '../actions';

interface Props {
  searchParams?: Promise<{ email?: string }>;
}

export default async function ForgotPasswordPage({ searchParams }: Props) {
  const query = await searchParams;
  return <ForgotPasswordForm initialEmail={query?.email ?? ''} onRequestReset={requestPasswordResetAction} />;
}
