import { TestingLabNav } from '@/components/testing-lab/testing-lab-nav';
import type { ReactNode } from 'react';

export default function TestingLabLayout({ children }: { children: ReactNode }) {
  return (
    <div className="min-w-0">
      <TestingLabNav />
      {children}
    </div>
  );
}
