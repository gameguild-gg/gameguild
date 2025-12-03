'use server';

/**
 * Stub server actions for courses module.
 * These are placeholder implementations for disabled backend functionality.
 */

interface SubmitActivityData {
    activityId: string;
    activityType: string;
    content: Record<string, unknown>;
    isGraded: boolean;
    attempt: number;
    submissionData?: unknown;
}

interface SubmitActivityResult {
    success: boolean;
    message?: string;
    score?: number;
    feedback?: string;
    submission?: {
        id: string;
        status: string;
    };
}

export async function submitActivity(
    data: SubmitActivityData
): Promise<SubmitActivityResult> {
    // Stub implementation - always returns success
    console.log('[STUB] submitActivity called with:', data);
    return {
        success: true,
        message: 'Activity submitted successfully (stub)',
        submission: {
            id: 'stub-submission-id',
            status: 'submitted',
        },
    };
}

interface CourseProgress {
    courseId: string;
    courseTitle: string;
    totalItems: number;
    completedItems: number;
    percentComplete: number;
    progressPercentage: number;
    currentStreak: number;
    timeSpent: number;
    items: Array<{
        id: string;
        title: string;
        type: 'lesson' | 'activity' | 'quiz' | 'assignment' | 'peer-review';
        status: 'not-started' | 'in-progress' | 'completed' | 'graded';
        completedAt?: string;
        grade?: number;
        required: boolean;
        estimatedMinutes?: number;
    }>;
    nextItem?: {
        id: string;
        title: string;
        type: 'lesson' | 'activity' | 'quiz' | 'assignment' | 'peer-review';
        status: 'not-started' | 'in-progress' | 'completed' | 'graded';
        completedAt?: string;
        grade?: number;
        required: boolean;
        estimatedMinutes?: number;
    };
    estimatedTimeToComplete: number;
    certificateEligible: boolean;
    lastAccessedAt?: string;
    modules: Array<{
        id: string;
        title: string;
        progress: number;
    }>;
}

export async function getCourseProgress(_courseId: string): Promise<CourseProgress> {
    // Stub implementation
    return {
        courseId: _courseId,
        courseTitle: 'Stub Course',
        totalItems: 0,
        completedItems: 0,
        percentComplete: 0,
        progressPercentage: 0,
        currentStreak: 0,
        timeSpent: 0,
        items: [],
        estimatedTimeToComplete: 0,
        certificateEligible: false,
        modules: [],
    };
}

export async function updateCourseProgress(
    _courseId: string,
    _contentId: string,
    _progress: number
): Promise<{ success: boolean }> {
    // Stub implementation
    return { success: true };
}

export async function markContentComplete(
    _courseId: string,
    _contentId: string
): Promise<{ success: boolean }> {
    // Stub implementation
    return { success: true };
}
