'use client';

import { TestSession } from '@/lib/admin';
import { TestSessionCard } from './test-session-card';

interface TestSessionGridProps {
  sessions: TestSession[];
}

export function TestSessionGrid({ sessions }: TestSessionGridProps) {
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {sessions.map((session) => (
        <TestSessionCard key={session.id} session={session} />
      ))}
    </div>
  );
}
