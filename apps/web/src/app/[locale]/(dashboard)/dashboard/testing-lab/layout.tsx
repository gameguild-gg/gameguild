import type { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Testing Lab | Game Guild',
  description: 'Submit, test, and manage game projects for Capstone teams',
};

export default function TestingLabLayout({ children }: { children: React.ReactNode }) {
  return children;
}
