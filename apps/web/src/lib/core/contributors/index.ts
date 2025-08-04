// Re-export GitHub functions and types from the consolidated GitHub actions.
export { getContributors, getRepositoryStats, getGitHubRepositoryData } from '@/lib/integrations/github';
export type { Contributor, EnhancedContributor, GitHubCommitStat, Repository, Issue, PullRequest } from '@/lib/integrations/github';
