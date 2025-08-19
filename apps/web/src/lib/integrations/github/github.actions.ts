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

// Create an Octokit instance with timeout configuration
const createOctokit = () => {
  const token = process.env.GITHUB_TOKEN;
  if (!token) {
    console.warn('GITHUB_TOKEN environment variable not set. GitHub API requests may be rate limited.');
  }
  return new Octokit({
    auth: token,
    userAgent: 'GameGuild-Website',
    request: {
      timeout: 30000, // 30 seconds timeout
      retries: 3,
      retryAfter: 2,
    },
  });
};

const octokit = createOctokit().rest;

/**
 * Removed hardcoded fallback data - errors should be visible, not hidden
 */

/**
 * Fetch contributors from GitHub API with filtering
 */
const fetchGitHubContributors = unstable_cache(
  async (repo: string = GITHUB_REPO): Promise<Contributor[]> => {
    try {
      console.log(`Fetching contributors from ${GITHUB_OWNER}/${repo}...`);
      
      const result = await retryWithBackoff(async () => {
        const response = await octokit.repos.listContributors({
          owner: GITHUB_OWNER,
          repo: repo,
          per_page: 100,
        });
        return response;
      });

      const { data: contributors } = result;
      
      if (!contributors || !Array.isArray(contributors)) {
        console.warn('No contributors data received from GitHub API');
        return [];
      }
      
      console.log(`GitHub API returned ${contributors.length} contributors`);

      // Filter out bots and unwanted contributors
      const filteredContributors = contributors.filter((contributor) => contributor.login !== 'semantic-release-bot' && contributor.login !== 'dependabot[bot]' && contributor.login !== 'github-actions[bot]' && contributor.login !== 'LMD9977') as Contributor[];
      console.log(`Filtered to ${filteredContributors.length} user contributors`);
      return filteredContributors;
    } catch (error) {
      console.error('Error fetching GitHub contributors:', error);
      
      // Enhanced error logging
      if (error && typeof error === 'object') {
        const errorObj = error as any;
        if (errorObj.status) {
          console.error(`GitHub API returned status: ${errorObj.status}`);
        }
        if (errorObj.message) {
          console.error(`Error message: ${errorObj.message}`);
        }
        if (errorObj.documentation_url) {
          console.error(`Documentation: ${errorObj.documentation_url}`);
        }
      }
      
      return [];
    }
  },
  ['github-contributors'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-contributors'] },
);

/**
 * Retry function with exponential backoff
 */
async function retryWithBackoff<T>(
  fn: () => Promise<T>,
  maxRetries: number = 3,
  baseDelay: number = 1000
): Promise<T> {
  let lastError: Error;
  
  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    try {
      return await fn();
    } catch (error) {
      lastError = error as Error;
      
      // Check for connection timeout errors
      const errorObj = error as any;
      const isTimeoutError = error && typeof error === 'object' && 
        ('code' in errorObj && errorObj.code === 'UND_ERR_CONNECT_TIMEOUT') ||
        ('message' in errorObj && typeof errorObj.message === 'string' && errorObj.message.includes('Connect Timeout Error'));
      
      // Don't retry on certain error types (but do retry timeouts)
      if (error && typeof error === 'object' && 'status' in errorObj && !isTimeoutError) {
        const status = (error as any).status;
        if (status === 401 || status === 403 || status === 404) {
          throw error; // Don't retry auth or not found errors
        }
      }
      
      if (attempt === maxRetries) {
        console.error(`All ${maxRetries + 1} attempts failed. Last error:`, lastError);
        throw lastError;
      }
      
      // Longer delays for timeout errors
      const timeoutMultiplier = isTimeoutError ? 3 : 1;
      const delay = baseDelay * Math.pow(2, attempt) * timeoutMultiplier + Math.random() * 1000;
      
      console.log(`Attempt ${attempt + 1} failed${isTimeoutError ? ' (timeout)' : ''}, retrying in ${Math.round(delay)}ms...`);
      await new Promise(resolve => setTimeout(resolve, delay));
    }
  }
  
  throw lastError!;
}

/**
 * Fetch detailed user information from GitHub API
 */
