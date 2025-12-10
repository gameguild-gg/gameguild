'use client';

import { TestingSession } from '@/lib/api/generated';
import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { TestSessionGrid } from './test-session-grid';
import { TestSessionRow } from './test-session-row';
import { TestSessionTable } from './test-session-table';

interface SessionContentProps {
  sessions: (TestSession | TestingSession)[];
  viewMode: 'cards' | 'row' | 'table';
}

export function SessionContent({ sessions, viewMode }: SessionContentProps) {
  // Cast to TestSession[] for compatibility - the grid/row/table components handle the data
  const sessionsAsTestSession = sessions as unknown as TestSession[];

  return (
    <section className="mb-12">
      {viewMode === 'cards' && <TestSessionGrid sessions={sessionsAsTestSession} />}
      {viewMode === 'row' && <TestSessionRow sessions={sessionsAsTestSession} />}
      {viewMode === 'table' && <TestSessionTable sessions={sessionsAsTestSession} />}
    </section>
  );
}
