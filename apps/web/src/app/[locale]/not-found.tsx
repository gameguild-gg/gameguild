import { NotFound } from '@/components/common/errors/not-found';
import { GitHubIssueProvider } from '@/components/providers/github-issue-provider';
import React from 'react';

export default function Page(): React.JSX.Element {
  return (
    <>
      <GitHubIssueProvider />
      <NotFound />
    </>
  );
}
