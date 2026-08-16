import { getMyLearningCourses } from '@/lib/learner/courses';
import { getCourseLearnerContext } from '@/lib/learner/records';
import { getMyTasks } from '@/lib/learning';
import { fetchReceivedPeerReviews } from '@/lib/learning/actions-peer-review';
import type { LearningAssessmentsReceivedPeerReview } from '@game-guild/client';
import { PeerReviewsPage } from './reviews-client';

export interface ReceivedFeedbackGroup {
  assessmentId: string;
  assessmentTitle: string;
  courseTitle: string;
  reviews: LearningAssessmentsReceivedPeerReview[];
}

async function loadReceivedFeedback(courseIds: { id: string; title: string }[]): Promise<ReceivedFeedbackGroup[]> {
  const perCourse = await Promise.all(
    courseIds.map(async (course) => {
      const context = await getCourseLearnerContext(course.id);
      const titleById = new Map(context.assessments.filter((a) => a.id).map((a) => [a.id!, a.title ?? 'Untitled assessment']));
      const perSubmission = await Promise.all(
        context.submissions
          .filter((submission) => submission.id)
          .map(async (submission) => {
            const result = await fetchReceivedPeerReviews(submission.id!);
            if (!result.ok || result.reviews.length === 0) return null;
            return {
              assessmentId: submission.assessmentId ?? '',
              assessmentTitle: titleById.get(submission.assessmentId ?? '') ?? 'Untitled assessment',
              courseTitle: course.title,
              reviews: result.reviews,
            };
          }),
      );
      return perSubmission.filter((group): group is ReceivedFeedbackGroup => group !== null);
    }),
  );
  return perCourse.flat();
}

export default async function LearnReviewsPage() {
  const [tasksResult, courses] = await Promise.all([getMyTasks(), getMyLearningCourses()]);
  const reviewTasks = tasksResult.ok ? tasksResult.tasks.filter((task) => task.type === 'review') : [];
  const received = await loadReceivedFeedback(courses);

  return <PeerReviewsPage reviewTasks={reviewTasks} received={received} />;
}
