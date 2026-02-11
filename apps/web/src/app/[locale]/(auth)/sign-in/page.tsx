import React from 'react';
import { LoginForm } from "@/components/login-form"

export default async function Page({ params }: PageProps<'/[locale]/sign-in'>): Promise<React.JSX.Element> {
  return <LoginForm />;
}
