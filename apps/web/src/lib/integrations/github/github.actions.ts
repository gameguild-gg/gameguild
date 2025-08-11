'use server';

import type { RestEndpointMethodTypes } from '@octokit/rest';
import { unstable_cache } from 'next/cache';
import { Octokit } from 'octokit';

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
      return contributors.filter((contributor) => contributor.login !== 'semantic-release-bot' && contributor.login !== 'dependabot[bot]' && contributor.login !== 'github-actions[bot]' && contributor.login !== 'LMD9977') as Contributor[];
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

// Cache repository basic info (small data)
const getCachedRepoInfo = unstable_cache(
  async () => {
    try {
      const { data: repo } = await octokit.repos.get({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
      });

      return {
        name: repo.name,
        description: repo.description,
        stars: repo.stargazers_count,
        forks: repo.forks_count,
        watchers: repo.watchers_count,
        openIssues: repo.open_issues_count,
        language: repo.language,
        createdAt: repo.created_at,
        updatedAt: repo.updated_at,
        defaultBranch: repo.default_branch,
        homepage: repo.homepage,
      };
    } catch (error) {
      console.error('Error fetching repo info:', error);
      throw error;
    }
  },
  ['repo-info'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-repo-info'] },
);

// Cache contributors in batches (paginated)
const getCachedContributorsBatch = unstable_cache(
  async (page: number = 1, perPage: number = 100) => {
    try {
      const { data: contributors } = await octokit.repos.listContributors({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
        page: page,
        per_page: perPage,
      });

      // Return only essential data to minimize size
      return contributors.map((contributor: Contributor) => ({
        login: contributor.login,
        id: contributor.id,
        avatar_url: contributor.avatar_url,
        contributions: contributor.contributions,
        type: contributor.type,
      }));
    } catch (error) {
      console.error(`Error fetching contributors page ${page}:`, error);
      return [];
    }
  },
  ['contributors-batch'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-contributors-batch'] },
);

// Cache releases info (small data)
const getCachedReleases = unstable_cache(
  async () => {
    try {
      const { data: releases } = await octokit.repos.listReleases({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
        per_page: 10,
      });

      return releases.map((release: RestEndpointMethodTypes['repos']['listReleases']['response']['data'][0]) => ({
        id: release.id,
        name: release.name,
        tag_name: release.tag_name,
        published_at: release.published_at,
        download_count: release.assets?.reduce((sum: number, asset: RestEndpointMethodTypes['repos']['listReleases']['response']['data'][0]['assets'][0]) => sum + asset.download_count, 0) || 0,
      }));
    } catch (error) {
      console.error('Error fetching releases:', error);
      return [];
    }
  },
  ['releases'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-releases'] },
);

// Cache languages (small data)
const getCachedLanguages = unstable_cache(
  async () => {
    try {
      const { data: languages } = await octokit.repos.listLanguages({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
      });

      return languages;
    } catch (error) {
      console.error('Error fetching languages:', error);
      return {};
    }
  },
  ['languages'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-languages'] },
);

// Cache issues count (small data)
const getCachedIssuesCount = unstable_cache(
  async () => {
    try {
      // Get total count from response headers if available
      const response = await octokit.issues.listForRepo({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
        state: 'all',
        per_page: 100,
      });

      return response.data.length;
    } catch (error) {
      console.error('Error fetching issues count:', error);
      return 0;
    }
  },
  ['issues-count'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-issues'] },
);

// Cache pulls count (small data)
const getCachedPullsCount = unstable_cache(
  async () => {
    try {
      const response = await octokit.pulls.list({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
        state: 'all',
        per_page: 100,
      });

      return response.data.length;
    } catch (error) {
      console.error('Error fetching pulls count:', error);
      return 0;
    }
  },
  ['pulls-count'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-pulls'] },
);

// Cache branches count (small data)
const getCachedBranchesCount = unstable_cache(
  async () => {
    try {
      const { data: branches } = await octokit.repos.listBranches({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
        per_page: 100,
      });

      return branches.length;
    } catch (error) {
      console.error('Error fetching branches count:', error);
      return 0;
    }
  },
  ['branches-count'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-branches'] },
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

// Main function that aggregates all cached parts
export async function getCachedContributors() {
  try {
    // Get all the cached parts in parallel
    const [repoInfo, releases, languages, issuesCount, pullsCount, branchesCount] = await Promise.all([
      getCachedRepoInfo(),
      getCachedReleases(),
      getCachedLanguages(),
      getCachedIssuesCount(),
      getCachedPullsCount(),
      getCachedBranchesCount(),
    ]);

    // Get contributors in batches
    const allContributors = [];
    let page = 1;
    let hasMore = true;

    while (hasMore && page <= 10) {
      // Limit to first 1000 contributors (10 pages * 100)
      try {
        const batch = await getCachedContributorsBatch(page, 100);

        if (batch.length === 0) {
          hasMore = false;
        } else {
          allContributors.push(...batch);
          page++;

          // If we got less than 100, we've reached the end
          if (batch.length < 100) {
            hasMore = false;
          }
        }
      } catch (error) {
        console.error(`Error fetching contributors page ${page}:`, error);
        hasMore = false;
      }
    }

    // Aggregate the data
    return {
      repository: repoInfo,
      contributors: allContributors,
      stats: {
        totalContributors: allContributors.length,
        totalIssues: issuesCount,
        totalPulls: pullsCount,
        totalBranches: branchesCount,
        totalReleases: releases.length,
      },
      releases: releases,
      languages: languages,
      meta: {
        lastUpdated: new Date().toISOString(),
        contributorsLimit: 1000, // We're limiting to first 1000 contributors
        actualContributorsCount: allContributors.length,
      },
    };
  } catch (error) {
    console.error('Error in getCachedContributors:', error);
    throw error;
  }
}

export const getGitHubRepositoryData = async () => {
  try {
    const repoData = await getCachedContributors();
    return {
      totalIssues: repoData.stats.totalIssues,
      totalPulls: repoData.stats.totalPulls,
      totalReleases: repoData.stats.totalReleases,
      totalBranches: repoData.stats.totalBranches,
      totalContributors: repoData.stats.totalContributors,
      data: {
        issues: [], // Empty array to avoid cache issues
        pulls: [], // Empty array to avoid cache issues
        releases: repoData.releases,
        branches: [], // Empty array to avoid cache issues
        contributors: repoData.contributors,
        license: null, // Simplified for now
      },
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
