import { gql } from '@apollo/client';

// Enrollment Mutations
export const ENROLL_IN_PROGRAM = gql`
  mutation EnrollInProgram($programId: String!) {
    enrollInProgram(programId: $programId) {
      id
      programId
      userId
      enrolledAt
      enrollmentSource
      status
      progress
    }
  }
`;

export const COMPLETE_CONTENT = gql`
  mutation CompleteContent($input: CompleteContentInput!) {
    completeContent(input: $input) {
      id
      contentId
      userId
      status
      progress
      completedAt
      timeSpent
    }
  }
`;

export const UPDATE_CONTENT_PROGRESS = gql`
  mutation UpdateContentProgress($input: UpdateContentProgressInput!) {
    updateContentProgress(input: $input) {
      id
      contentId
      userId
      status
      progress
      timeSpent
      lastAccessedAt
    }
  }
`;

// Query for course catalog
export const GET_COURSES = gql`
  query GetCourses($filters: CourseFilters) {
    programs(filters: $filters) {
      id
      title
      description
      estimatedHours
      instructor
      difficulty
      category
      tags
      rating
      enrollmentCount
      price
      currency
      isEnrolled
      progress
      hasCertificate
      lastUpdated
      contentCount
      isPremium
      thumbnail
    }
  }
`;

// Query for enrolled courses
export const GET_MY_COURSES = gql`
  query GetMyCourses {
    myEnrollments {
      id
      programId
      enrolledAt
      status
      progress
      certificateIssued
      program {
        id
        title
        description
        estimatedHours
        instructor
        thumbnail
        hasCertificate
      }
    }
  }
`;

// Query for course details
export const GET_COURSE_DETAILS = gql`
  query GetCourseDetails($id: String!) {
    program(id: $id) {
      id
      title
      description
      estimatedHours
      instructor
      difficulty
      category
      tags
      rating
      enrollmentCount
      price
      currency
      isEnrolled
      progress
      hasCertificate
      thumbnail
      contents {
        id
        title
        type
        duration
        isRequired
        isCompleted
        isLocked
        progress
        description
        score
        maxScore
        order
      }
      enrollment {
        id
        enrolledAt
        status
        progress
        certificateIssued
      }
    }
  }
`;

// Query for content progress
export const GET_CONTENT_PROGRESS = gql`
  query GetContentProgress($programId: String!) {
    contentProgress(programId: $programId) {
      id
      contentId
      userId
      status
      progress
      score
      maxScore
      attempts
      timeSpent
      firstAccessedAt
      lastAccessedAt
      completedAt
      content {
        id
        title
        type
        duration
        isRequired
      }
    }
  }
`;

// Subscription for real-time progress updates
export const PROGRESS_UPDATED = gql`
  subscription ProgressUpdated($userId: String!) {
    progressUpdated(userId: $userId) {
      contentId
      programId
      status
      progress
      completedAt
    }
  }
`;

// Types
export interface Course {
  id: string;
  title: string;
  description: string;
  instructor: string;
  instructorAvatar?: string;
  duration: number;
  level: 'beginner' | 'intermediate' | 'advanced';
  category: string;
  tags: string[];
  rating: number;
  enrollmentCount: number;
  price: number;
  currency: string;
  thumbnail: string;
  isEnrolled: boolean;
  progress?: number;
  hasCertificate: boolean;
  lastUpdated: string;
  contentCount: number;
  isPremium: boolean;
}

export interface ContentItem {
  id: string;
  title: string;
  type: 'page' | 'video' | 'assignment' | 'quiz' | 'discussion' | 'code' | 'project';
  duration: number;
  isRequired: boolean;
  isCompleted: boolean;
  isLocked: boolean;
  progress: number;
  description?: string;
  score?: number;
  maxScore?: number;
  order: number;
}

export interface CourseContent {
  id: string;
  title: string;
  description: string;
  estimatedHours: number;
  instructor: string;
  progress: number;
  isEnrolled: boolean;
  certificate: boolean;
  contents: ContentItem[];
}

export interface ProgramEnrollment {
  id: string;
  programId: string;
  userId: string;
  enrolledAt: string;
  enrollmentSource: string;
  status: string;
  progress: number;
  certificateIssued: boolean;
}

export interface ContentProgress {
  id: string;
  contentId: string;
  userId: string;
  status: string;
  progress: number;
  score?: number;
  maxScore?: number;
  attempts: number;
  timeSpent: number;
  firstAccessedAt: string;
  lastAccessedAt: string;
  completedAt?: string;
}
