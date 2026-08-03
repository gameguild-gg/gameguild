import { getMyLearnerRecords } from '@/lib/learner/records';
import { LearnerActivityCenter } from '@game-guild/courses/components/learner';

export default async function ActivitiesPage() {
  return <LearnerActivityCenter records={await getMyLearnerRecords()} />;
}
