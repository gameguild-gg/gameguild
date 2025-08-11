'use client';

import { TestingLabOverview } from '@/components/testing-lab/overview/testing-lab-overview';

export function TestingLabPage() {
  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-8">
        <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-2">Testing Lab</h1>
        <p className="text-slate-400 text-lg">Participate in game testing sessions, provide feedback, and help improve games in development.</p>
      </div>

      <TestingLabOverview />
    </div>
  );
}
