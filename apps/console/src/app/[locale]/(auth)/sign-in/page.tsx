import React from 'react';
import { SignInForm } from '@gameguild/auth/auth/sign-in-form';

export default async function Page(): Promise<React.JSX.Element> {
  return <SignInForm />;
}
