import React from 'react';
import { VerifyPageContent } from "./verify-page-content"

export default async function Page({ params }: PageProps<'/[locale]/verify'>): Promise<React.JSX.Element> {
  return <VerifyPageContent />;
}
