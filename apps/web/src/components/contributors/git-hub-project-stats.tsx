import React from 'react';
import { AlertCircle, Eye, FileText, GitBranch, GitPullRequest, Shield, Users } from 'lucide-react';
import type { Issue, PullRequest, Repository } from '@/lib/integrations/github';

interface ProjectStat {
  icon: React.ReactNode;
  label: string;
  value: string | React.ReactNode;
  change?: string;
  link?: string;
}

interface RepoData {
  totalIssues: number;
  totalPulls: number;
  totalReleases: number;
  totalBranches: number;
  totalContributors: number;
  data: {
    issues: Issue[];
    pulls: PullRequest[];
    releases: { id: number; name: string | null; tag_name: string; published_at: string | null; download_count: number; }[];
    branches: any[];
    contributors: any[];
    license: any;
  };
}

interface GitHubProjectStatsProps {
  repositoryData: RepoData;
}

export function GitHubProjectStats({ repositoryData }: GitHubProjectStatsProps) {
  // Format numbers for display
  const formatNumber = (num: number): string => {
    if (num >= 1000000) return `${(num / 1000000).toFixed(1)}M`;
    if (num >= 1000) return `${(num / 1000).toFixed(1)}K`;
    return num.toString();
  };

  // Calculate stats from real data with safe fallbacks
  // Filter out pull requests from issues (GitHub API includes PRs in issues endpoint)
  const actualIssues = repositoryData.data.issues?.filter((issue) => !issue.pull_request) || [];
  const openIssues = actualIssues.filter((issue) => issue.state === 'open').length;
  const closedIssues = actualIssues.filter((issue) => issue.state === 'closed').length;
  const mergedPulls = repositoryData.data.pulls?.filter((pr) => pr.state === 'closed' && pr.merged_at).length || 0;

  // GitHub repository URLs
  const GITHUB_REPO_URL = 'https://github.com/gameguild-gg/gameguild';

  // Stats using real GitHub data
  const topRowStats: ProjectStat[] = [
    {
      icon: <Users className="size-4" />,
      label: 'Contributors',
      value: formatNumber(repositoryData.totalContributors),
      link: `${GITHUB_REPO_URL}/graphs/contributors`,
    },
    {
      icon: <GitPullRequest className="size-4" />,
      label: 'Pull Requests',
      value: formatNumber(repositoryData.totalPulls),
      link: `${GITHUB_REPO_URL}/pulls?q=is%3Apr`,
    },
    {
      icon: <Eye className="size-4" />,
      label: 'Open Issues',
      value: formatNumber(openIssues),
      link: `${GITHUB_REPO_URL}/issues?q=is%3Aopen+is%3Aissue`,
    },
    {
      icon: <AlertCircle className="size-4" />,
      label: 'Closed Issues',
      value: formatNumber(closedIssues),
      link: `${GITHUB_REPO_URL}/issues?q=is%3Aclosed+is%3Aissue`,
    },
  ];

  const bottomRowStats: ProjectStat[] = [
    {
      icon: <FileText className="size-4" />,
      label: 'Releases',
      value: formatNumber(repositoryData.totalReleases),
      link: `${GITHUB_REPO_URL}/releases`,
    },
    {
      icon: <GitBranch className="size-4" />,
      label: 'Branches',
      value: formatNumber(repositoryData.totalBranches),
      link: `${GITHUB_REPO_URL}/branches`,
    },
    {
      icon: <GitPullRequest className="size-4" />,
      label: 'Merged PRs',
      value: formatNumber(mergedPulls),
      link: `${GITHUB_REPO_URL}/pulls?q=is%3Apr+is%3Amerged`,
    },
    {
      icon: <Shield className="size-4" />,
      label: 'Dual License',
      value: (
        <div className="flex items-center gap-2">
          <img 
             src="https://upload.wikimedia.org/wikipedia/commons/0/06/AGPLv3_Logo.svg" 
             alt="AGPL v3" 
             className="h-4 w-auto bg-white rounded px-1"
           />
          <span className="text-xs">+</span>
          <span className="text-xs bg-blue-600 text-white px-1 py-0.5 rounded font-semibold">💼</span>
        </div>
      ),
      link: `${GITHUB_REPO_URL}/blob/main/LICENSE.md`,
    },
  ];

  return (
    <section className="w-full mb-12">
      {/* Background with planet effect */}
      <div className="relative overflow-hidden bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
        {/* Planet/sphere background */}
        <div className="absolute bottom-0 left-1/2 transform -translate-x-1/2 translate-y-1/2">
          <div className="w-96 h-96 rounded-full bg-gradient-to-t from-blue-600/30 via-purple-500/20 to-transparent blur-3xl"></div>
        </div>

        {/* Additional glow effects */}
        <div className="absolute top-0 left-0 w-full h-px bg-gradient-to-r from-transparent via-blue-400/50 to-transparent"></div>

        {/* Centered content container */}
        <div className="container mx-auto relative px-4 py-8 md:px-6 md:py-12">
          {/* Command line header */}
          <div className="mb-8">
            <div className="text-slate-400 text-sm font-mono mb-6">gh pulse --year 2024 --repo gameguild-gg/gameguild</div>
          </div>

          {/* Stats Grid - Top Row */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 mb-8">
            {topRowStats.map((stat) => (
              <div key={stat.label} className="text-center">
                <div className="flex items-center justify-center gap-2 mb-2">
                  <span className="text-slate-400 text-sm">{stat.icon}</span>
                  <span className="text-slate-400 text-sm">{stat.label}</span>
                </div>
                {stat.link ? (
                  <a
                    href={stat.link}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-white text-3xl md:text-4xl font-bold mb-1 hover:text-blue-400 transition-colors duration-200 cursor-pointer inline-block"
                  >
                    {stat.value}
                  </a>
                ) : (
                  <div className="text-white text-3xl md:text-4xl font-bold mb-1">{stat.value}</div>
                )}
                {stat.change && <div className="text-slate-400 text-xs">{stat.change}</div>}
              </div>
            ))}
          </div>

          {/* Stats Grid - Bottom Row */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 mb-12">
            {bottomRowStats.map((stat) => (
              <div key={stat.label} className="text-center">
                <div className="flex items-center justify-center gap-2 mb-2">
                  <span className="text-slate-400 text-sm">{stat.icon}</span>
                  <span className="text-slate-400 text-sm">{stat.label}</span>
                </div>
                {stat.link ? (
                  <a
                    href={stat.link}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-white text-3xl md:text-4xl font-bold mb-1 hover:text-blue-400 transition-colors duration-200 cursor-pointer inline-block"
                  >
                    {stat.value}
                  </a>
                ) : (
                  <div className="text-white text-3xl md:text-4xl font-bold mb-1">{stat.value}</div>
                )}
                {stat.change && <div className="text-slate-400 text-xs">{stat.change}</div>}
              </div>
            ))}
          </div>

          {/* Repository Activity Summary */}
          <div className="text-center text-slate-400">
            <p className="text-sm">Real-time data from GitHub API • Last updated: {new Date().toLocaleDateString()}</p>
          </div>
        </div>
      </div>
    </section>
  );
}
