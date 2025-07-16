'use server';

export async function getGitHubIssues(searchParams: URLSearchParams) {
  try {
    const response = await fetch(`https://api.github.com/repos/gameguild-gg/website/issues?${searchParams.toString()}`, {
      headers: {
        Accept: 'application/vnd.github.v3+json',
        Authorization: `Bearer ${process.env.GITHUB_TOKEN}`,
      },
    });

    if (!response.ok) {
      throw new Error('Failed to fetch issues');
    }

    const issues = await response.json();
    return issues;
  } catch (error) {
    throw new Error('Failed to fetch issues');
  }
}

import { Octokit } from 'octokit';
import NodeCache from 'node-cache';

const octokit = new Octokit({ auth: process.env.GITHUB_TOKEN }).rest;
const cache = new NodeCache({ stdTTL: 3600 }); // 1 hour TTL

async function fetchAllIssues(state: 'open' | 'closed' | 'all') {
  const cacheKey = `all_issues_${state}`;
  const cachedIssues = cache.get(cacheKey);

  if (cachedIssues) {
    return cachedIssues;
  }

  let allIssues: any[] = [];
  let page = 1;
  let hasNextPage = true;

  while (hasNextPage) {
    const [issuesResponse, pullsResponse] = await Promise.all([
      octokit.issues.listForRepo({
        owner: 'gameguild-gg',
        repo: 'website',
        state: state,
        per_page: 100,
        page: page,
      }),
      octokit.pulls.list({
        owner: 'gameguild-gg',
        repo: 'website',
        state: state,
        per_page: 100,
        page: page,
      }),
    ]);

    const issuesWithReviews = await Promise.all(
      issuesResponse.data.map(async (issue) => {
        if (issue.pull_request) {
          // Get both requested reviewers and actual reviews
          const [requestedReviewers, reviews] = await Promise.all([
            octokit.pulls.listRequestedReviewers({
              owner: 'gameguild-gg',
              repo: 'website',
              pull_number: issue.number,
            }),
            octokit.pulls.listReviews({
              owner: 'gameguild-gg',
              repo: 'website',
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
      })
    );

    allIssues = allIssues.concat(issuesWithReviews);

    hasNextPage = issuesResponse.data.length === 100;
    page++;
  }

  cache.set(cacheKey, allIssues);
  return allIssues;
}

export async function getAllGitHubIssues(state: 'open' | 'closed' | 'all' = 'all') {
  try {
    const issues = await fetchAllIssues(state);
    return {
      total: issues.length,
      issues: issues,
    };
  } catch (error) {
    console.error('Error fetching GitHub issues:', error);
    throw new Error('Failed to fetch GitHub issues');
  }
}
