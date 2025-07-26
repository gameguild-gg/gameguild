import React from 'react';
import { SignInForm } from '@/components/auth';

export default async function SignInPage(): Promise<React.JSX.Element> {
  return <SignInForm />;
}
