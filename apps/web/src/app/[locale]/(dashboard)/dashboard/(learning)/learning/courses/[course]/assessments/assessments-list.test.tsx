import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { AnchorHTMLAttributes, ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AssessmentsList } from './assessments-list';
import { createAssessmentGroup, deleteAssessmentGroup, updateAssessmentGroup } from '@/lib/learning/actions';
import type { Assessment, CourseAssessmentAnalytics } from '@/lib/learning/queries/assessments';
import type { ContentItem } from '@/lib/learning/types';

Object.defineProperties(HTMLElement.prototype, {
  hasPointerCapture: { value: vi.fn(() => false) },
  setPointerCapture: { value: vi.fn() },
  releasePointerCapture: { value: vi.fn() },
  scrollIntoView: { value: vi.fn() },
});

global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};

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

const assignmentAssessment = {
  id: 'assignment-1',
  courseId: 'course-1',
  contentId: 'lesson-1',
  title: 'Environment setup',
  description: null,
  type: 'Assignment',
  maxScore: 15,
  passingScore: 10,
  timeLimitMinutes: 45,
  maxAttempts: 2,
  isRequired: false,
  order: 0,
  availableFrom: null,
  availableUntil: null,
  isAvailable: false,
  assessmentGroupId: null,
  assessmentGroupName: null,
  assessmentGroupWeightPercent: null,
  assessmentGroupOrder: null,
} as unknown as Assessment;

const gradedQuizContent = {
  id: 'content-quiz-1',
  parentId: null,
  order: 0,
  type: 'Questionnaire',
  title: 'Practice quiz',
  description: null,
  status: 'published',
  duration: null,
  metadata: {},
  gradingMethod: null,
  maxPoints: null,
  gradingConfig: {
    enabled: true,
    schemaVersion: 1,
    validationMode: 'public',
    gradebook: {
      maxScore: 3,
      official: false,
      required: true,
    },
    policy: {
      feedbackMode: 'immediate',
      presentationMode: 'continuous',
    },
    items: {
      question_1: {
        contentBlockId: 'question_1',
        points: 3,
        gradingKind: 'deterministic',
      },
    },
  },
  createdAt: '2024-01-01T00:00:00.000Z',
  updatedAt: '2024-01-01T00:00:00.000Z',
} satisfies ContentItem;

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

const emptyAnalytics = {
  courseId: 'course-1',
  assessmentCount: 0,
  gradedCount: 0,
  ungradedCount: 2,
  averagePercent: 0,
  passRate: 0,
  distribution: [
    { label: '0-59', minPercent: 0, maxPercent: 59, count: 0 },
    { label: '60-69', minPercent: 60, maxPercent: 69, count: 0 },
    { label: '70-79', minPercent: 70, maxPercent: 79, count: 0 },
    { label: '80-89', minPercent: 80, maxPercent: 89, count: 0 },
    { label: '90-100', minPercent: 90, maxPercent: 100, count: 0 },
  ],
  groups: [
    {
      groupId: null,
      groupName: 'Ungrouped activities',
      weightPercent: null,
      assessmentCount: 2,
      gradedCount: 0,
      ungradedCount: 2,
      averagePercent: 0,
      passRate: 0,
      distribution: [],
    },
  ],
} satisfies CourseAssessmentAnalytics;

