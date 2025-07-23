import { auth } from '@/auth';
import { notFound } from 'next/navigation';
import { TestingRequestDetails } from '@/components/testing-lab/testing-request-details';
import {
  getTestingRequestById,
  getTestingRequestParticipants,
  getTestingRequestFeedback,
  getTestingRequestStatistics,
} from '@/lib/testing-lab/testing-lab.actions';
import { Alert, AlertDescription } from '@/components/ui/alert';

interface TestingRequestPageProps {
  params: Promise<{
    id: string;
  }>;
}

export default async function TestingRequestPage({ params }: TestingRequestPageProps) {
  const session = await auth();
  const { id } = await params;

  if (!session?.accessToken) {
    return (
      <div className="container mx-auto p-6">
        <Alert variant="destructive">
          <AlertDescription>Authentication required to view testing request details</AlertDescription>
        </Alert>
      </div>
    );
  }

  try {
    const [request, participants, feedback, statistics] = await Promise.all([
      getTestingRequestById(id),
      getTestingRequestParticipants(id).catch(() => []),
      getTestingRequestFeedback(id).catch(() => []),
      getTestingRequestStatistics(id).catch(() => null),
    ]);

    if (!request) {
      notFound();
    }

    return <TestingRequestDetails request={request} participants={participants} feedback={feedback} statistics={statistics} />;
  } catch (error) {
    console.error('Error loading testing request:', error);
    return (
      <div className="container mx-auto p-6">
        <Alert variant="destructive">
          <AlertDescription>Failed to load testing request details</AlertDescription>
        </Alert>
      </div>
    );
  }
}
