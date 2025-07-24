import { TestingLabLanding } from '@/components/testing-lab/testing-lab-landing';
import { getAvailableTestSessions } from '@/lib/api/testing-lab/test-sessions';

export default async function TestingLabPage() {
  // Fetch available test sessions server-side
  const testSessions = await getAvailableTestSessions();

  return <TestingLabLanding testSessions={testSessions} />;
}
