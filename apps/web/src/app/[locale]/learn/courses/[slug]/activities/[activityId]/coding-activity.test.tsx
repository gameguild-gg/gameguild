import { render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getCourseAccessData: vi.fn(),
  getCourseLearnerContext: vi.fn(),
  getMyProjects: vi.fn(),
  auth: vi.fn(),
  getCodingDefinitionPublic: vi.fn(),
  submitAssessment: vi.fn(),
  useRouterPush: vi.fn(),
  IdeRender: vi.fn(),
  computeScore: vi.fn(),
}));

vi.mock('@/lib/learner/courses', () => ({
  getCourseAccessData: mocks.getCourseAccessData,
}));
vi.mock('@/lib/learner/records', () => ({
  getCourseLearnerContext: mocks.getCourseLearnerContext,
  getMyProjects: mocks.getMyProjects,
}));
vi.mock('@/auth', () => ({ auth: mocks.auth }));
vi.mock('@/lib/learning/queries/assessments', () => ({
  getCodingDefinitionPublic: mocks.getCodingDefinitionPublic,
}));
vi.mock('@/lib/learner/activity-actions', () => ({
  submitAssessment: mocks.submitAssessment,
}));
vi.mock('@/lib/emception/scoring', () => ({
  computeScore: mocks.computeScore,
}));
vi.mock('next/navigation', () => ({
  notFound: () => {
    throw new Error('not-found');
  },
}));
vi.mock('@/i18n/navigation', () => ({
  useRouter: () => ({ push: mocks.useRouterPush }),
  Link: ({ children, href }: { children: ReactNode; href: string }) => <a href={href}>{children}</a>,
}));

vi.mock('@game-guild/emception-ui', () => ({
  Ide: mocks.IdeRender,
}));

vi.mock('next/dynamic', () => ({
  // Resolve the loader promise synchronously by going through React.lazy so the
  // mocked <Ide> mounts inside <Suspense>.
  default: (loader: () => Promise<{ default: React.ComponentType<unknown> }>) => {
    const React = require('react');
    const Comp = React.lazy(loader);
    return function DynamicIde(props: unknown) {
      return React.createElement(
        React.Suspense,
        { fallback: React.createElement('div', null, 'Loading IDE…') },
        React.createElement(Comp, props),
      );
    };
  },
}));

import LearnerActivityPage from './page';

function makeAssessment(overrides: Record<string, unknown> = {}) {
  return {
    id: 'assessment-1',
    courseId: 'course-1',
    title: 'Coding Assessment',
    description: 'Write code',
    type: 'Assignment',
    maxScore: 100,
    passingScore: 60,
    submissionModalities: 'Code',
    dueAt: null,
    ...overrides,
  };
}

function makeReadyAccess() {
  return {
    kind: 'ready' as const,
    course: {
      id: 'course-1',
      title: 'Test Course',
      slug: 'test-course',
      description: '',
      thumbnail: null,
      modules: [],
      overallProgress: 0,
      totalItems: 0,
      completedItems: 0,
      remainingMinutes: 0,
      enrollmentId: 'enrollment-1',
    },
  };
}

function makeContext(assessment: ReturnType<typeof makeAssessment>) {
  return {
    enrollmentId: 'enrollment-1',
    cohort: null,
    calendar: [],
    assessmentGroups: [],
    assessments: [assessment],
    submissions: [],
    discussions: [],
    certificates: [],
  };
}

function pageParams(activityId = 'assessment-assessment-1', slug = 'test-course') {
  return { params: Promise.resolve({ activityId, slug }) };
}

function stubIde() {
  mocks.IdeRender.mockImplementation(({ ref, ...rest }) => {
    if (ref && typeof ref === 'object') {
      (ref as { current: unknown }).current = {
        getFiles: async () => [{ path: 'main.cpp', content: 'int main(){}' }],
      };
    }
    return (
      <div data-testid="mock-ide" data-props={JSON.stringify(rest)}>
        mocked ide
      </div>
    );
  });
}

