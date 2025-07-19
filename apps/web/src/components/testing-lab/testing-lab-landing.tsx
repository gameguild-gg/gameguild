import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { TestingLabHero } from './testing-lab-hero';
import { TestingLabStats } from './testing-lab-stats';
import { FloatingIcons } from './floating-icons';
import { Button } from '@/components/ui/button';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { TestingLabCallToAction } from './testing-lab-call-to-action';

interface TestingLabLandingProps {
  testSessions: TestSession[];
}

export function TestingLabLanding({ testSessions }: TestingLabLandingProps) {
  const openSessions = testSessions.filter((session) => session.status === 'open');
  const upcomingSessions = testSessions.filter((session) => new Date(session.sessionDate) > new Date());

  return (
    <div className="flex flex-col flex-1 relative">
      <FloatingIcons />
      <div className="container mx-auto px-4 py-8 relative z-10">
        {/* Navigation */}
        <div className="flex items-center gap-4 mb-8">
          <Button
            asChild
            variant="ghost"
            className="bg-slate-900/20 backdrop-blur-md border border-slate-700/50 text-slate-200 hover:text-white hover:bg-slate-800/30 hover:border-slate-600/50 transition-all duration-200"
          >
            <Link href="/">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Home
            </Link>
          </Button>
        </div>
      </div>
      <main className="flex flex-col flex-1 items-center justify-center relative z-10">
        <div className="container ">
          {/* Hero Section */}
          <TestingLabHero />

          {/* Statistics Section */}
          <TestingLabStats totalSessions={testSessions.length} openSessions={openSessions.length} upcomingSessions={upcomingSessions.length} />

          {/* Learn More Section */}
          <div className="flex flex-col items-center mt-16 mb-8">
            <div className="flex items-center gap-4 mb-4">
              <div className="h-px bg-gradient-to-r from-transparent via-slate-600 to-transparent w-64"></div>
              <span className="text-slate-400 text-sm">or</span>
              <div className="h-px bg-gradient-to-r from-transparent via-slate-600 to-transparent w-64"></div>
            </div>
            <Button
              size="lg"
              variant="outline"
              className="bg-slate-900/20 backdrop-blur-md border border-slate-700/50 text-slate-200 hover:text-white hover:bg-slate-800/30 hover:border-slate-600/50 px-8 py-4 text-lg transition-all duration-200"
            >
              Learn More
            </Button>
          </div>

          {/* Call to Action */}
          {/* <TestingLabCallToAction /> */}
        </div>
      </main>
    </div>
  );
}
