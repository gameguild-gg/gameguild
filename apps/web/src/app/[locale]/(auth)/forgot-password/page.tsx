import type { PageProps } from 'next';
import { ForgotPasswordForm } from '@/components/forgot-password-form';

export default function ForgotPasswordPage(
  _props: PageProps<'/[locale]/forgot-password'>
) {
  return <ForgotPasswordForm />;
}
