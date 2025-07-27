'use server';

import { unstable_cache } from 'next/cache';
import { Octokit } from 'octokit';
import type { RestEndpointMethodTypes } from '@octokit/rest';

// GitHub repository constants
const GITHUB_OWNER = 'gameguild-gg';
const GITHUB_REPO = 'gameguild';

// Cache configuration
const CACHE_REVALIDATE_SECONDS = 3600; // 1 hour

// Types - Use GitHub's official types for better maintainability
type Issue = RestEndpointMethodTypes['issues']['listForRepo']['response']['data'][0];
type PullRequest = RestEndpointMethodTypes['pulls']['list']['response']['data'][0];
type Contributor = RestEndpointMethodTypes['repos']['listContributors']['response']['data'][0];
type Repository = RestEndpointMethodTypes['repos']['get']['response']['data'];
// Extract the array type from the contributor stats response and get individual items
type GitHubCommitStatsResponse = RestEndpointMethodTypes['repos']['getContributorsStats']['response']['data'];
type GitHubCommitStat = Extract<GitHubCommitStatsResponse, readonly unknown[]>[0];

// Enhanced contributor type with additional stats
type EnhancedContributor = Contributor & {
  additions?: number;
  deletions?: number;
  total_commits?: number;
};

// Create an Octokit instance
const createOctokit = () => {
  const token = process.env.GITHUB_TOKEN;
  return new Octokit({
    auth: token,
    userAgent: 'GameGuild-Website',
  });
};

const octokit = createOctokit().rest;

/**
 * Fetch contributors from GitHub API with filtering
 */
