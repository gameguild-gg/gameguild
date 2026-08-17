import React from 'react';
import { VerifyPageContent } from '@/components/auth/verify-page-content'
import { AuthErrorNotice } from '@/components/auth/auth-error-notice';

export default async function Page({
  params,
  searchParams,
}: PageProps<'/[locale]/verify'>): Promise<React.JSX.Element> {
  const [, query] = await Promise.all([params, searchParams]);
  const errorCode = typeof query?.error === 'string' ? query.error : undefined;

  return (
    <>
      <AuthErrorNotice errorCode={errorCode} />
      <VerifyPageContent />
    </>
  );
}