describe('coding activity page routing', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getCourseAccessData.mockResolvedValue(makeReadyAccess());
    mocks.getCourseLearnerContext.mockResolvedValue(
      makeContext(makeAssessment()),
    );
    mocks.auth.mockResolvedValue(null);
    mocks.getMyProjects.mockResolvedValue([]);
    mocks.computeScore.mockReturnValue({ score: 0, passed: false, feedback: '' });
    stubIde();
  });

  it('mounts the emception IDE when a v2 coding definition is present', async () => {
    mocks.getCodingDefinitionPublic.mockResolvedValue({
      kind: 'coding',
      language: 'cpp',
      workspaceConfig: { files: { 'main.cpp': { encoding: 'text', content: '' } } },
      testPlan: { cases: [{ kind: 'stdio', name: 't1' }] },
      maxScore: 100,
      passingScore: 60,
    });

    render(await LearnerActivityPage(pageParams()));

    expect(mocks.getCodingDefinitionPublic).toHaveBeenCalledWith('assessment-1');
    const ide = await screen.findByTestId('mock-ide');
    expect(ide).toBeInTheDocument();
    const passedProps = JSON.parse(ide.dataset.props ?? '{}');
    expect(passedProps.testMode).toBe('public');
    expect(passedProps.maxScore).toBe(100);
    expect(passedProps.passingScore).toBe(60);
    expect(passedProps.manifestUrl).toBeUndefined();
    expect(screen.getByRole('button', { name: /^Submit$/ })).toBeInTheDocument();
  });

  it('falls back to LearnerActivityForm when coding def is null', async () => {
    mocks.getCodingDefinitionPublic.mockResolvedValue(null);

    render(await LearnerActivityPage(pageParams()));

    expect(mocks.getCodingDefinitionPublic).toHaveBeenCalledWith('assessment-1');
    expect(screen.queryByTestId('mock-ide')).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Submit assessment/i }),
    ).toBeInTheDocument();
  });

  it('falls back when coding def kind is not "coding"', async () => {
    mocks.getCodingDefinitionPublic.mockResolvedValue({
      kind: 'essay',
      language: 'text',
      workspaceConfig: {},
      testPlan: null,
      maxScore: 100,
      passingScore: 60,
    });

    render(await LearnerActivityPage(pageParams()));

    expect(screen.queryByTestId('mock-ide')).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Submit assessment/i }),
    ).toBeInTheDocument();
  });

  it('falls back when coding def has no workspaceConfig (malformed)', async () => {
    mocks.getCodingDefinitionPublic.mockResolvedValue({
      kind: 'coding',
      language: 'cpp',
      workspaceConfig: null,
      testPlan: null,
      maxScore: 100,
      passingScore: 60,
    });

    render(await LearnerActivityPage(pageParams()));

    expect(screen.queryByTestId('mock-ide')).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Submit assessment/i }),
    ).toBeInTheDocument();
  });

  it('does not call getCodingDefinitionPublic for a Quiz (non-coding type)', async () => {
    mocks.getCourseLearnerContext.mockResolvedValue(
      makeContext(
        makeAssessment({ type: 'Quiz', submissionModalities: 'StructuredAnswer' }),
      ),
    );

    render(await LearnerActivityPage(pageParams()));

    expect(mocks.getCodingDefinitionPublic).not.toHaveBeenCalled();
    expect(screen.queryByTestId('mock-ide')).not.toBeInTheDocument();
  });

  it('does not render the IDE when Code modality is undeclared even with a coding def', async () => {
    mocks.getCourseLearnerContext.mockResolvedValue(
      makeContext(makeAssessment({ submissionModalities: 'Text' })),
    );
    mocks.getCodingDefinitionPublic.mockResolvedValue({
      kind: 'coding',
      language: 'cpp',
      workspaceConfig: { files: {} },
      testPlan: null,
      maxScore: 100,
      passingScore: 60,
    });

    render(await LearnerActivityPage(pageParams()));

    expect(mocks.getCodingDefinitionPublic).not.toHaveBeenCalled();
    expect(screen.queryByTestId('mock-ide')).not.toBeInTheDocument();
  });

  it('posts codePayload via submitAssessment when Submit is clicked', async () => {
    mocks.getCodingDefinitionPublic.mockResolvedValue({
      kind: 'coding',
      language: 'cpp',
      workspaceConfig: { files: { 'main.cpp': { encoding: 'text', content: '' } } },
      testPlan: null,
      maxScore: 100,
      passingScore: 60,
    });
    mocks.submitAssessment.mockResolvedValue({ success: true });

    render(await LearnerActivityPage(pageParams()));

    await screen.findByTestId('mock-ide');
    screen.getByRole('button', { name: /^Submit$/ }).click();

    await waitFor(() =>
      expect(mocks.submitAssessment).toHaveBeenCalledTimes(1),
    );
    const [, formData] = mocks.submitAssessment.mock.calls[0];
    expect(formData.get('assessmentId')).toBe('assessment-1');
    expect(formData.get('enrollmentId')).toBe('enrollment-1');
    expect(formData.get('modality')).toBe('Code');
    const payload = JSON.parse(formData.get('response') as string);
    expect(payload).toEqual({ 'main.cpp': 'int main(){}' });
    expect(mocks.useRouterPush).toHaveBeenCalledWith(
      '/learn/courses/test-course/activities',
    );
  });

  it('renders the public-test estimate banner with computed score when Ide fires onTestReport', async () => {
    // 2 cases weights [2,3], both pass → passedWeight=5, totalWeight=5,
    // score = round(5/5 * 100) = 100.
    mocks.computeScore.mockReturnValue({ score: 100, passed: true, feedback: '' });
    mocks.getCodingDefinitionPublic.mockResolvedValue({
      kind: 'coding',
      language: 'cpp',
      workspaceConfig: { files: { 'main.cpp': { encoding: 'text', content: '' } } },
      testPlan: {
        cases: [
          { kind: 'stdio', name: 't1', weight: 2 },
          { kind: 'stdio', name: 't2', weight: 3 },
        ],
      },
      maxScore: 100,
      passingScore: 60,
    });

    render(await LearnerActivityPage(pageParams()));

    const ide = await screen.findByTestId('mock-ide');
    expect(ide).toBeInTheDocument();

    const lastCall = mocks.IdeRender.mock.calls.at(-1);
    const onTestReport = lastCall?.[0]?.onTestReport;
    expect(typeof onTestReport).toBe('function');

    onTestReport({
      passed: 2,
      failed: 0,
      totalDurationMs: 0,
      cases: [
        { name: 't1', passed: true, durationMs: 0 },
        { name: 't2', passed: true, durationMs: 0 },
      ],
    });

    const banner = await screen.findByTestId('public-test-estimate-banner');
    expect(banner.textContent).toContain('2/2 passed');
    expect(banner.textContent).toContain('estimated score: 100/100');
    expect(banner.textContent).toContain('estimate based on public tests only');
    expect(mocks.computeScore).toHaveBeenCalledTimes(1);
    expect(screen.queryByTestId('public-test-estimate-unavailable')).toBeNull();
  });

  it('renders "Estimate unavailable" without crashing when computeScore throws', async () => {
    mocks.computeScore.mockImplementation(() => {
      throw new Error('NaN weight');
    });
    mocks.getCodingDefinitionPublic.mockResolvedValue({
      kind: 'coding',
      language: 'cpp',
      workspaceConfig: { files: { 'main.cpp': { encoding: 'text', content: '' } } },
      testPlan: { cases: [{ kind: 'stdio', name: 't1', weight: NaN }] },
      maxScore: 100,
      passingScore: 60,
    });

    render(await LearnerActivityPage(pageParams()));

    await screen.findByTestId('mock-ide');
    const lastCall = mocks.IdeRender.mock.calls.at(-1);
    const onTestReport = lastCall?.[0]?.onTestReport;
    expect(typeof onTestReport).toBe('function');

    onTestReport({
      passed: 1,
      failed: 0,
      totalDurationMs: 0,
      cases: [{ name: 't1', passed: true, durationMs: 0 }],
    });

    const unavailable = await screen.findByTestId(
      'public-test-estimate-unavailable',
    );
    expect(unavailable.textContent).toContain('Estimate unavailable');
    expect(screen.queryByTestId('public-test-estimate-banner')).toBeNull();
  });
});
