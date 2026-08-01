export type LearnerContentStatus =
  "locked" | "available" | "in-progress" | "completed";

export interface LearnerContentItem {
  id: string;
  title: string;
  type: "lesson" | "activity" | "quiz" | "assignment" | "peer-review";
  status: LearnerContentStatus;
  duration?: number;
  description?: string;
  order: number;
  isRequired: boolean;
  content?: unknown;
  contentType?: string | null;
  lessonFormat?: string | null;
  activitySettings?: unknown;
  maxPoints?: number;
  gradingMethod?: string | null;
}

export interface LearnerCourseModule {
  id: string;
  title: string;
  description: string;
  order: number;
  items: LearnerContentItem[];
  progress: number;
}

export interface LearnerCourse {
  id: string;
  title: string;
  slug: string;
  description: string;
  thumbnail: string | null;
  modules: LearnerCourseModule[];
  overallProgress: number;
  totalItems: number;
  completedItems: number;
  currentItem?: LearnerContentItem;
  remainingMinutes: number;
  enrollmentId?: string;
}

export interface LearnerCohort {
  id?: string | null;
  name?: string | null;
  startDate?: string | null;
  endDate?: string | null;
  instructorId?: string | null;
}

export interface LearnerCalendarEntry {
  itemId?: string | null;
  title?: string | null;
  type?: string | null;
  itemType?: string | null;
  startsAt?: string | null;
  endsAt?: string | null;
  dueAt?: string | null;
  availableFrom?: string | null;
  cohortId?: string | null;
  cohortName?: string | null;
  status?: string | null;
}

export interface LearnerAssessment {
  id?: string | null;
  courseId?: string | null;
  title?: string | null;
  description?: string | null;
  type?: string | null;
  dueAt?: string | null;
  maxScore?: number | null;
  passingScore?: number | null;
  isAvailable?: boolean | null;
  submissionModalities?: string | null;
  assessmentGroupName?: string | null;
}

export interface LearnerSubmission {
  id?: string | null;
  assessmentId?: string | null;
  enrollmentId?: string | null;
  status?: string | null;
  score?: number | null;
  passed?: boolean | null;
  feedback?: string | null;
}

export interface LearnerDiscussion {
  id?: string | null;
  title?: string | null;
}

export interface LearnerCertificate {
  id?: string | null;
  courseId?: string | null;
  courseName?: string | null;
  certificateNumber?: string | null;
  issuedAt?: string | null;
  status?: string | null;
  verificationUrl?: string | null;
}

export interface LearnerCourseContext {
  enrollmentId: string | null;
  cohort: LearnerCohort | null;
  calendar: LearnerCalendarEntry[];
  assessments: LearnerAssessment[];
  submissions: LearnerSubmission[];
  discussions: LearnerDiscussion[];
  certificates: LearnerCertificate[];
}

export interface LearnerCourseRecord {
  course: LearnerCourse;
  context: LearnerCourseContext;
}

export interface LearnerRoutes {
  catalog: string;
  course: (slug: string) => string;
  content: (slug: string) => string;
  activities: (slug: string) => string;
  activity: (slug: string, activityId: string) => string;
  community: (slug: string) => string;
}

export const defaultLearnerRoutes: LearnerRoutes = {
  catalog: "/catalog",
  course: (slug) => `/courses/${slug}`,
  content: (slug) => `/courses/${slug}/content`,
  activities: (slug) => `/courses/${slug}/activities`,
  activity: (slug, activityId) => `/courses/${slug}/activities/${activityId}`,
  community: (slug) => `/courses/${slug}/community`,
};
