'use client';

import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { TestSessionGrid } from '../test-session-grid';
import { TestSessionRow } from '../test-session-row';
import { TestSessionTable } from '../test-session-table';

interface SessionContentProps {
  sessions: TestSession[];
  viewMode: 'cards' | 'row' | 'table';
}

export function SessionContent({ sessions, viewMode }: SessionContentProps) {
  return (
    <section className="mb-12">
      {viewMode === 'cards' && <TestSessionGrid sessions={sessions} />}
      {viewMode === 'row' && <TestSessionRow sessions={sessions} />}
      {viewMode === 'table' && <TestSessionTable sessions={sessions} />}
    </section>
  );
}
