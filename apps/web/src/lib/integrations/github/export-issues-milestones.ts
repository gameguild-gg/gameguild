#!/usr/bin/env tsx
/**
 * Script to export GitHub issues and milestones to a markdown file
 * Usage: npx tsx src/lib/integrations/github/export-issues-milestones.ts
 */

import { writeFileSync } from 'fs';
import { Octokit } from 'octokit';
import { join } from 'path';

// GitHub repository constants
const GITHUB_OWNER = 'gameguild-gg';
const GITHUB_REPO = 'gameguild';

// Create Octokit instance
const createOctokit = () => {
    const token = process.env.GITHUB_TOKEN;
    if (!token) {
        console.warn('GITHUB_TOKEN environment variable not set. API requests may be rate limited.');
    }
    return new Octokit({
        auth: token,
        userAgent: 'GameGuild-Export-Script',
        request: {
            timeout: 30000,
            retries: 3,
            retryAfter: 2,
        },
    });
};

const octokit = createOctokit().rest;

// Types
type Issue = {
    number: number;
    title: string;
    state: string;
    created_at: string;
    updated_at: string;
    closed_at: string | null;
    user: { login: string } | null;
    labels: Array<{ name: string; color: string }>;
    assignees: Array<{ login: string }>;
    milestone: { title: string; number: number } | null;
    body: string | null;
    html_url: string;
    comments: number;
    pull_request?: unknown;
};

type Milestone = {
    number: number;
    title: string;
    state: string;
    description: string | null;
    created_at: string;
    updated_at: string;
    due_on: string | null;
    closed_at: string | null;
    open_issues: number;
    closed_issues: number;
    html_url: string;
};

/**
 * Fetch all milestones (open and closed)
 */
async function fetchAllMilestones(): Promise<Milestone[]> {
    console.log('Fetching milestones...');
    const milestones: Milestone[] = [];

    // Fetch open milestones
    let page = 1;
    let hasMore = true;

    while (hasMore) {
        const { data } = await octokit.issues.listMilestones({
            owner: GITHUB_OWNER,
            repo: GITHUB_REPO,
            state: 'open',
            per_page: 100,
            page,
        });

        if (data.length === 0) {
            hasMore = false;
        } else {
            milestones.push(...(data as Milestone[]));
            page++;
            if (data.length < 100) hasMore = false;
        }
    }

    // Fetch closed milestones
    page = 1;
    hasMore = true;

    while (hasMore) {
        const { data } = await octokit.issues.listMilestones({
            owner: GITHUB_OWNER,
            repo: GITHUB_REPO,
            state: 'closed',
            per_page: 100,
            page,
        });

        if (data.length === 0) {
            hasMore = false;
        } else {
            milestones.push(...(data as Milestone[]));
            page++;
            if (data.length < 100) hasMore = false;
        }
    }

    console.log(`Found ${milestones.length} milestones`);
    return milestones;
}

/**
 * Fetch all issues (open and closed, excluding pull requests)
 */
async function fetchAllIssues(): Promise<Issue[]> {
    console.log('Fetching issues...');
    const issues: Issue[] = [];

    // Fetch open issues
    let page = 1;
    let hasMore = true;

    while (hasMore) {
        const { data } = await octokit.issues.listForRepo({
            owner: GITHUB_OWNER,
            repo: GITHUB_REPO,
            state: 'open',
            per_page: 100,
            page,
        });

        if (data.length === 0) {
            hasMore = false;
        } else {
            // Filter out pull requests
            const filteredIssues = data.filter(issue => !issue.pull_request);
            issues.push(...(filteredIssues as Issue[]));
            page++;
            if (data.length < 100) hasMore = false;
        }
    }

    // Fetch closed issues
    page = 1;
    hasMore = true;

    while (hasMore) {
        const { data } = await octokit.issues.listForRepo({
            owner: GITHUB_OWNER,
            repo: GITHUB_REPO,
            state: 'closed',
            per_page: 100,
            page,
        });

        if (data.length === 0) {
            hasMore = false;
        } else {
            // Filter out pull requests
            const filteredIssues = data.filter(issue => !issue.pull_request);
            issues.push(...(filteredIssues as Issue[]));
            page++;
            if (data.length < 100) hasMore = false;
        }
    }

    console.log(`Found ${issues.length} issues`);
    return issues;
}

