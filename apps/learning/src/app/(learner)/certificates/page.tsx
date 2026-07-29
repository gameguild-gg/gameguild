import { LearnerCertificates } from '@/components/learner-records';
import { getMyCertificates } from '@/lib/learner-data';

export default async function CertificatesPage() {
    return <LearnerCertificates certificates={await getMyCertificates()} />;
}