import '@testing-library/jest-dom/vitest';
import { render, screen, within } from '@testing-library/react';
import type { AnchorHTMLAttributes, ReactNode } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { AssessmentsList } from './assessments-list';
import type { Assessment } from '@/lib/learning/queries/assessments';

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: AnchorHTMLAttributes<HTMLAnchorElement> & { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
  usePathname: () => '/dashboard/learning/courses/course-1/assessments',
  useRouter: () => ({ refresh: vi.fn() }),
}));

vi.mock('@/lib/learning/actions', () => ({
  createAssessment: vi.fn(),
}));

const groupedAssessments = [
  {
    id: 'quiz-1',
    courseId: 'course-1',
    contentId: null,
    title: 'Schema Patterns',
    description: null,
    type: 'Quiz',
    maxScore: 10,
    passingScore: 7,
    timeLimitMinutes: null,
    maxAttempts: null,
    isRequired: true,
    order: 1,
    availableFrom: null,
    availableUntil: null,
    isAvailable: true,
    assessmentGroupId: 'group-quizzes',
    assessmentGroupName: 'Weekly quizzes',
    assessmentGroupWeightPercent: 30,
    assessmentGroupOrder: 1,
  },
  {
    id: 'project-1',
    courseId: 'course-1',
    contentId: null,
    title: 'Final project proposal',
    description: null,
    type: 'Project',
    maxScore: 40,
    passingScore: 28,
    timeLimitMinutes: null,
    maxAttempts: null,
    isRequired: true,
    order: 1,
    availableFrom: null,
    availableUntil: null,
    isAvailable: true,
    assessmentGroupId: 'group-project',
    assessmentGroupName: 'Final project',
    assessmentGroupWeightPercent: 40,
    assessmentGroupOrder: 2,
  },
] as unknown as Assessment[];

describe('AssessmentsList weighted groups', () => {
  it('renders graded activities inside weighted assessment groups', () => {
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
      />,
    );

    const quizGroup = screen.getByTestId('assessment-group-group-quizzes');
    expect(within(quizGroup).getByRole('heading', { name: /weekly quizzes/i })).toBeInTheDocument();
    expect(within(quizGroup).getByText('30% of Total')).toBeInTheDocument();
    expect(within(quizGroup).getByRole('link', { name: /schema patterns/i })).toBeInTheDocument();

    const projectGroup = screen.getByTestId('assessment-group-group-project');
    expect(within(projectGroup).getByRole('heading', { name: /final project/i })).toBeInTheDocument();
    expect(within(projectGroup).getByText('40% of Total')).toBeInTheDocument();
    expect(within(projectGroup).getByRole('link', { name: /final project proposal/i })).toBeInTheDocument();
  });
});
