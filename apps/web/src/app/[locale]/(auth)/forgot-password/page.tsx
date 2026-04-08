import { ForgotPasswordForm } from '@/components/forgot-password-form';
import { requestPasswordResetAction } from '../actions';

export default function ForgotPasswordPage() {
  return <ForgotPasswordForm onRequestReset={requestPasswordResetAction} />;
}