describe('AssessmentsList weighted groups', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(createAssessmentGroup).mockResolvedValue({ success: true, data: { id: 'group-new' } });
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

  it('renders empty analytics and ungraded weighted groups without score bars', () => {
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={[]}
        total={0}
        assessmentGroups={[]}
        analytics={emptyAnalytics}
      />,
    );

    expect(screen.getByText('No graded scores yet')).toBeInTheDocument();
    expect(screen.getByText(/score distribution appears after submissions are graded/i)).toBeInTheDocument();
    expect(screen.getByText('Ungrouped activities')).toBeInTheDocument();
    expect(screen.getAllByText('Ungraded').length).toBeGreaterThan(0);
  });

  it('renders ungrouped assignments with schedule and attempt metadata', () => {
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={[assignmentAssessment]}
        total={1}
      />,
    );

    const ungrouped = screen.getByTestId('assessment-group-ungrouped');
    expect(within(ungrouped).getByText('Activities that do not yet count toward a weighted grade group.')).toBeInTheDocument();
    expect(within(ungrouped).getByText('45m')).toBeInTheDocument();
    expect(within(ungrouped).getByText('2 attempts')).toBeInTheDocument();
    expect(within(ungrouped).getByText('scheduled')).toBeInTheDocument();
    expect(within(ungrouped).getByText('Assignment')).toBeInTheDocument();
  });

  it('renders the empty state without offering direct assessment creation', () => {
    render(<AssessmentsList courseId="course-1" assessments={[]} total={0} />);

    expect(screen.getByText('No assessments yet')).toBeInTheDocument();
    expect(screen.getByText(/graded content will appear here after grading is enabled from the content editor/i)).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /create first assessment/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /add assessment/i })).not.toBeInTheDocument();
  });

  it('projects content-owned graded activities without direct assessment records', () => {
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={[]}
        total={0}
        gradedContentItems={[gradedQuizContent]}
      />,
    );

    const contentSection = screen.getByTestId('content-owned-graded-activities');
    const link = within(contentSection).getByRole('link', { name: /practice quiz/i });

    expect(link).toHaveAttribute('href', '/dashboard/learning/courses/course-1/content/content-quiz-1');
    expect(within(contentSection).getByText('3 pts')).toBeInTheDocument();
    expect(within(contentSection).getByText('public practice')).toBeInTheDocument();
    expect(within(contentSection).getByText('Quiz')).toBeInTheDocument();
    expect(screen.queryByText('No assessments yet')).not.toBeInTheDocument();
  });

  it('creates a weighted group and validates group weights before calling the API', async () => {
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
        assessmentGroups={assessmentGroups}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /add group/i }));
    let dialog = screen.getByRole('dialog', { name: /create assessment group/i });
    fireEvent.change(within(dialog).getByLabelText(/group name/i), { target: { value: 'Attendance' } });
    fireEvent.change(within(dialog).getByLabelText(/weight percent/i), { target: { value: '125' } });
    fireEvent.click(within(dialog).getByRole('button', { name: /create group/i }));

    expect(await screen.findByText('Weight must be between 0 and 100.')).toBeInTheDocument();
    expect(createAssessmentGroup).not.toHaveBeenCalled();

    fireEvent.change(within(dialog).getByLabelText(/weight percent/i), { target: { value: '30' } });
    fireEvent.click(within(dialog).getByRole('button', { name: /create group/i }));

    await waitFor(() => {
      expect(createAssessmentGroup).toHaveBeenCalledWith({
        courseId: 'course-1',
        name: 'Attendance',
        weightPercent: 30,
        order: 3,
      });
    });
  });

  it('shows create group server errors without closing the dialog', async () => {
    vi.mocked(createAssessmentGroup).mockResolvedValueOnce({ success: false, error: 'Group quota reached.' });
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
        assessmentGroups={assessmentGroups}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /add group/i }));
    const dialog = screen.getByRole('dialog', { name: /create assessment group/i });
    fireEvent.click(within(dialog).getByRole('button', { name: /create group/i }));

    expect(await screen.findByText('Group name is required.')).toBeInTheDocument();
    expect(createAssessmentGroup).not.toHaveBeenCalled();

    fireEvent.change(within(dialog).getByLabelText(/group name/i), { target: { value: 'Participation' } });
    fireEvent.click(within(dialog).getByRole('button', { name: /create group/i }));

    expect(await screen.findByText('Group quota reached.')).toBeInTheDocument();
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

  it('validates edit group fields and keeps server errors visible', async () => {
    vi.mocked(updateAssessmentGroup).mockResolvedValueOnce({ success: false, error: 'Group name already exists.' });
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
        assessmentGroups={assessmentGroups}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: /edit group weekly quizzes/i }));
    const dialog = screen.getByRole('dialog', { name: /edit assessment group/i });
    fireEvent.change(within(dialog).getByLabelText(/group name/i), { target: { value: '' } });
    fireEvent.click(within(dialog).getByRole('button', { name: /save group/i }));

    expect(await screen.findByText('Group name is required.')).toBeInTheDocument();
    expect(updateAssessmentGroup).not.toHaveBeenCalled();

    fireEvent.change(within(dialog).getByLabelText(/group name/i), { target: { value: 'Weekly coding' } });
    fireEvent.change(within(dialog).getByLabelText(/weight percent/i), { target: { value: '-1' } });
    fireEvent.click(within(dialog).getByRole('button', { name: /save group/i }));

    expect(await screen.findByText('Weight must be between 0 and 100.')).toBeInTheDocument();

    fireEvent.change(within(dialog).getByLabelText(/weight percent/i), { target: { value: '30' } });
    fireEvent.change(within(dialog).getByLabelText(/description/i), { target: { value: 'Short weekly checks' } });
    fireEvent.click(within(dialog).getByRole('button', { name: /save group/i }));

    expect(await screen.findByText('Group name already exists.')).toBeInTheDocument();
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

  it('keeps delete group errors visible for retry', async () => {
    const user = userEvent.setup();
    vi.mocked(deleteAssessmentGroup).mockResolvedValueOnce({ success: false, error: 'Cannot delete a locked grade group.' });
    render(
      <AssessmentsList
        courseId="course-1"
        assessments={groupedAssessments}
        total={groupedAssessments.length}
        assessmentGroups={assessmentGroups}
      />,
    );

    await user.click(screen.getByRole('button', { name: /delete group final project/i }));
    await user.click(screen.getByRole('button', { name: /delete group/i }));

    expect(await screen.findByText('Cannot delete a locked grade group.')).toBeInTheDocument();
  });
});
