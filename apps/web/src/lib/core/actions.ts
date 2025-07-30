'use server';

import fs from 'fs';
import path from 'path';

export async function getVersion() {
  try {
    let version = 'v0.0.1';
    try {
      const gitStatsPath = path.resolve(process.cwd(), 'git-stats.json');
      if (fs.existsSync(gitStatsPath)) {
        const gitStatsContent = fs.readFileSync(gitStatsPath, 'utf-8');
        const gitStats = JSON.parse(gitStatsContent);
        version = gitStats.version;
      }
    } catch (fsError) {
      console.error('Error reading git-stats.json from filesystem:', fsError);
    }
    return { version };
  } catch (error) {
    console.error('Error in version API:', error);
    return { version: 'v0.0.1', error: 'Failed to fetch version' };
  }
}

interface ReportRequest {
  reportType: 'course' | 'content' | 'review';
  targetId: string;
  reason: string;
  description?: string;
  targetTitle?: string;
}

export async function submitReport(reportData: ReportRequest) {
  const { auth } = await import('@/auth');
  try {
    const session = await auth();
    if (!session?.user) {
      throw new Error('Authentication required');
    }
    if (!reportData.reportType || !reportData.targetId || !reportData.reason) {
      throw new Error('Missing required fields');
    }
    console.log('Report submitted:', {
      userId: session.user.id,
      reportType: reportData.reportType,
      targetId: reportData.targetId,
      reason: reportData.reason,
      timestamp: new Date().toISOString(),
    });
    const mockReport = {
      id: `report_${Date.now()}`,
      status: 'pending',
      reportType: reportData.reportType,
      targetId: reportData.targetId,
      reason: reportData.reason,
      description: reportData.description,
      targetTitle: reportData.targetTitle,
      reportedBy: session.user.id,
      submittedAt: new Date().toISOString(),
    };
    return {
      success: true,
      report: mockReport,
      message: 'Report submitted successfully.',
    };
  } catch (error) {
    console.error('Error processing report:', error);
    throw new Error(error instanceof Error ? error.message : 'Internal server error');
  }
}

export async function testCreateProject(projectData: any) {
  const { auth } = await import('@/auth');
  const { createProject } = await import('@/components/legacy/projects/actions');
  try {
    const session = await auth();
    if (!session || !session.accessToken) {
      throw new Error('Authentication required');
    }
    const project = await createProject(projectData);
    return project;
  } catch (error) {
    console.error('Test create project error:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create project');
  }
}
