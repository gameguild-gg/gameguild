import { NotFound } from '@/components/common/errors/not-found';
import { GitHubIssueProvider } from '@/components/providers/github-issue-provider';
import React from 'react';


export default async function Page(): Promise<React.JSX.Element> {
  return (
    <html lang="en">
      <body>
        <GitHubIssueProvider />
        <NotFound />
      </body>
    </html>
  );
}
