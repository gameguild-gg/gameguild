import {getAvailableTestSessions, SESSION_STATUS} from '@/lib/admin';
import {FloatingIcons} from "@/components/testing-lab/common/ui/floating-icons";
import {TestingLabHero, TestingLabHowItWorks, TestingLabStats} from "@/components/testing-lab";
import {TestingLabLearnMore} from "@/components/testing-lab/landing/testing-lab-learn-more";

export default async function Page() {
  // Fetch available test sessions server-side
  const testSessions = await getAvailableTestSessions();

  // Filter sessions by status using constants
  const openSessions = testSessions.filter((session) => session.status === SESSION_STATUS.SCHEDULED || session.status === SESSION_STATUS.ACTIVE);
  const upcomingSessions = testSessions.filter((session) => new Date(session.sessionDate) > new Date());

  return (
    <div className="flex flex-col flex-1 relative">
      <FloatingIcons/>
      <div className="flex flex-col flex-1 items-center justify-center relative">
        <main className="flex flex-col flex-1 items-center justify-center container">
          {/* Hero Section */}
          <TestingLabHero/>
          {/* Statistics Section */}
          <TestingLabStats totalSessions={testSessions.length} openSessions={openSessions.length}
                           upcomingSessions={upcomingSessions.length}/>
          {/* Learn More Section */}
          <TestingLabLearnMore/>
        </main>
        <aside id="learn-more" className="flex flex-col flex-1 items-center justify-center container">
          {/* How It Works Section */}
          <TestingLabHowItWorks/>
        </aside>
      </div>
    </div>
  );
}
