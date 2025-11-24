#!/usr/bin/env tsx
/**
 * Script to analyze GitHub issues against the current codebase
 * Usage: npx tsx src/lib/integrations/github/analyze-issues.ts
 */

import { existsSync, writeFileSync } from 'fs';
import { Octokit } from 'octokit';
import { join } from 'path';

// GitHub repository constants
const GITHUB_OWNER = 'gameguild-gg';
const GITHUB_REPO = 'gameguild';

// Workspace root
const WORKSPACE_ROOT = process.cwd();

// Create Octokit instance
const createOctokit = () => {
    const token = process.env.GITHUB_TOKEN;
    if (!token) {
        console.warn('GITHUB_TOKEN environment variable not set. API requests may be rate limited.');
    }
    return new Octokit({
        auth: token,
        userAgent: 'GameGuild-Analysis-Script',
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

type Analysis = {
    issue: Issue;
    analysis: {
        relevance: 'high' | 'medium' | 'low' | 'obsolete';
        reasoning: string;
        mentionedFiles: string[];
        existingFiles: string[];
        missingFiles: string[];
        relatedModules: string[];
        recommendation: string;
        complexity: 'simple' | 'moderate' | 'complex';
        estimatedEffort: string;
    };
};

/**
 * Fetch all open issues
 */
async function fetchOpenIssues(): Promise<Issue[]> {
    console.log('Fetching open issues...');
    const issues: Issue[] = [];

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

    console.log(`Found ${issues.length} open issues`);
    return issues;
}

/**
 * Extract file paths mentioned in issue text
 */
function extractMentionedFiles(text: string): string[] {
    const files: string[] = [];

    // Match common file path patterns
    const patterns = [
        // Explicit paths with extensions
        /(?:^|\s|`)((?:[\w-]+\/)*[\w-]+\.\w+)(?:\s|`|$)/g,
        // Code block file references
        /```[\w]*\n.*?(?:File:|Path:)\s*([^\n]+)/gs,
        // Markdown links to files
        /\[([^\]]+\.[\w]+)\]/g,
    ];

    patterns.forEach(pattern => {
        const matches = text.matchAll(pattern);
        for (const match of matches) {
            const file = match[1]?.trim();
            if (file && !files.includes(file)) {
                files.push(file);
            }
        }
    });

    return files;
}

/**
 * Check if files exist in the codebase
 */
function checkFilesExist(files: string[]): { existing: string[], missing: string[] } {
    const existing: string[] = [];
    const missing: string[] = [];

    files.forEach(file => {
        const possiblePaths = [
            join(WORKSPACE_ROOT, file),
            join(WORKSPACE_ROOT, 'apps/api', file),
            join(WORKSPACE_ROOT, 'apps/web', file),
            join(WORKSPACE_ROOT, 'apps/api/Source', file),
            join(WORKSPACE_ROOT, 'apps/web/src', file),
        ];

        const exists = possiblePaths.some(path => existsSync(path));
        if (exists) {
            existing.push(file);
        } else {
            missing.push(file);
        }
    });

    return { existing, missing };
}

/**
 * Identify related modules based on issue labels and content
 */
function identifyRelatedModules(issue: Issue): string[] {
    const modules: string[] = [];
    const text = `${issue.title} ${issue.body || ''}`.toLowerCase();

    // Check labels
    const labelMap: Record<string, string[]> = {
        'front-end': ['apps/web'],
        'back-end': ['apps/api'],
        'web3': ['apps/web/src/lib/web3'],
        'authentication': ['apps/api/Source/Modules/Authentication'],
        'database': ['apps/api/Source/Modules'],
        'design-&-marketing': ['apps/web/src/app', 'apps/web/public'],
        'documentation': ['docs'],
    };

    issue.labels.forEach(label => {
        const labelModules = labelMap[label.name];
        if (labelModules) {
            modules.push(...labelModules);
        }
    });

    // Check content keywords
    const keywordMap: Record<string, string[]> = {
        'user': ['apps/api/Source/Modules/Users'],
        'profile': ['apps/api/Source/Modules/UserProfiles'],
        'auth': ['apps/api/Source/Modules/Authentication'],
        'tenant': ['apps/api/Source/Modules/Tenants'],
        'permission': ['apps/api/Source/Modules/Permissions'],
        'course': ['apps/api/Source/Modules/Contents'],
        'payment': ['apps/web/src/lib/commerce'],
        'wallet': ['apps/web/src/lib/web3'],
        'notification': ['apps/web/src/lib/communication'],
    };

    Object.entries(keywordMap).forEach(([keyword, paths]) => {
        if (text.includes(keyword)) {
            modules.push(...paths);
        }
    });

    return [...new Set(modules)]; // Remove duplicates
}

/**
 * Analyze issue complexity and effort
 */
function analyzeComplexity(issue: Issue): { complexity: 'simple' | 'moderate' | 'complex', estimatedEffort: string } {
    const text = `${issue.title} ${issue.body || ''}`;
    const labels = issue.labels.map(l => l.name);

    // Simple indicators
    const simpleIndicators = [
        labels.includes('good first issue'),
        labels.includes('documentation'),
        text.toLowerCase().includes('typo'),
        text.toLowerCase().includes('update readme'),
        text.length < 200,
    ];

    // Complex indicators
    const complexIndicators = [
        labels.includes('full-stack'),
        labels.includes('web3'),
        labels.includes('back-end') && labels.includes('front-end'),
        text.toLowerCase().includes('architecture'),
        text.toLowerCase().includes('refactor'),
        text.toLowerCase().includes('migration'),
        text.toLowerCase().includes('database'),
        text.split('\n').length > 20,
    ];

    const simpleCount = simpleIndicators.filter(Boolean).length;
    const complexCount = complexIndicators.filter(Boolean).length;

    if (complexCount >= 3) {
        return { complexity: 'complex', estimatedEffort: '2-4 weeks' };
    } else if (complexCount >= 1 || simpleCount === 0) {
        return { complexity: 'moderate', estimatedEffort: '3-7 days' };
    } else {
        return { complexity: 'simple', estimatedEffort: '1-2 days' };
    }
}

/**
 * Determine issue relevance based on age, labels, and content
 */
function determineRelevance(issue: Issue): { relevance: 'high' | 'medium' | 'low' | 'obsolete', reasoning: string } {
    const ageInDays = Math.floor((Date.now() - new Date(issue.created_at).getTime()) / (1000 * 60 * 60 * 24));
    const labels = issue.labels.map(l => l.name);
    const hasStaleLabel = labels.includes('Stale');
    const hasAssignee = issue.assignees.length > 0;
    const hasMilestone = issue.milestone !== null;
    const recentActivity = Math.floor((Date.now() - new Date(issue.updated_at).getTime()) / (1000 * 60 * 60 * 24)) < 30;

    // High priority indicators
    if ((labels.includes('bug') || labels.includes('security')) && !hasStaleLabel) {
        return {
            relevance: 'high',
            reasoning: 'Critical issue (bug/security) requiring immediate attention'
        };
    }

    if (hasMilestone && hasAssignee && recentActivity) {
        return {
            relevance: 'high',
            reasoning: 'Active issue with milestone and assignee, recently updated'
        };
    }

    // Obsolete indicators
    if (hasStaleLabel && ageInDays > 180 && !recentActivity) {
        return {
            relevance: 'obsolete',
            reasoning: 'Stale issue, not updated in 6+ months, likely no longer relevant'
        };
    }

    // Medium priority
    if (hasMilestone || hasAssignee) {
        return {
            relevance: 'medium',
            reasoning: 'Issue has milestone or assignee, indicating planned work'
        };
    }

    if (labels.includes('enhancement') && !hasStaleLabel) {
        return {
            relevance: 'medium',
            reasoning: 'Enhancement request without stale label'
        };
    }

    // Low priority
    if (hasStaleLabel || ageInDays > 90) {
        return {
            relevance: 'low',
            reasoning: 'Old or stale issue, may need review before work'
        };
    }

    return {
        relevance: 'medium',
        reasoning: 'Standard issue awaiting triage'
    };
}

/**
 * Generate recommendation for each issue
 */
function generateRecommendation(issue: Issue, analysis: any): string {
    const recommendations: string[] = [];

    if (analysis.relevance === 'obsolete') {
        recommendations.push('🗑️ **Consider closing this issue** - It appears to be outdated and no longer relevant.');
    } else if (analysis.relevance === 'high') {
        recommendations.push('🚀 **Prioritize this issue** - High impact and currently relevant.');
    }

    if (analysis.missingFiles.length > 0) {
        recommendations.push(`⚠️ **Verify file references** - ${analysis.missingFiles.length} mentioned files not found in current codebase.`);
    }

    if (analysis.relatedModules.length > 0) {
        recommendations.push(`📂 **Related modules**: ${analysis.relatedModules.join(', ')}`);
    }

    if (issue.labels.map(l => l.name).includes('good first issue') && analysis.complexity === 'simple') {
        recommendations.push('👋 **Great for new contributors** - Simple issue with clear scope.');
    }

    if (!issue.milestone) {
        recommendations.push('📋 **Assign to milestone** - No milestone set, consider adding to roadmap.');
    }

    if (issue.assignees.length === 0) {
        recommendations.push('👤 **Needs assignee** - No one currently assigned to this issue.');
    }

    return recommendations.join('\n');
}

/**
 * Analyze a single issue
 */
async function analyzeIssue(issue: Issue): Promise<Analysis> {
    const text = `${issue.title}\n${issue.body || ''}`;

    // Extract file references
    const mentionedFiles = extractMentionedFiles(text);
    const { existing, missing } = checkFilesExist(mentionedFiles);

    // Identify related modules
    const relatedModules = identifyRelatedModules(issue);

    // Analyze complexity
    const { complexity, estimatedEffort } = analyzeComplexity(issue);

    // Determine relevance
    const { relevance, reasoning } = determineRelevance(issue);

    // Generate recommendation
    const recommendation = generateRecommendation(issue, {
        relevance,
        missingFiles: missing,
        relatedModules,
        complexity,
    });

    return {
        issue,
        analysis: {
            relevance,
            reasoning,
            mentionedFiles,
            existingFiles: existing,
            missingFiles: missing,
            relatedModules,
            recommendation,
            complexity,
            estimatedEffort,
        },
    };
}

/**
 * Generate markdown report
 */
function generateReport(analyses: Analysis[]): string {
    let md = `# GitHub Issues Analysis Report\n\n`;
    md += `**Repository:** ${GITHUB_OWNER}/${GITHUB_REPO}\n\n`;
    md += `**Generated:** ${new Date().toLocaleString()}\n\n`;
    md += `**Total Open Issues Analyzed:** ${analyses.length}\n\n`;
    md += `---\n\n`;

    // Summary by relevance
    const byCriticalityCount = {
        high: analyses.filter(a => a.analysis.relevance === 'high').length,
        medium: analyses.filter(a => a.analysis.relevance === 'medium').length,
        low: analyses.filter(a => a.analysis.relevance === 'low').length,
        obsolete: analyses.filter(a => a.analysis.relevance === 'obsolete').length,
    };

    md += `## Summary by Priority\n\n`;
    md += `| Priority | Count | Percentage |\n`;
    md += `|----------|-------|------------|\n`;
    md += `| 🔴 High | ${byCriticalityCount.high} | ${Math.round((byCriticalityCount.high / analyses.length) * 100)}% |\n`;
    md += `| 🟡 Medium | ${byCriticalityCount.medium} | ${Math.round((byCriticalityCount.medium / analyses.length) * 100)}% |\n`;
    md += `| 🟢 Low | ${byCriticalityCount.low} | ${Math.round((byCriticalityCount.low / analyses.length) * 100)}% |\n`;
    md += `| ⚪ Obsolete | ${byCriticalityCount.obsolete} | ${Math.round((byCriticalityCount.obsolete / analyses.length) * 100)}% |\n\n`;

    // Summary by complexity
    const byComplexity = {
        simple: analyses.filter(a => a.analysis.complexity === 'simple').length,
        moderate: analyses.filter(a => a.analysis.complexity === 'moderate').length,
        complex: analyses.filter(a => a.analysis.complexity === 'complex').length,
    };

    md += `## Summary by Complexity\n\n`;
    md += `| Complexity | Count |\n`;
    md += `|------------|-------|\n`;
    md += `| ✅ Simple (1-2 days) | ${byComplexity.simple} |\n`;
    md += `| ⚠️ Moderate (3-7 days) | ${byComplexity.moderate} |\n`;
    md += `| 🔥 Complex (2-4 weeks) | ${byComplexity.complex} |\n\n`;

    md += `---\n\n`;

    // Detailed analysis by priority
    const priorityGroups = [
        { name: '🔴 High Priority Issues', filter: (a: Analysis) => a.analysis.relevance === 'high' },
        { name: '🟡 Medium Priority Issues', filter: (a: Analysis) => a.analysis.relevance === 'medium' },
        { name: '🟢 Low Priority Issues', filter: (a: Analysis) => a.analysis.relevance === 'low' },
        { name: '⚪ Obsolete Issues', filter: (a: Analysis) => a.analysis.relevance === 'obsolete' },
    ];

    priorityGroups.forEach(group => {
        const groupIssues = analyses.filter(group.filter);

        if (groupIssues.length > 0) {
            md += `## ${group.name} (${groupIssues.length})\n\n`;

            groupIssues.forEach(({ issue, analysis }) => {
                md += `### [#${issue.number} - ${issue.title}](${issue.html_url})\n\n`;

                md += `**Metadata:**\n`;
                md += `- **Created:** ${new Date(issue.created_at).toLocaleDateString()}\n`;
                md += `- **Updated:** ${new Date(issue.updated_at).toLocaleDateString()}\n`;
                md += `- **Author:** @${issue.user?.login || 'unknown'}\n`;
                md += `- **Labels:** ${issue.labels.map(l => `\`${l.name}\``).join(' ')}\n`;
                if (issue.milestone) {
                    md += `- **Milestone:** ${issue.milestone.title}\n`;
                }
                if (issue.assignees.length > 0) {
                    md += `- **Assignees:** ${issue.assignees.map(a => `@${a.login}`).join(', ')}\n`;
                }
                md += `\n`;

                md += `**Analysis:**\n`;
                md += `- **Relevance:** ${analysis.relevance.toUpperCase()}\n`;
                md += `- **Reasoning:** ${analysis.reasoning}\n`;
                md += `- **Complexity:** ${analysis.complexity}\n`;
                md += `- **Estimated Effort:** ${analysis.estimatedEffort}\n`;
                md += `\n`;

                if (analysis.mentionedFiles.length > 0) {
                    md += `**Referenced Files:**\n`;
                    md += `- Found: ${analysis.existingFiles.length}\n`;
                    md += `- Missing: ${analysis.missingFiles.length}\n`;
                    if (analysis.missingFiles.length > 0) {
                        md += `  - ${analysis.missingFiles.join(', ')}\n`;
                    }
                    md += `\n`;
                }

                if (analysis.relatedModules.length > 0) {
                    md += `**Related Modules:**\n`;
                    analysis.relatedModules.forEach(module => {
                        md += `- ${module}\n`;
                    });
                    md += `\n`;
                }

                md += `**Recommendation:**\n\n`;
                md += `${analysis.recommendation}\n\n`;

                md += `---\n\n`;
            });
        }
    });

    return md;
}

/**
 * Main execution
 */
async function main() {
    try {
        console.log('Starting issue analysis...');
        console.log(`Repository: ${GITHUB_OWNER}/${GITHUB_REPO}`);
        console.log(`Workspace: ${WORKSPACE_ROOT}`);
        console.log('---');

        // Fetch open issues
        const issues = await fetchOpenIssues();

        console.log(`Analyzing ${issues.length} issues...`);

        // Analyze each issue
        const analyses: Analysis[] = [];
        for (let i = 0; i < issues.length; i++) {
            const issue = issues[i];
            if (!issue) continue;
            console.log(`[${i + 1}/${issues.length}] Analyzing #${issue.number}: ${issue.title}`);
            const analysis = await analyzeIssue(issue);
            analyses.push(analysis);
        }

        // Generate report
        console.log('Generating analysis report...');
        const report = generateReport(analyses);

        // Save to file
        const outputPath = join(WORKSPACE_ROOT, 'GITHUB_ISSUES_ANALYSIS.md');
        writeFileSync(outputPath, report, 'utf-8');

        console.log('---');
        console.log(`✅ Analysis complete!`);
        console.log(`📄 Report saved to: ${outputPath}`);
        console.log(`📊 Summary:`);
        console.log(`   - Total Issues: ${analyses.length}`);
        console.log(`   - High Priority: ${analyses.filter(a => a.analysis.relevance === 'high').length}`);
        console.log(`   - Medium Priority: ${analyses.filter(a => a.analysis.relevance === 'medium').length}`);
        console.log(`   - Low Priority: ${analyses.filter(a => a.analysis.relevance === 'low').length}`);
        console.log(`   - Obsolete: ${analyses.filter(a => a.analysis.relevance === 'obsolete').length}`);
    } catch (error) {
        console.error('❌ Error during analysis:', error);
        process.exit(1);
    }
}

// Run the script
main();
