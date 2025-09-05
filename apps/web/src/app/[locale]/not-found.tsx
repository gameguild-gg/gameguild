import { NotFound } from '@/components/common/errors/not-found';
import { GitHubIssueProvider } from '@/components/providers/github-issue-provider';
import React from 'react';

interface Params {
  params: { locale: string };
}

export default function Page({ params }: Params): React.JSX.Element {
  const { locale } = params;
  return (
    <html lang={locale}>
      <body>
        <GitHubIssueProvider />
        <NotFound />
      </body>
    </html>
  );
}
