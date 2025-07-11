import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Learning Tracks | Game Guild',
  description: 'Follow structured learning paths to master game development. Our tracks guide you from beginner to expert with comprehensive curricula.',
  keywords: ['learning tracks', 'game development', 'structured learning', 'curriculum', 'beginner to expert'],
  openGraph: {
    title: 'Game Development Learning Tracks | Game Guild',
    description: 'Follow structured learning paths to master game development',
    type: 'website',
  },
};

export default function TracksLayout({ children }: { children: React.ReactNode }) {
  return <>{children}</>;
}
