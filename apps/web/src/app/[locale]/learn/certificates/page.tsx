import { getMyCertificates } from '@/lib/learner/records';
import { LearnerCertificates } from '@game-guild/courses/components/learner';

export default async function CertificatesPage() {
  return <LearnerCertificates certificates={await getMyCertificates()} />;
}
