import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'Courses | Game Guild',
  description:
    'Explore our comprehensive collection of game development courses. Learn Unity, Unreal Engine, programming, design, and more with hands-on projects.',
  keywords: ['game development', 'courses', 'unity', 'unreal engine', 'programming', 'game design'],
  openGraph: {
    title: 'Game Development Courses | Game Guild',
    description: 'Explore our comprehensive collection of game development courses',
    type: 'website',
  },
};

export default function CoursesLayout({ children }: { children: React.ReactNode }) {
  return <>{children}</>;
}