/**
 * Format date to readable string
 */
function formatDate(dateString: string | null): string {
    if (!dateString) return 'N/A';
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric'
    });
}

/**
 * Generate markdown content
 */
function generateMarkdown(milestones: Milestone[], issues: Issue[]): string {
    const openMilestones = milestones.filter(m => m.state === 'open');
    const closedMilestones = milestones.filter(m => m.state === 'closed');
    const openIssues = issues.filter(i => i.state === 'open');
    const closedIssues = issues.filter(i => i.state === 'closed');

    let markdown = `# GitHub Issues and Milestones Export\n\n`;
    markdown += `**Repository:** ${GITHUB_OWNER}/${GITHUB_REPO}\n\n`;
    markdown += `**Generated:** ${new Date().toLocaleString()}\n\n`;
    markdown += `---\n\n`;

    // Summary
    markdown += `## Summary\n\n`;
    markdown += `- **Total Milestones:** ${milestones.length} (${openMilestones.length} open, ${closedMilestones.length} closed)\n`;
    markdown += `- **Total Issues:** ${issues.length} (${openIssues.length} open, ${closedIssues.length} closed)\n\n`;
    markdown += `---\n\n`;

    // Milestones Section
    markdown += `## Milestones\n\n`;

    // Open Milestones
    if (openMilestones.length > 0) {
        markdown += `### Open Milestones (${openMilestones.length})\n\n`;

        // Sort by due date
        openMilestones.sort((a, b) => {
            if (!a.due_on) return 1;
            if (!b.due_on) return -1;
            return new Date(a.due_on).getTime() - new Date(b.due_on).getTime();
        });

        openMilestones.forEach(milestone => {
            markdown += `#### [${milestone.title}](${milestone.html_url}) (#${milestone.number})\n\n`;
            if (milestone.description) {
                markdown += `${milestone.description}\n\n`;
            }
            markdown += `- **State:** 🟢 Open\n`;
            markdown += `- **Created:** ${formatDate(milestone.created_at)}\n`;
            markdown += `- **Due Date:** ${formatDate(milestone.due_on)}\n`;
            markdown += `- **Progress:** ${milestone.closed_issues}/${milestone.open_issues + milestone.closed_issues} issues closed\n`;
            const progress = milestone.open_issues + milestone.closed_issues > 0
                ? Math.round((milestone.closed_issues / (milestone.open_issues + milestone.closed_issues)) * 100)
                : 0;
            markdown += `- **Completion:** ${progress}%\n\n`;
        });
    }

    // Closed Milestones
    if (closedMilestones.length > 0) {
        markdown += `### Closed Milestones (${closedMilestones.length})\n\n`;

        // Sort by closed date (most recent first)
        closedMilestones.sort((a, b) => {
            if (!a.closed_at) return 1;
            if (!b.closed_at) return -1;
            return new Date(b.closed_at).getTime() - new Date(a.closed_at).getTime();
        });

        closedMilestones.forEach(milestone => {
            markdown += `#### [${milestone.title}](${milestone.html_url}) (#${milestone.number})\n\n`;
            if (milestone.description) {
                markdown += `${milestone.description}\n\n`;
            }
            markdown += `- **State:** ✅ Closed\n`;
            markdown += `- **Created:** ${formatDate(milestone.created_at)}\n`;
            markdown += `- **Closed:** ${formatDate(milestone.closed_at)}\n`;
            markdown += `- **Total Issues:** ${milestone.open_issues + milestone.closed_issues}\n\n`;
        });
    }

    markdown += `---\n\n`;

    // Issues Section
    markdown += `## Issues\n\n`;

    // Group issues by milestone
    const issuesByMilestone = new Map<string, Issue[]>();
    issuesByMilestone.set('no-milestone', []);

    milestones.forEach(m => {
        issuesByMilestone.set(m.title, []);
    });

    issues.forEach(issue => {
        const milestoneKey = issue.milestone ? issue.milestone.title : 'no-milestone';
        const arr = issuesByMilestone.get(milestoneKey);
        if (arr) {
            arr.push(issue);
        }
    });

    // Open Issues
    markdown += `### Open Issues (${openIssues.length})\n\n`;

    // Issues without milestone
    const openNoMilestone = issuesByMilestone.get('no-milestone')?.filter(i => i.state === 'open') || [];
    if (openNoMilestone.length > 0) {
        markdown += `#### No Milestone (${openNoMilestone.length})\n\n`;
        openNoMilestone.forEach(issue => {
            markdown += formatIssue(issue);
        });
    }

    // Issues by milestone
    openMilestones.forEach(milestone => {
        const milestoneIssues = issuesByMilestone.get(milestone.title)?.filter(i => i.state === 'open') || [];
        if (milestoneIssues.length > 0) {
            markdown += `#### Milestone: [${milestone.title}](${milestone.html_url}) (${milestoneIssues.length})\n\n`;
            milestoneIssues.forEach(issue => {
                markdown += formatIssue(issue);
            });
        }
    });

    // Closed Issues
    markdown += `### Closed Issues (${closedIssues.length})\n\n`;

    // Issues without milestone
    const closedNoMilestone = issuesByMilestone.get('no-milestone')?.filter(i => i.state === 'closed') || [];
    if (closedNoMilestone.length > 0) {
        markdown += `#### No Milestone (${closedNoMilestone.length})\n\n`;
        closedNoMilestone.forEach(issue => {
            markdown += formatIssue(issue);
        });
    }

    // Issues by milestone
    [...openMilestones, ...closedMilestones].forEach(milestone => {
        const milestoneIssues = issuesByMilestone.get(milestone.title)?.filter(i => i.state === 'closed') || [];
        if (milestoneIssues.length > 0) {
            markdown += `#### Milestone: [${milestone.title}](${milestone.html_url}) (${milestoneIssues.length})\n\n`;
            milestoneIssues.forEach(issue => {
                markdown += formatIssue(issue);
            });
        }
    });

    return markdown;
}

