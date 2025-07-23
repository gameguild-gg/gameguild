import { auth } from '@/auth';
import { EnhancedTestingSessionsList } from '@/components/testing-lab/enhanced-testing-sessions-list';
import { getTestingSessionsData } from '@/lib/testing-lab/testing-lab.actions';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Loader2 } from 'lucide-react';

export default async function TestingSessionsPage() {
  const session = await auth();

  if (!session?.accessToken) {
    return (
      <div className="container mx-auto p-6">
        <div className="flex items-center justify-center min-h-64">
          <div className="text-center">
            <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4" />
            <p className="text-gray-600">Loading authentication...</p>
          </div>
        </div>
      </div>
    );
  }

  let sessions = [];
  let error: string | null = null;

  try {
    const sessionsData = await getTestingSessionsData();
    sessions = sessionsData?.testingSessions || [];
  } catch (err) {
    error = err instanceof Error ? err.message : 'Failed to load testing sessions';
  }

  if (error) {
    return (
      <div className="container mx-auto p-6">
        <Alert variant="destructive">
          <AlertDescription>Failed to load testing sessions: {error}</AlertDescription>
        </Alert>
      </div>
    );
  }

  return <EnhancedTestingSessionsList initialSessions={sessions} />;
}
