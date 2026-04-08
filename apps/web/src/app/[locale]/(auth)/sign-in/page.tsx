import React from 'react';
import { SignInForm } from '@/components/sign-in-form';

export default async function Page({ params }: PageProps<'/[locale]/sign-in'>): Promise<React.JSX.Element> {
  return <SignInForm />;
}
