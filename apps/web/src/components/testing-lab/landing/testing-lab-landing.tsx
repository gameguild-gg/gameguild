import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { TestingLabHero } from './testing-lab-hero';
import { TestingLabStats } from '../management/testing-lab-stats';
import { TestingLabHowItWorks } from './testing-lab-how-it-works';
import { FloatingIcons } from '../common/ui/floating-icons';
import { Button } from '@/components/ui/button';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';

interface TestingLabLandingProps {
  testSessions: TestSession[];
}

export function TestingLabLanding({ testSessions }: TestingLabLandingProps) {
  const openSessions = testSessions.filter((session) => session.status === 'open');
  const upcomingSessions = testSessions.filter((session) => new Date(session.sessionDate) > new Date());

  return (
    <div className="flex flex-col flex-1 relative">
      <FloatingIcons />
      <div className="container mx-auto px-4 py-8 relative">
        {/* Navigation */}
        <div className="flex items-center gap-4 mb-8">
          <Button
            asChild
            variant="ghost"
            className="bg-slate-900/20 backdrop-blur-md border border-slate-700/50 text-slate-200 hover:text-white hover:bg-slate-800/30 hover:border-slate-600/50 transition-all duration-200"
          >
            <Link href="/apps/web/public">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Home
            </Link>
          </Button>
        </div>
      </div>
      <main className="flex flex-col flex-1 items-center justify-center relative z-10">
        <div className="container">
          {/* Hero Section */}
          <TestingLabHero />

          {/* Statistics Section */}
          <TestingLabStats totalSessions={testSessions.length} openSessions={openSessions.length} upcomingSessions={upcomingSessions.length} />

          {/* Learn More Section */}
          <div className="flex flex-col items-center mt-24 mb-12">
            <div className="flex items-center gap-6 mb-8">
              <div className="h-px bg-gradient-to-r from-transparent via-slate-600 to-transparent w-32"></div>
              <span className="text-slate-400 text-base font-medium">curious how it all works?</span>
              <div className="h-px bg-gradient-to-r from-transparent via-slate-600 to-transparent w-32"></div>
            </div>
            <Button
              asChild
              size="lg"
              variant="outline"
              className="bg-slate-900/20 backdrop-blur-md border border-slate-700/50 text-slate-200 hover:text-white hover:bg-slate-800/30 hover:border-slate-600/50 px-8 py-4 text-lg transition-all duration-200"
            >
              <Link href="#learn-more">Learn More</Link>
            </Button>
          </div>

          {/* How It Works Section */}
        </div>
        <div id="learn-more">
          <TestingLabHowItWorks />
        </div>
      </main>
    </div>
  );
}
