import { LearnerGradebook } from '@/components/learner-records';
import { getMyLearnerRecords } from '@/lib/learner-data';

export default async function GradesPage() {
    return <LearnerGradebook records={await getMyLearnerRecords()} />;
}