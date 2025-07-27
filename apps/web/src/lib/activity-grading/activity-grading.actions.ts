'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  // Activity Grading Management
  postApiProgramsByProgramIdActivityGrades,
  getApiProgramsByProgramIdActivityGradesInteractionByContentInteractionId,
  getApiProgramsByProgramIdActivityGradesGraderByGraderProgramUserId,
  getApiProgramsByProgramIdActivityGradesStudentByProgramUserId,
  deleteApiProgramsByProgramIdActivityGradesByGradeId,
  putApiProgramsByProgramIdActivityGradesByGradeId,
  getApiProgramsByProgramIdActivityGradesPending,
  getApiProgramsByProgramIdActivityGradesStatistics,
  getApiProgramsByProgramIdActivityGradesContentByContentId,
} from '@/lib/api/generated/sdk.gen';

// =============================================================================
// ACTIVITY GRADING MANAGEMENT
// =============================================================================

/**
 * Create a new activity grade
 */
export async function createActivityGrade(
  programId: string,
  gradeData: {
    studentProgramUserId: string;
    graderProgramUserId: string;
    contentInteractionId: string;
    score?: number;
    maxScore?: number;
    feedback?: string;
    gradingCriteria?: Record<string, unknown>;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiProgramsByProgramIdActivityGrades({
      path: { programId },
      body: gradeData,
    });

    if (!response.data) {
      throw new Error('Failed to create activity grade');
    }

    revalidateTag('activity-grades');
    revalidateTag('student-grades');
    revalidateTag('grader-activity');
    revalidateTag(`program-${programId}-grades`);
    return response.data;
  } catch (error) {
    console.error('Error creating activity grade:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create activity grade');
  }
}

/**
 * Get grade by content interaction ID
 */
