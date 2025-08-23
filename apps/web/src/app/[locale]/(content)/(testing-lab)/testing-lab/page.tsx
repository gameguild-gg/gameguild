import {TestingLabLandingSection} from '@/components/testing-lab/landing/testing-lab-landing-section';
import {getAvailableTestSessions} from '@/lib/admin';

export default async function Page() {
  // Fetch available test sessions server-side
  const testSessions = await getAvailableTestSessions();

  return <TestingLabLandingSection testSessions={testSessions}/>;
}