const fetchGitHubContributors = unstable_cache(
  async (repo: string = GITHUB_REPO): Promise<Contributor[]> => {
    try {
      const { data: contributors } = await octokit.repos.listContributors({
        owner: GITHUB_OWNER,
        repo: repo,
        per_page: 100,
      });

      // Filter out bots and unwanted contributors
      return contributors.filter(
        (contributor) =>
          contributor.login !== 'semantic-release-bot' &&
          contributor.login !== 'dependabot[bot]' &&
          contributor.login !== 'github-actions[bot]' &&
          contributor.login !== 'LMD9977',
      ) as Contributor[];
    } catch (error) {
      console.error('Error fetching GitHub contributors:', error);
      return [];
    }
  },
  ['github-contributors'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-contributors'] },
);

/**
 * Fetch detailed user information from GitHub API
 */
const fetchUserDetails = unstable_cache(
  async (username: string): Promise<Partial<Contributor>> => {
    try {
      const { data: user } = await octokit.users.getByUsername({
        username,
      });

      return user as Partial<Contributor>;
    } catch (error) {
      console.error(`Error fetching user details for ${username}:`, error);
      return {};
    }
  },
  ['github-user-details'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-user'] },
);

/**
 * Fetch commit statistics for contributors
 */
const fetchContributorStats = unstable_cache(
  async (repo: string = GITHUB_REPO): Promise<GitHubCommitStat[]> => {
    try {
      const { data: stats } = await octokit.repos.getContributorsStats({
        owner: GITHUB_OWNER,
        repo: repo,
      });

      // GitHub API sometimes returns 202 (Accepted) while computing stats
      if (!stats || !Array.isArray(stats)) {
        console.warn('Contributor stats not ready or unavailable');
        return [];
      }

      // Ensure we return the properly typed array
      return stats as GitHubCommitStat[];
    } catch (error) {
      console.error('Error fetching contributor stats:', error);
      return [];
    }
  },
  ['github-contributor-stats'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-stats'] },
);

const fetchGitHubRepositoryData = unstable_cache(
  async () => {
    let allIssues: Issue[] = [];
    let allPulls: PullRequest[] = [];
    let page = 1;
    let hasNextPage = true;

    while (hasNextPage) {
      const [issuesResponse, pullsResponse] = await Promise.all([
        octokit.issues.listForRepo({
          owner: GITHUB_OWNER,
          repo: GITHUB_REPO,
          state: 'all',
          per_page: 100,
          page: page,
        }),
        octokit.pulls.list({
          owner: GITHUB_OWNER,
          repo: GITHUB_REPO,
          state: 'all',
          per_page: 100,
          page: page,
        }),
      ]);

      // Process issues with review data for pull requests
      const issuesWithReviews = await Promise.all(
        issuesResponse.data.map(async (issue) => {
          if (issue.pull_request) {
            const [requestedReviewers, reviews] = await Promise.all([
              octokit.pulls.listRequestedReviewers({
                owner: GITHUB_OWNER,
                repo: GITHUB_REPO,
                pull_number: issue.number,
              }),
              octokit.pulls.listReviews({
                owner: GITHUB_OWNER,
                repo: GITHUB_REPO,
                pull_number: issue.number,
              }),
            ]);

            return {
              ...issue,
              requestedReviewers: requestedReviewers.data,
              reviews: reviews.data,
            };
          }
          return issue;
        }),
      );

      // Process pulls with additional data
      const pullsWithDetails = await Promise.all(
        pullsResponse.data.map(async (pull) => {
          const [commits, files] = await Promise.all([
            octokit.pulls.listCommits({
              owner: GITHUB_OWNER,
              repo: GITHUB_REPO,
              pull_number: pull.number,
            }),
            octokit.pulls.listFiles({
              owner: GITHUB_OWNER,
              repo: GITHUB_REPO,
              pull_number: pull.number,
            }),
          ]);

          return {
            ...pull,
            commits: commits.data,
            files: files.data,
          };
        }),
      );

      allIssues = allIssues.concat(issuesWithReviews);
      allPulls = allPulls.concat(pullsWithDetails);

      hasNextPage = Math.max(issuesResponse.data.length, pullsResponse.data.length) === 100;
      page++;
    }

    // Fetch other repo data
    const [releases, branches, contributors, license] = await Promise.all([
      octokit.repos.listReleases({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
        per_page: 50,
      }),
      octokit.repos.listBranches({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
        per_page: 50,
      }),
      octokit.repos.listContributors({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
        per_page: 50,
      }),
      octokit.licenses.getForRepo({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
      }),
    ]);

    return {
      issues: allIssues,
      pulls: allPulls,
      releases: releases.data,
      branches: branches.data,
      contributors: contributors.data,
      license: license.data,
    };
  },
  ['github-repo-data'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-repo'] },
);

/**
 * Get contributors with enhanced data from GitHub API
 */
export async function getContributors(): Promise<EnhancedContributor[]> {
  try {
    // Fetch basic contributor data
    const contributors = await fetchGitHubContributors();

    if (contributors.length === 0) {
      return [];
    }

    // Fetch detailed stats
    const stats = await fetchContributorStats();

    // Enhance contributors with additional data
    const enhancedContributors: EnhancedContributor[] = await Promise.all(
      contributors.map(async (contributor) => {
        // Skip if login is undefined
        if (!contributor.login) {
          return contributor;
        }

        // Get user details
        const userDetails = await fetchUserDetails(contributor.login);

        // Find stats for this contributor
        const contributorStats = stats.find((stat) => stat.author?.login === contributor.login);

        // Calculate total additions and deletions
        let additions = 0;
        let deletions = 0;
        let totalCommits = 0;

        if (contributorStats) {
          totalCommits = contributorStats.total;
          contributorStats.weeks.forEach((week) => {
            additions += week.a || 0;
            deletions += week.d || 0;
          });
        }

        return {
          ...contributor,
          ...userDetails,
          additions,
          deletions,
          total_commits: totalCommits,
        };
      }),
    );

    // Sort by total contributions
    enhancedContributors.sort((a, b) => {
      const scoreA = (a.contributions || 0) + (a.additions || 0) + (a.deletions || 0);
      const scoreB = (b.contributions || 0) + (b.additions || 0) + (b.deletions || 0);
      return scoreB - scoreA;
    });

    return enhancedContributors;
  } catch (error) {
    console.error('Error getting contributors:', error);
    return [];
  }
}

/**
 * Get repository statistics from GitHub API
 */
const getRepositoryStatsCache = unstable_cache(
  async (repo: string): Promise<Repository | null> => {
    try {
      const { data: repoData } = await octokit.repos.get({
        owner: GITHUB_OWNER,
        repo: repo,
      });

      return {
        ...repoData,
        stars: repoData.stargazers_count,
        forks: repoData.forks_count,
        openIssues: repoData.open_issues_count,
        language: repoData.language,
        size: repoData.size,
        createdAt: repoData.created_at,
        updatedAt: repoData.updated_at,
        defaultBranch: repoData.default_branch,
        description: repoData.description,
        homepage: repoData.homepage,
      } as Repository;
    } catch (error) {
      console.error('Error fetching repository stats:', error);
      return null;
    }
  },
  ['github-repo-stats'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-repo-stats'] },
);

export async function getRepositoryStats(repo: string = 'website'): Promise<Repository | null> {
  return getRepositoryStatsCache(repo);
}

export const getGitHubRepositoryData = async () => {
  try {
    const repoData = await fetchGitHubRepositoryData();
    return {
      totalIssues: repoData.issues.length,
      totalPulls: repoData.pulls.length,
      totalReleases: repoData.releases.length,
      totalBranches: repoData.branches.length,
      totalContributors: repoData.contributors.length,
      data: repoData,
    };
  } catch (error) {
    console.error('Error fetching GitHub repo data:', error);
    throw new Error('Failed to fetch GitHub repository data');
  }
};

/**
 * Get license content from GitHub API
 */
export async function getLicenseContent(): Promise<{ content: string; name: string; spdx_id: string } | null> {
  try {
    const { data: license } = await octokit.licenses.getForRepo({
      owner: GITHUB_OWNER,
      repo: GITHUB_REPO,
    });

    // The license content is base64 encoded, so we need to decode it
    const content = Buffer.from(license.content, 'base64').toString('utf8');

    return {
      content,
      name: license.license?.name || 'Unknown License',
      spdx_id: license.license?.spdx_id || 'Unknown',
    };
  } catch (error) {
    console.error('Error fetching license content:', error);
    return null;
  }
}

// Re-export types for convenience
export type { Contributor, EnhancedContributor, GitHubCommitStat, Repository, Issue, PullRequest };