/**
 * Format a single issue
 */
function formatIssue(issue: Issue): string {
    let md = `##### [#${issue.number} - ${issue.title}](${issue.html_url})\n\n`;

    md += `- **State:** ${issue.state === 'open' ? '🟢 Open' : '✅ Closed'}\n`;
    md += `- **Created:** ${formatDate(issue.created_at)}\n`;
    md += `- **Updated:** ${formatDate(issue.updated_at)}\n`;
    if (issue.closed_at) {
        md += `- **Closed:** ${formatDate(issue.closed_at)}\n`;
    }
    if (issue.user) {
        md += `- **Author:** @${issue.user.login}\n`;
    }
    if (issue.assignees.length > 0) {
        md += `- **Assignees:** ${issue.assignees.map(a => `@${a.login}`).join(', ')}\n`;
    }
    if (issue.labels.length > 0) {
        const labelBadges = issue.labels.map(l => `\`${l.name}\``).join(' ');
        md += `- **Labels:** ${labelBadges}\n`;
    }
    if (issue.comments > 0) {
        md += `- **Comments:** ${issue.comments}\n`;
    }

    if (issue.body && issue.body.trim().length > 0) {
        // Include full body content
        const body = issue.body.trim();
        md += `\n**Description:**\n\n`;
        md += `${body}\n`;
    } else {
        md += `\n*No description provided.*\n`;
    }

    md += `\n---\n\n`;
    return md;
}

/**
 * Main execution
 */
async function main() {
    try {
        console.log('Starting GitHub export...');
        console.log(`Repository: ${GITHUB_OWNER}/${GITHUB_REPO}`);
        console.log('---');

        // Fetch data
        const [milestones, issues] = await Promise.all([
            fetchAllMilestones(),
            fetchAllIssues(),
        ]);

        // Generate markdown
        console.log('Generating markdown...');
        const markdown = generateMarkdown(milestones, issues);

        // Save to file
        const outputPath = join(process.cwd(), 'GITHUB_ISSUES_AND_MILESTONES.md');
        writeFileSync(outputPath, markdown, 'utf-8');

        console.log('---');
        console.log(`✅ Export complete!`);
        console.log(`📄 File saved to: ${outputPath}`);
        console.log(`📊 Summary:`);
        console.log(`   - Milestones: ${milestones.length}`);
        console.log(`   - Issues: ${issues.length}`);
    } catch (error) {
        console.error('❌ Error during export:', error);
        process.exit(1);
    }
}

// Run the script
main();
