'use client';

import { initGitHubIssueHandler } from '@/lib/github-issue-handler';
import { useEffect } from 'react';

export function GitHubIssueProvider() {
  useEffect(() => {
    // Initialize the global GitHub issue handler
    initGitHubIssueHandler();
  }, []);

  return null; // This component doesn't render anything
}