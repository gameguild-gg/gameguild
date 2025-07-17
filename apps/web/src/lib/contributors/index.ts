import { Octokit } from '@octokit/rest';
import type { Contributor, GitHubCommitStats, GitStats } from './types';

// Re-export types for convenience
export type { Contributor, GitHubCommitStats, GitStats };

const REPO_OWNER = 'gameguild-gg';
const REPO_NAME = 'website';

// Create Octokit instance
const createOctokit = () => {
  const token = process.env.GITHUB_TOKEN;
  return new Octokit({
    auth: token,
    userAgent: 'GameGuild-Website',
  });
};

// Simple cache for GitHub API responses
const cache = new Map<string, { data: unknown; timestamp: number }>();
const CACHE_DURATION = 3600000; // 1 hour in milliseconds

function getCachedData<T>(key: string): T | null {
  const cached = cache.get(key);
  if (cached && Date.now() - cached.timestamp < CACHE_DURATION) {
    return cached.data as T;
  }
  return null;
}

function setCachedData<T>(key: string, data: T): void {
  cache.set(key, { data, timestamp: Date.now() });
}

/**
 * Fetch contributors from GitHub API using Octokit with caching
 */
async function fetchGitHubContributors(): Promise<Contributor[]> {
  const cacheKey = 'contributors';
  const cached = getCachedData<Contributor[]>(cacheKey);
  if (cached) {
    return cached;
  }

  try {
    const octokit = createOctokit();
    
    const { data: contributors } = await octokit.rest.repos.listContributors({
      owner: REPO_OWNER,
      repo: REPO_NAME,
      per_page: 100,
    });

    // Filter out bots and unwanted contributors
    const filtered = contributors.filter(
      (contributor) =>
        contributor.login !== 'semantic-release-bot' &&
        contributor.login !== 'dependabot[bot]' &&
        contributor.login !== 'github-actions[bot]' &&
        contributor.login !== 'LMD9977',
    ) as Contributor[];

    setCachedData(cacheKey, filtered);
    return filtered;
  } catch (error) {
    console.error('Error fetching GitHub contributors:', error);
    return [];
  }
}

/**
 * Fetch detailed user information from GitHub API using Octokit with caching
 */
async function fetchUserDetails(username: string): Promise<Partial<Contributor>> {
  const cacheKey = `user-${username}`;
  const cached = getCachedData<Partial<Contributor>>(cacheKey);
  if (cached) {
    return cached;
  }

  try {
    const octokit = createOctokit();
    
    const { data: user } = await octokit.rest.users.getByUsername({
      username,
    });

    const userData = user as Partial<Contributor>;
    setCachedData(cacheKey, userData);
    return userData;
  } catch (error) {
    console.error(`Error fetching user details for ${username}:`, error);
    return {};
  }
}

/**
 * Fetch commit statistics for contributors using Octokit with caching
 */
async function fetchContributorStats(): Promise<GitHubCommitStats[]> {
  const cacheKey = 'contributor-stats';
  const cached = getCachedData<GitHubCommitStats[]>(cacheKey);
  if (cached) {
    return cached;
  }

  try {
    const octokit = createOctokit();
    
    const { data: stats } = await octokit.rest.repos.getContributorsStats({
      owner: REPO_OWNER,
      repo: REPO_NAME,
    });

    // GitHub API sometimes returns 202 (Accepted) while computing stats
    // In that case, data might be empty or null
    if (!stats || !Array.isArray(stats)) {
      console.warn('Contributor stats not ready or unavailable');
      return [];
    }

    const statsData = stats as GitHubCommitStats[];
    setCachedData(cacheKey, statsData);
    return statsData;
  } catch (error) {
    console.error('Error fetching contributor stats:', error);
    return [];
  }
}

/**
 * Get contributors with enhanced data from GitHub API
 */
export async function getContributors(): Promise<Contributor[]> {
  try {
    // Fetch basic contributor data
    const contributors = await fetchGitHubContributors();

    if (contributors.length === 0) {
      return [];
    }

    // Fetch detailed stats
    const stats = await fetchContributorStats();

    // Enhance contributors with additional data
    const enhancedContributors = await Promise.all(
      contributors.map(async (contributor) => {
        // Get user details
        const userDetails = await fetchUserDetails(contributor.login);

        // Find stats for this contributor (with proper type checking)
        const contributorStats = Array.isArray(stats) ? stats.find((stat) => stat.author.login === contributor.login) : undefined;

        // Calculate total additions and deletions
        let additions = 0;
        let deletions = 0;
        let totalCommits = 0;

        if (contributorStats) {
          totalCommits = contributorStats.total;
          contributorStats.weeks.forEach((week) => {
            additions += week.a;
            deletions += week.d;
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

    // Sort by total contributions (commits + additions + deletions)
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
 * Get repository statistics from GitHub API using Octokit
 */
export async function getRepositoryStats() {
  try {
    const octokit = createOctokit();
    
    const { data: repo } = await octokit.rest.repos.get({
      owner: REPO_OWNER,
      repo: REPO_NAME,
    });

    return {
      stars: repo.stargazers_count,
      forks: repo.forks_count,
      openIssues: repo.open_issues_count,
      language: repo.language,
      size: repo.size,
      createdAt: repo.created_at,
      updatedAt: repo.updated_at,
      defaultBranch: repo.default_branch,
      description: repo.description,
      homepage: repo.homepage,
    };
  } catch (error) {
    console.error('Error fetching repository stats:', error);
    return null;
  }
}
