import { NextRequest, NextResponse } from 'next/server';

interface ProgressItem {
  id: string;
  title: string;
  type: 'lesson' | 'activity' | 'quiz' | 'assignment' | 'peer-review';
  status: 'not-started' | 'in-progress' | 'completed' | 'graded';
  completedAt?: string;
  grade?: number;
  required: boolean;
  estimatedMinutes?: number;
}

interface ProgressData {
  courseId: string;
  courseTitle: string;
  totalItems: number;
  completedItems: number;
  progressPercentage: number;
  currentStreak: number;
  timeSpent: number;
  items: ProgressItem[];
  nextItem?: ProgressItem;
  estimatedTimeToComplete: number;
  certificateEligible: boolean;
}

export async function GET(
  request: NextRequest,
  { params }: { params: { courseId: string } }
) {
  try {
    const { courseId } = params;

    if (!courseId) {
      return NextResponse.json(
        { error: 'Course ID is required' },
        { status: 400 }
      );
    }

    // In a real implementation, fetch from database
    // const progress = await getStudentProgress(courseId, studentId);

    // Mock progress data
    const mockItems: ProgressItem[] = [
      {
        id: '1',
        title: 'Introduction to Game Development',
        type: 'lesson',
        status: 'completed',
        completedAt: '2024-01-15T10:00:00Z',
        required: true,
        estimatedMinutes: 30,
      },
      {
        id: '2',
        title: 'Setting Up Your Development Environment',
        type: 'activity',
        status: 'completed',
        completedAt: '2024-01-16T14:30:00Z',
        grade: 95,
        required: true,
        estimatedMinutes: 45,
      },
      {
        id: '3',
        title: 'Basic Programming Concepts Quiz',
        type: 'quiz',
        status: 'completed',
        completedAt: '2024-01-17T09:15:00Z',
        grade: 88,
        required: true,
        estimatedMinutes: 20,
      },
      {
        id: '4',
        title: 'Creating Your First Game Object',
        type: 'lesson',
        status: 'in-progress',
        required: true,
        estimatedMinutes: 40,
      },
      {
        id: '5',
        title: 'Game Physics Implementation',
        type: 'assignment',
        status: 'not-started',
        required: true,
        estimatedMinutes: 120,
      },
      {
        id: '6',
        title: 'Peer Review: Game Design Document',
        type: 'peer-review',
        status: 'not-started',
        required: true,
        estimatedMinutes: 60,
      },
      {
        id: '7',
        title: 'Advanced Scripting Techniques',
        type: 'lesson',
        status: 'not-started',
        required: false,
        estimatedMinutes: 35,
      },
      {
        id: '8',
        title: 'Final Project Submission',
        type: 'assignment',
        status: 'not-started',
        required: true,
        estimatedMinutes: 180,
      },
    ];

    const completedItems = mockItems.filter(item => 
      item.status === 'completed' || item.status === 'graded'
    ).length;
    
    const totalItems = mockItems.length;
    const progressPercentage = Math.round((completedItems / totalItems) * 100);
    
    const nextItem = mockItems.find(item => 
      item.status === 'in-progress' || item.status === 'not-started'
    );

    const requiredItems = mockItems.filter(item => item.required);
    const completedRequiredItems = requiredItems.filter(item => 
      item.status === 'completed' || item.status === 'graded'
    ).length;
    
    const certificateEligible = completedRequiredItems === requiredItems.length;

    const remainingItems = mockItems.filter(item => 
      item.status === 'not-started' || item.status === 'in-progress'
    );
    
    const estimatedTimeToComplete = remainingItems.reduce((total, item) => 
      total + (item.estimatedMinutes || 0), 0
    ) / 60; // Convert to hours

    const progressData: ProgressData = {
      courseId,
      courseTitle: 'Complete Game Development Course',
      totalItems,
      completedItems,
      progressPercentage,
      currentStreak: 7, // Mock streak data
      timeSpent: 320, // Mock time spent in minutes
      items: mockItems,
      nextItem,
      estimatedTimeToComplete: Math.round(estimatedTimeToComplete),
      certificateEligible,
    };

    return NextResponse.json(progressData);

  } catch (error) {
    console.error('Error fetching progress:', error);
    return NextResponse.json(
      { error: 'Failed to fetch progress data' },
      { status: 500 }
    );
  }
}

export async function POST(
  request: NextRequest,
  { params }: { params: { courseId: string } }
) {
  try {
    const { courseId } = params;
    const body = await request.json();
    const { itemId, status, grade, timeSpent } = body;

    if (!courseId || !itemId || !status) {
      return NextResponse.json(
        { error: 'Missing required fields' },
        { status: 400 }
      );
    }

    // In a real implementation, update progress in database
    // await updateStudentProgress({
    //   courseId,
    //   studentId,
    //   itemId,
    //   status,
    //   grade,
    //   timeSpent,
    //   completedAt: status === 'completed' ? new Date() : undefined
    // });

    // Mock update
    console.log('Updating progress:', {
      courseId,
      itemId,
      status,
      grade,
      timeSpent,
    });

    return NextResponse.json({
      success: true,
      message: 'Progress updated successfully',
    });

  } catch (error) {
    console.error('Error updating progress:', error);
    return NextResponse.json(
      { error: 'Failed to update progress' },
      { status: 500 }
    );
  }
}

// Helper function to calculate streak (mock implementation)
function calculateCurrentStreak(): number {
  // In a real implementation, calculate based on daily activity
  // const activities = await getRecentStudentActivities(studentId, 30);
  // return calculateStreakFromActivities(activities);
  return Math.floor(Math.random() * 14) + 1; // Mock streak 1-14 days
}

// Helper function to get time spent (mock implementation)
function getTotalTimeSpent(): number {
  // In a real implementation, sum up actual time tracking data
  // const timeEntries = await getStudentTimeEntries(studentId, courseId);
  // return timeEntries.reduce((total, entry) => total + entry.minutes, 0);
  return Math.floor(Math.random() * 500) + 100; // Mock time 100-600 minutes
}
