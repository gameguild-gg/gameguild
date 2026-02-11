import React from 'react';
import { SignupForm } from "@/components/signup-form"

export default async function Page({ params }: PageProps<'/[locale]/sign-up'>): Promise<React.JSX.Element> {
  return <SignupForm />;
}