const fetchUserDetails = unstable_cache(
  async (username: string): Promise<Partial<Contributor>> => {
    try {
      const result = await retryWithBackoff(async () => {
        const { data: user } = await octokit.users.getByUsername({
          username,
        });
        return user as Partial<Contributor>;
      });
      
      return result;
    } catch (error) {
      console.error(`Error fetching user details for ${username}:`, error);
      
      // Return basic info if we can't fetch details
      return {
        login: username,
        avatar_url: `https://github.com/${username}.png`,
      };
    }
  },
  ['github-user-details'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-user'] },
);

/**
 * Fetch commit statistics for contributors with proper 202 handling
 */
const fetchContributorStats = unstable_cache(
  async (repo: string = GITHUB_REPO): Promise<GitHubCommitStat[]> => {
    try {
      console.log(`Fetching contributor stats for ${GITHUB_OWNER}/${repo}...`);
      
      const result = await retryWithBackoff(async () => {
        const response = await octokit.repos.getContributorsStats({
          owner: GITHUB_OWNER,
          repo: repo,
        });

        // GitHub API returns 202 (Accepted) while computing stats
        if (response.status === 202) {
          console.log('GitHub is computing contributor stats, waiting...');
          throw new Error('Stats being computed, retry needed');
        }

        return response;
      }, 5, 2000); // More retries and longer delay for stats computation

      const { data: stats } = result;

      // Validate the response data
      if (!stats || !Array.isArray(stats)) {
        console.warn('Contributor stats not ready or unavailable after retries');
        return [];
      }

      console.log(`Successfully fetched stats for ${stats.length} contributors`);
      
      // Ensure we return the properly typed array
      return stats as GitHubCommitStat[];
    } catch (error) {
      console.error('GitHub API Error - Contributor Stats:', error);
      
      // Log detailed error information for debugging
      if (error && typeof error === 'object') {
        const errorObj = error as any;
        console.error('Error details:', {
          status: errorObj.status,
          message: errorObj.message,
          code: errorObj.code,
          name: errorObj.name
        });
      }
      
      // Re-throw the error instead of hiding it with fallback data
      throw new Error(`Failed to fetch contributor statistics: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  },
  ['github-contributor-stats'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-stats'] },
);

// Cache repository basic info (small data)
const getCachedRepoInfo = unstable_cache(
  async () => {
    try {
      const result = await retryWithBackoff(async () => {
        const { data: repo } = await octokit.repos.get({
          owner: GITHUB_OWNER,
          repo: GITHUB_REPO,
        });
        return repo;
      }, 5, 2000);

      return {
        name: result.name,
        description: result.description,
        stars: result.stargazers_count,
        forks: result.forks_count,
        watchers: result.watchers_count,
        openIssues: result.open_issues_count,
        language: result.language,
        createdAt: result.created_at,
        updatedAt: result.updated_at,
        defaultBranch: result.default_branch,
        homepage: result.homepage,
      };
    } catch (error) {
      console.error('GitHub API Error - Repository Info:', error);
      
      // Log detailed error information for debugging
      if (error && typeof error === 'object') {
        const errorObj = error as any;
        console.error('Error details:', {
          status: errorObj.status,
          message: errorObj.message,
          code: errorObj.code,
          name: errorObj.name
        });
      }
      
      // Re-throw the error instead of hiding it with fallback data
      throw new Error(`Failed to fetch repository information: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  },
  ['repo-info'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-repo-info'] },
);

// Cache contributors in batches (paginated)
const getCachedContributorsBatch = unstable_cache(
  async (page: number = 1, perPage: number = 100) => {
    try {
      const contributors = await retryWithBackoff(async () => {
        const { data: contributors } = await octokit.repos.listContributors({
          owner: GITHUB_OWNER,
          repo: GITHUB_REPO,
          page: page,
          per_page: perPage,
        });
        return contributors;
      }, 5, 2000);

      // Filter out bots and unwanted contributors (same as fetchGitHubContributors)
      const filteredContributors = contributors.filter((contributor) => 
        contributor.login !== 'semantic-release-bot' && 
        contributor.login !== 'dependabot[bot]' && 
        contributor.login !== 'github-actions[bot]' && 
        contributor.login !== 'LMD9977'
      );

      // Return only essential data to minimize size
      return filteredContributors.map((contributor: Contributor) => ({
        login: contributor.login,
        id: contributor.id,
        avatar_url: contributor.avatar_url,
        contributions: contributor.contributions,
        type: contributor.type,
      }));
    } catch (error) {
      console.error(`GitHub API Error - Contributors Page ${page}:`, error);
      
      // Log detailed error information for debugging
      if (error && typeof error === 'object') {
        const errorObj = error as any;
        console.error('Error details:', {
          status: errorObj.status,
          message: errorObj.message,
          code: errorObj.code,
          name: errorObj.name
        });
      }
      
      // Re-throw the error instead of hiding it with fallback data
      throw new Error(`Failed to fetch contributors page ${page}: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  },
  ['contributors-batch'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-contributors-batch'] },
);

// Cache releases info (with pagination to get all releases)
const getCachedReleases = unstable_cache(
  async () => {
    try {
      const allReleases = await retryWithBackoff(async () => {
        let releases: RestEndpointMethodTypes['repos']['listReleases']['response']['data'] = [];
        let page = 1;
        let hasMore = true;

        while (hasMore && page <= 50) { // Limit to 5000 releases max (50 pages * 100)
          const response = await octokit.repos.listReleases({
            owner: GITHUB_OWNER,
            repo: GITHUB_REPO,
            per_page: 100,
            page: page,
          });

          if (response.data.length === 0) {
            hasMore = false;
          } else {
            releases = releases.concat(response.data);
            page++;
            
            if (response.data.length < 100) {
              hasMore = false;
            }
          }
        }
        return releases;
      }, 5, 2000);

      return allReleases.map((release: RestEndpointMethodTypes['repos']['listReleases']['response']['data'][0]) => ({
        id: release.id,
        name: release.name,
        tag_name: release.tag_name,
        published_at: release.published_at,
        download_count: release.assets?.reduce((sum: number, asset: RestEndpointMethodTypes['repos']['listReleases']['response']['data'][0]['assets'][0]) => sum + asset.download_count, 0) || 0,
      }));
    } catch (error) {
      console.error('GitHub API Error - Releases:', error);
      
      // Log detailed error information for debugging
      if (error && typeof error === 'object') {
        const errorObj = error as any;
        console.error('Error details:', {
          status: errorObj.status,
          message: errorObj.message,
          code: errorObj.code,
          name: errorObj.name
        });
      }
      
      // Re-throw the error instead of hiding it with fallback data
      throw new Error(`Failed to fetch releases: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  },
  ['releases'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-releases'] },
);

// Cache languages (small data)
const getCachedLanguages = unstable_cache(
  async () => {
    try {
      const languages = await retryWithBackoff(async () => {
        const { data: languages } = await octokit.repos.listLanguages({
          owner: GITHUB_OWNER,
          repo: GITHUB_REPO,
        });
        return languages;
      }, 5, 2000);

      return languages;
    } catch (error) {
      console.error('GitHub API Error - Languages:', error);
      
      // Log detailed error information for debugging
      if (error && typeof error === 'object') {
        const errorObj = error as any;
        console.error('Error details:', {
          status: errorObj.status,
          message: errorObj.message,
          code: errorObj.code,
          name: errorObj.name
        });
      }
      
      // Re-throw the error instead of hiding it with fallback data
      throw new Error(`Failed to fetch languages: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  },
  ['languages'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-languages'] },
);

// Cache issues data (with actual issue objects)
const getCachedIssues = unstable_cache(
  async () => {
    try {
      const allIssues = await retryWithBackoff(async () => {
        let issues: Issue[] = [];
        let page = 1;
        let hasMore = true;

        while (hasMore && page <= 10) { // Limit to 1000 issues max
          const response = await octokit.issues.listForRepo({
            owner: GITHUB_OWNER,
            repo: GITHUB_REPO,
            state: 'all',
            per_page: 100,
            page: page,
          });

          if (response.data.length === 0) {
            hasMore = false;
          } else {
            issues = issues.concat(response.data);
            page++;
            
            if (response.data.length < 100) {
              hasMore = false;
            }
          }
        }
        return issues;
      }, 5, 2000);

      return allIssues;
    } catch (error) {
      console.error('GitHub API Error - Issues:', error);
      
      // Log detailed error information for debugging
      if (error && typeof error === 'object') {
        const errorObj = error as any;
        console.error('Error details:', {
          status: errorObj.status,
          message: errorObj.message,
          code: errorObj.code,
          name: errorObj.name
        });
      }
      
      // Re-throw the error instead of hiding it with fallback data
      throw new Error(`Failed to fetch issues: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  },
  ['issues-data'],
  { revalidate: CACHE_REVALIDATE_SECONDS, tags: ['github-issues'] },
);

// Cache pulls data (with actual pull request objects)
const getCachedPulls = unstable_cache(
  async () => {
    try {
      let allPulls: PullRequest[] = [];
      let page = 1;
      let hasMore = true;

      while (hasMore && page <= 10) { // Limit to 1000 pulls max
        const response = await octokit.pulls.list({
          owner: GITHUB_OWNER,
          repo: GITHUB_REPO,
          state: 'all',
          per_page: 100,
          page: page,
        });

        if (response.data.length === 0) {
          hasMore = false;
        } else {
          allPulls = allPulls.concat(response.data);
          page++;
          
          if (response.data.length < 100) {
            hasMore = false;
          }
        }
      }

      return allPulls;
    } catch (error) {
      console.error('GitHub API Error - Pulls:', error);
      
      // Log detailed error information for debugging
      if (error && typeof error === 'object') {
        const errorObj = error as any;
        console.error('Error details:', {
          status: errorObj.status,
          message: errorObj.message,
          code: errorObj.code,
          name: errorObj.name
        });
      }
      
      // Re-throw the error instead of hiding it with fallback data
      throw new Error(`Failed to fetch pull requests: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  },
  ['pulls-data'],
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
      console.error('GitHub API Error - Branches:', error);
      
      // Log detailed error information for debugging
      if (error && typeof error === 'object') {
        const errorObj = error as any;
        console.error('Error details:', {
          status: errorObj.status,
          message: errorObj.message,
          code: errorObj.code,
          name: errorObj.name
        });
      }
      
      // Re-throw the error instead of hiding it with fallback data
      throw new Error(`Failed to fetch branches: ${error instanceof Error ? error.message : 'Unknown error'}`);
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
    console.log('=== Starting getContributors function ===');
    
    // Fetch all contributors with pagination
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
    
    console.log(`Found ${allContributors.length} contributors across ${page - 1} pages`);

    if (allContributors.length === 0) {
      console.log('No contributors found, returning empty array');
      return [];
    }
    
    // Convert to full Contributor type for compatibility
    const contributors = allContributors as Contributor[];

    // Fetch detailed stats
    console.log('Fetching contributor stats...');
    const stats = await fetchContributorStats();
    console.log(`Found stats for ${stats.length} contributors`);

    // Enhance contributors with additional data
    const enhancedContributors: EnhancedContributor[] = await Promise.all(
      contributors.map(async (contributor) => {
        try {
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

          if (contributorStats && contributorStats.weeks && Array.isArray(contributorStats.weeks)) {
            // Use the total field for accurate commit count
            totalCommits = contributorStats.total || 0;
            
            // Sum up additions and deletions from all weeks
            contributorStats.weeks.forEach((week) => {
              additions += week.a || 0;
              deletions += week.d || 0;
            });
            
            console.log(`Stats for ${contributor.login}: commits=${totalCommits}, additions=${additions}, deletions=${deletions}`);
          } else {
            console.warn(`No detailed stats available for ${contributor.login}, using fallback data`);
            
            // Fallback: use contributions count as approximate commits
            totalCommits = contributor.contributions || 0;
            additions = 0;
            deletions = 0;
          }

          return {
            ...contributor,
            ...userDetails,
            additions,
            deletions,
            total_commits: totalCommits,
          };
        } catch (error) {
          console.error(`Error processing contributor ${contributor.login}:`, error);
          
          // Return basic contributor info even if enhancement fails
          return {
            ...contributor,
            total_commits: contributor.contributions || 0,
            additions: 0,
            deletions: 0,
            avatar_url: contributor.avatar_url || `https://github.com/${contributor.login}.png`,
          };
        }
      }),
    );

    // Sort by total contributions
    enhancedContributors.sort((a, b) => {
      const scoreA = (a.contributions || 0) + (a.additions || 0) + (a.deletions || 0);
      const scoreB = (b.contributions || 0) + (b.additions || 0) + (b.deletions || 0);
      return scoreB - scoreA;
    });

    console.log('Enhanced contributors summary:');
    enhancedContributors.forEach(contributor => {
      console.log(`  ${contributor.login}: ${contributor.total_commits} commits, ${contributor.additions} additions, ${contributor.deletions} deletions`);
    });

    console.log('=== getContributors function completed successfully ===');
    return enhancedContributors;
  } catch (error) {
    console.error('=== GitHub API Error in getContributors function ===', error);
    
    // Log detailed error information for debugging
    if (error && typeof error === 'object') {
      const errorObj = error as any;
      console.error('Error details:', {
        status: errorObj.status,
        message: errorObj.message,
        code: errorObj.code,
        name: errorObj.name
      });
    }
    
    // Re-throw the error instead of hiding it with fallback data
    throw new Error(`Failed to fetch contributors: ${error instanceof Error ? error.message : 'Unknown error'}`);
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
    const [repoInfo, releases, languages, issues, pulls, branchesCount] = await Promise.all([
      getCachedRepoInfo(),
      getCachedReleases(),
      getCachedLanguages(),
      getCachedIssues(),
      getCachedPulls(),
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
        totalIssues: issues.length,
        totalPulls: pulls.length,
        totalBranches: branchesCount,
        totalReleases: releases.length,
      },
      issues: issues,
      pulls: pulls,
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
        issues: repoData.issues || [],
        pulls: repoData.pulls || [],
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

/**
 * Get multiple license files from GitHub API with caching
 */
const getCachedLicensesFromGitHub = unstable_cache(
  async (): Promise<Array<{ content: string; name: string; filename: string }>> => {
    const licenses = [];
    
    try {
      // Get LICENSE file (dual licensing explanation)
      const { data: licenseFile } = await octokit.repos.getContent({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
        path: 'LICENSE',
      });
      
      if ('content' in licenseFile) {
        const licenseContent = Buffer.from(licenseFile.content, 'base64').toString('utf8');
        licenses.push({
          content: licenseContent,
          name: 'GameGuild Dual License',
          filename: 'LICENSE'
        });
      }
      
      // Get LICENSE-AGPLv3 file (full AGPL text)
      const { data: agplFile } = await octokit.repos.getContent({
        owner: GITHUB_OWNER,
        repo: GITHUB_REPO,
        path: 'LICENSE-AGPLv3',
      });
      
      if ('content' in agplFile) {
        const agplContent = Buffer.from(agplFile.content, 'base64').toString('utf8');
        licenses.push({
          content: agplContent,
          name: 'GNU Affero General Public License v3.0',
          filename: 'LICENSE-AGPLv3'
        });
      }
      
    } catch (error) {
      console.error('Error fetching license files from GitHub:', error);
    }
    
    return licenses;
  },
  ['github-licenses'],
  { revalidate: 604800, tags: ['github-licenses'] } // 1 week cache (604800 seconds)
);

export async function getLicensesFromGitHub(): Promise<Array<{ content: string; name: string; filename: string }>> {
  return getCachedLicensesFromGitHub();
}

// Cache invalidation function to force refresh of contributor data
export async function invalidateContributorCache(): Promise<{ success: boolean; message: string }> {
  'use server';
  
  try {
    const { revalidateTag } = await import('next/cache');
    
    // Invalidate all GitHub-related cache tags
    revalidateTag('github-contributors');
    revalidateTag('github-stats');
    revalidateTag('github-contributors-batch');
    revalidateTag('github-user');
    revalidateTag('github-repo-info');
    revalidateTag('github-releases');
    revalidateTag('github-languages');
    revalidateTag('github-issues');
    revalidateTag('github-pulls');
    revalidateTag('github-branches');
    revalidateTag('github-repo-stats');
    
    console.log('GitHub contributor cache invalidated successfully');
    
    return {
      success: true,
      message: 'Contributor cache refreshed successfully. The updated calculations will be reflected on the next page load.'
    };
  } catch (error) {
    console.error('Error invalidating contributor cache:', error);
    return {
      success: false,
      message: 'Failed to refresh contributor cache'
    };
  }
}

// Re-export types for convenience
export type { Contributor, EnhancedContributor, GitHubCommitStat, Repository, Issue, PullRequest };