export async function getGradeByContentInteraction(programId: string, contentInteractionId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdActivityGradesInteractionByContentInteractionId({
      path: { programId, contentInteractionId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching grade by content interaction:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch grade by content interaction');
  }
}

/**
 * Get all grades by grader
 */
export async function getGradesByGrader(programId: string, graderProgramUserId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdActivityGradesGraderByGraderProgramUserId({
      path: { programId, graderProgramUserId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching grades by grader:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch grades by grader');
  }
}

/**
 * Get all grades for a student
 */
export async function getGradesByStudent(programId: string, studentProgramUserId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdActivityGradesStudentByProgramUserId({
      path: { programId, programUserId: studentProgramUserId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching grades by student:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch grades by student');
  }
}

/**
 * Update an existing activity grade
 */
export async function updateActivityGrade(
  programId: string,
  gradeId: string,
  gradeData: {
    score?: number;
    maxScore?: number;
    feedback?: string;
    gradingCriteria?: Record<string, unknown>;
    isFinalized?: boolean;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiProgramsByProgramIdActivityGradesByGradeId({
      path: { programId, gradeId },
      body: gradeData,
    });

    if (!response.data) {
      throw new Error('Failed to update activity grade');
    }

    revalidateTag('activity-grades');
    revalidateTag('student-grades');
    revalidateTag('grader-activity');
    revalidateTag(`program-${programId}-grades`);
    revalidateTag(`grade-${gradeId}`);
    return response.data;
  } catch (error) {
    console.error('Error updating activity grade:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update activity grade');
  }
}

/**
 * Delete an activity grade
 */
export async function deleteActivityGrade(programId: string, gradeId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiProgramsByProgramIdActivityGradesByGradeId({
      path: { programId, gradeId },
    });

    revalidateTag('activity-grades');
    revalidateTag('student-grades');
    revalidateTag('grader-activity');
    revalidateTag(`program-${programId}-grades`);
    revalidateTag(`grade-${gradeId}`);
    return response.data;
  } catch (error) {
    console.error('Error deleting activity grade:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to delete activity grade');
  }
}

/**
 * Get pending grades that need grading
 */
export async function getPendingGrades(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdActivityGradesPending({
      path: { programId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching pending grades:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch pending grades');
  }
}

/**
 * Get grading statistics for a program
 */
export async function getGradingStatistics(programId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdActivityGradesStatistics({
      path: { programId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching grading statistics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch grading statistics');
  }
}

/**
 * Get all grades for specific content
 */
export async function getGradesByContent(programId: string, contentId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiProgramsByProgramIdActivityGradesContentByContentId({
      path: { programId, contentId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching grades by content:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch grades by content');
  }
}

// =============================================================================
// COMPREHENSIVE GRADING ANALYTICS
// =============================================================================

/**
 * Get comprehensive grading analytics for a program
 */
export async function getProgramGradingAnalytics(programId: string) {
  await configureAuthenticatedClient();

  try {
    const [statistics, pendingGrades] = await Promise.all([
      getGradingStatistics(programId),
      getPendingGrades(programId),
    ]);

    return {
      statistics,
      pendingGrades,
      summary: {
        programId,
        pendingCount: pendingGrades.length,
        generatedAt: new Date().toISOString(),
      },
    };
  } catch (error) {
    console.error('Error generating program grading analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate program grading analytics');
  }
}

/**
 * Get grader performance analytics
 */
export async function getGraderAnalytics(programId: string, graderProgramUserId: string) {
  await configureAuthenticatedClient();

  try {
    const grades = await getGradesByGrader(programId, graderProgramUserId);

    const analytics = {
      totalGrades: grades.length,
      averageScore: grades.length > 0 ? grades.reduce((sum: number, grade: any) => sum + (grade.score || 0), 0) / grades.length : 0,
      gradingDistribution: {
        excellent: grades.filter((grade: any) => (grade.score || 0) >= 90).length,
        good: grades.filter((grade: any) => (grade.score || 0) >= 80 && (grade.score || 0) < 90).length,
        satisfactory: grades.filter((grade: any) => (grade.score || 0) >= 70 && (grade.score || 0) < 80).length,
        needsImprovement: grades.filter((grade: any) => (grade.score || 0) < 70).length,
      },
      feedbackMetrics: {
        totalWithFeedback: grades.filter((grade: any) => grade.feedback && grade.feedback.trim().length > 0).length,
        averageFeedbackLength: grades.length > 0 
          ? grades.reduce((sum: number, grade: any) => sum + (grade.feedback?.length || 0), 0) / grades.length 
          : 0,
      },
    };

    return {
      analytics,
      grades,
      summary: {
        programId,
        graderProgramUserId,
        gradingEfficiency: analytics.totalGrades > 0 ? (analytics.feedbackMetrics.totalWithFeedback / analytics.totalGrades) * 100 : 0,
        generatedAt: new Date().toISOString(),
      },
    };
  } catch (error) {
    console.error('Error generating grader analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate grader analytics');
  }
}

/**
 * Get student performance analytics
 */
export async function getStudentGradingAnalytics(programId: string, studentProgramUserId: string) {
  await configureAuthenticatedClient();

  try {
    const grades = await getGradesByStudent(programId, studentProgramUserId);

    const analytics = {
      totalGrades: grades.length,
      averageScore: grades.length > 0 ? grades.reduce((sum: number, grade: any) => sum + (grade.score || 0), 0) / grades.length : 0,
      highestScore: grades.length > 0 ? Math.max(...grades.map((grade: any) => grade.score || 0)) : 0,
      lowestScore: grades.length > 0 ? Math.min(...grades.map((grade: any) => grade.score || 0)) : 0,
      improvementTrend: grades.length >= 2 ? calculateImprovementTrend(grades) : null,
      performanceDistribution: {
        excellent: grades.filter((grade: any) => (grade.score || 0) >= 90).length,
        good: grades.filter((grade: any) => (grade.score || 0) >= 80 && (grade.score || 0) < 90).length,
        satisfactory: grades.filter((grade: any) => (grade.score || 0) >= 70 && (grade.score || 0) < 80).length,
        needsImprovement: grades.filter((grade: any) => (grade.score || 0) < 70).length,
      },
    };

    return {
      analytics,
      grades,
      summary: {
        programId,
        studentProgramUserId,
        overallPerformance: analytics.averageScore >= 90 ? 'Excellent' : 
                           analytics.averageScore >= 80 ? 'Good' : 
                           analytics.averageScore >= 70 ? 'Satisfactory' : 'Needs Improvement',
        generatedAt: new Date().toISOString(),
      },
    };
  } catch (error) {
    console.error('Error generating student grading analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate student grading analytics');
  }
}

/**
 * Get content grading analytics
 */
export async function getContentGradingAnalytics(programId: string, contentId: string) {
  await configureAuthenticatedClient();

  try {
    const grades = await getGradesByContent(programId, contentId);

    const analytics = {
      totalSubmissions: grades.length,
      averageScore: grades.length > 0 ? grades.reduce((sum: number, grade: any) => sum + (grade.score || 0), 0) / grades.length : 0,
      scoreDistribution: {
        excellent: grades.filter((grade: any) => (grade.score || 0) >= 90).length,
        good: grades.filter((grade: any) => (grade.score || 0) >= 80 && (grade.score || 0) < 90).length,
        satisfactory: grades.filter((grade: any) => (grade.score || 0) >= 70 && (grade.score || 0) < 80).length,
        needsImprovement: grades.filter((grade: any) => (grade.score || 0) < 70).length,
      },
      difficulty: grades.length > 0 ? (grades.reduce((sum: number, grade: any) => sum + (grade.score || 0), 0) / grades.length) < 75 ? 'High' : 'Moderate' : 'Unknown',
    };

    return {
      analytics,
      grades,
      summary: {
        programId,
        contentId,
        contentDifficulty: analytics.difficulty,
        passRate: grades.length > 0 ? (grades.filter((grade: any) => (grade.score || 0) >= 70).length / grades.length) * 100 : 0,
        generatedAt: new Date().toISOString(),
      },
    };
  } catch (error) {
    console.error('Error generating content grading analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate content grading analytics');
  }
}

// =============================================================================
// HELPER FUNCTIONS
// =============================================================================

/**
 * Calculate improvement trend from grades over time
 */
function calculateImprovementTrend(grades: any[]): 'improving' | 'declining' | 'stable' {
  if (grades.length < 2) return 'stable';
  
  const sortedGrades = grades
    .filter(grade => grade.createdAt && grade.score !== null)
    .sort((a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime());
  
  if (sortedGrades.length < 2) return 'stable';
  
  const firstHalf = sortedGrades.slice(0, Math.ceil(sortedGrades.length / 2));
  const secondHalf = sortedGrades.slice(Math.ceil(sortedGrades.length / 2));
  
  const firstHalfAvg = firstHalf.reduce((sum, grade) => sum + grade.score, 0) / firstHalf.length;
  const secondHalfAvg = secondHalf.reduce((sum, grade) => sum + grade.score, 0) / secondHalf.length;
  
  const difference = secondHalfAvg - firstHalfAvg;
  
  if (difference > 5) return 'improving';
  if (difference < -5) return 'declining';
  return 'stable';
}
