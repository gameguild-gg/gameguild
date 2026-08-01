import { getMyLearnerRecords } from '@/lib/learner/records';
import { LearnerGradebook } from '@game-guild/courses/components/learner';

export default async function GradesPage() {
  return <LearnerGradebook records={await getMyLearnerRecords()} />;
}
