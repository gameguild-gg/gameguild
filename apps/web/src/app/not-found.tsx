import React from 'react';
import { NotFound } from '@/components/common/errors/not-found';
import { GitHubIssueProvider } from '@/components/providers/github-issue-provider';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <>
      <GitHubIssueProvider />
      <NotFound />
    </>
  );
}
