import '@testing-library/jest-dom/vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { AnchorHTMLAttributes, ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AssessmentsList } from './assessments-list';
import { deleteAssessmentGroup, updateAssessmentGroup } from '@/lib/learning/actions';
import type { Assessment, CourseAssessmentAnalytics } from '@/lib/learning/queries/assessments';

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
  createAssessmentGroup: vi.fn(),
  deleteAssessmentGroup: vi.fn(),
  updateAssessmentGroup: vi.fn(),
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

const assessmentGroups = [
  {
    id: 'group-quizzes',
    courseId: 'course-1',
    name: 'Weekly quizzes',
    description: null,
    weightPercent: 30,
    order: 1,
  },
  {
    id: 'group-project',
    courseId: 'course-1',
    name: 'Final project',
    description: null,
    weightPercent: 40,
    order: 2,
  },
];

const analytics = {
  courseId: 'course-1',
  assessmentCount: 2,
  gradedCount: 2,
  ungradedCount: 0,
  averagePercent: 65,
  passRate: 50,
  distribution: [
    { label: '0-59', minPercent: 0, maxPercent: 59, count: 1 },
    { label: '60-69', minPercent: 60, maxPercent: 69, count: 0 },
    { label: '70-79', minPercent: 70, maxPercent: 79, count: 0 },
    { label: '80-89', minPercent: 80, maxPercent: 89, count: 1 },
    { label: '90-100', minPercent: 90, maxPercent: 100, count: 0 },
  ],
  groups: [
    {
      groupId: 'group-quizzes',
      groupName: 'Weekly quizzes',
      weightPercent: 30,
      assessmentCount: 1,
      gradedCount: 1,
      ungradedCount: 0,
      averagePercent: 80,
      passRate: 100,
      distribution: [{ label: '80-89', minPercent: 80, maxPercent: 89, count: 1 }],
    },
    {
      groupId: 'group-project',
      groupName: 'Final project',
      weightPercent: 40,
      assessmentCount: 1,
      gradedCount: 1,
      ungradedCount: 0,
      averagePercent: 50,
      passRate: 0,
      distribution: [{ label: '0-59', minPercent: 0, maxPercent: 59, count: 1 }],
    },
  ],
} satisfies CourseAssessmentAnalytics;

describe('AssessmentsList weighted groups', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(updateAssessmentGroup).mockResolvedValue({ success: true, data: { id: 'group-quizzes' } });
    vi.mocked(deleteAssessmentGroup).mockResolvedValue({ success: true, data: null });
  });

  it('renders graded activities inside weighted assessment groups', () => {
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
        assessmentGroups={assessmentGroups}
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

  it('warns when weighted groups do not total 100 percent', () => {
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
        assessmentGroups={assessmentGroups}
      />,
    );

    expect(screen.getByText(/grade weights total 70%/i)).toBeInTheDocument();
    expect(screen.getByText(/adjust groups until they equal 100%/i)).toBeInTheDocument();
  });

  it('renders assessment score analytics in the assessment hub', () => {
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
        assessmentGroups={assessmentGroups}
        analytics={analytics}
      />,
    );

    expect(screen.getByRole('heading', { name: /score distribution/i })).toBeInTheDocument();
    expect(screen.getByText('65%')).toBeInTheDocument();
    expect(screen.getAllByText('50%').length).toBeGreaterThan(0);
    expect(screen.getByText(/2 graded/i)).toBeInTheDocument();
    expect(screen.getAllByText(/weekly quizzes/i).length).toBeGreaterThan(0);
  });

  it('does not offer Exam as a professor-facing assessment type', async () => {
    const user = userEvent.setup();
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
        assessmentGroups={assessmentGroups}
      />,
    );

    await user.click(screen.getByRole('button', { name: /add assessment/i }));

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.queryByText(/\bExam\b/i)).not.toBeInTheDocument();
    expect(screen.getByText(/quiz, assignment, or project/i)).toBeInTheDocument();
  });

  it('lets professors edit a weighted assessment group without leaving the assessment hub', async () => {
    const user = userEvent.setup();
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
        assessmentGroups={assessmentGroups}
      />,
    );

    await user.click(screen.getByRole('button', { name: /edit group weekly quizzes/i }));
    await user.clear(screen.getByLabelText(/weight percent/i));
    await user.type(screen.getByLabelText(/weight percent/i), '35');
    await user.click(screen.getByRole('button', { name: /save group/i }));

    await waitFor(() => {
      expect(updateAssessmentGroup).toHaveBeenCalledWith({
        courseId: 'course-1',
        groupId: 'group-quizzes',
        name: 'Weekly quizzes',
        description: null,
        weightPercent: 35,
        order: 1,
      });
    });
  });

  it('lets professors delete a weighted group and move its assessments back to ungrouped work', async () => {
    const user = userEvent.setup();
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
        assessmentGroups={assessmentGroups}
      />,
    );

    await user.click(screen.getByRole('button', { name: /delete group final project/i }));
    expect(screen.getByRole('dialog')).toHaveTextContent(/move existing assessments to ungrouped work/i);
    await user.click(screen.getByRole('button', { name: /delete group/i }));

    await waitFor(() => {
      expect(deleteAssessmentGroup).toHaveBeenCalledWith('course-1', 'group-project');
    });
  });
});
