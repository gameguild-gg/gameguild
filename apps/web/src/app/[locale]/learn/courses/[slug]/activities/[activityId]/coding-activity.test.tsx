import { render, screen, waitFor } from '@testing-library/react';
import { useLayoutEffect, type ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getCourseAccessData: vi.fn(),
  getCourseLearnerContext: vi.fn(),
  getMyProjects: vi.fn(),
  auth: vi.fn(),
  getToken: vi.fn(),
  getCodingAssignmentPublic: vi.fn(),
  getMySubmissions: vi.fn(),
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
vi.mock('@/auth', () => ({ auth: mocks.auth, getToken: mocks.getToken }));
vi.mock('@game-guild/client', async (importOriginal) => {
  // Stub only the method the page's loadLastSubmissionFiles calls; the rest
  // stays real so co-importers keep their bindings (same pattern as page.test.tsx).
  const actual = await importOriginal<typeof import('@game-guild/client')>();
  class LearningAssessmentsModuleStub {
    getAssessmentsMySubmissions = mocks.getMySubmissions;
  }
  return {
    ...actual,
    GeneratedApi: {
      ...actual.GeneratedApi,
      LearningAssessmentsModule: LearningAssessmentsModuleStub,
    },
  };
});
vi.mock('@/lib/coding-assignment/client', () => ({
  getCodingAssignmentPublic: mocks.getCodingAssignmentPublic,
}));
vi.mock('@/lib/learner/activity-actions', () => ({
  submitAssessment: mocks.submitAssessment,
}));
vi.mock('@/lib/emception/scoring', () => ({
  computeScore: mocks.computeScore,
}));
vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning',
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
  // Minimal stand-in for the real presets — the page only reads
  // workspaceConfig.{id,label,files} off the sample and overrides files.
  ASSIGNMENT_SAMPLES: {
    cpp: {
      workspaceConfig: {
        id: 'cpp',
        label: 'C++ Assignment',
        files: { '/user/main.cpp': { encoding: 'text', content: '// preset' } },
      },
    },
  },
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

// Real (unmocked) key math — same module hasRestorableDraft reads through.
import { workspaceStorageKey } from '@game-guild/emception-ui/ide-types';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/types';
import LearnerActivityPage from './page';
import { CodingActivityClient } from '@/components/learning/coding-activity-client';
import type { SeedFile } from '@/components/learning/resolve-seed';

function makeAssessment(overrides: Record<string, unknown> = {}) {
  return {
    id: 'assessment-1',
    courseId: 'course-1',
    contentId: 'content-1',
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

function makeAssignment(overrides: Record<string, unknown> = {}) {
  return {
    Type: 'coding-assignment',
    Version: 1,
    Environment: {
      Language: 'cpp',
      Tools: '',
      AllowStudentCreateFiles: true,
    },
    Data: {
      Files: {
        'main.cpp': {
          Content: '// starter',
          Encoding: 'text',
          Visibility: 'Public',
          Modifiable: true,
        },
      },
    },
    Tests: {
      Public: [],
      Private: [],
    },
    Grading: { MaxScore: 100, PassingScore: 60 },
    ...overrides,
  };
}

/**
 * Attach an IdeHandle to the page's ref — object refs assign directly,
 * callback refs (the page's lazy-mount-safe pattern) are invoked post-render
 * to mirror React's commit-phase ref timing.
 */
function attachHandle(
  ref: unknown,
  handle: Record<string, unknown>,
): void {
  if (typeof ref === 'function') {
    (ref as (h: unknown) => void)(handle);
  } else if (ref && typeof ref === 'object') {
    (ref as { current: unknown }).current = handle;
  }
}

function stubIde(getModified: Array<{ path: string; content: string; encoding: 'text' }> = []) {
  mocks.IdeRender.mockImplementation(({ ref, ...rest }) => {
    const handle = {
      getFiles: async () => getModified,
      getModifiedFiles: async () => getModified,
      setFiles: vi.fn(async () => undefined),
      setFileMeta: vi.fn(async () => undefined),
      setBaseline: vi.fn(),
      addFile: vi.fn(async () => undefined),
    };
    useLayoutEffect(() => {
      attachHandle(ref, handle);
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    return (
      <div data-testid="mock-ide" data-props={JSON.stringify(rest)}>
        mocked ide
      </div>
    );
  });
}

/** Client-level stub: same attach pattern, but every handle method is a
 *  spy so load-order tests can assert call shapes (and non-calls). */
function stubClientIde(current: Array<{ path: string; content: string }> = []) {
  const handle = {
    getFiles: vi.fn(async () => current),
    getModifiedFiles: vi.fn(async () => []),
    setFiles: vi.fn(async () => undefined),
    setFileMeta: vi.fn(async () => undefined),
    setBaseline: vi.fn(),
    addFile: vi.fn(async () => undefined),
  };
  mocks.IdeRender.mockImplementation(({ ref, ...rest }) => {
    useLayoutEffect(() => {
      attachHandle(ref, handle);
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    return (
      <div data-testid="mock-ide" data-props={JSON.stringify(rest)}>
        mocked ide
      </div>
    );
  });
  return handle;
}

describe('coding activity page routing', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();
    mocks.getCourseAccessData.mockResolvedValue(makeReadyAccess());
    mocks.getCourseLearnerContext.mockResolvedValue(
      makeContext(makeAssessment()),
    );
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
    mocks.getToken.mockResolvedValue('token-1');
    mocks.getMySubmissions.mockResolvedValue({ ok: true, data: [] });
    mocks.getMyProjects.mockResolvedValue([]);
    mocks.computeScore.mockReturnValue({ score: 0, passed: false, feedback: '' });
    stubIde();
  });

  it('mounts the emception IDE when a v1 coding-assignment is present', async () => {
    mocks.getCodingAssignmentPublic.mockResolvedValue(makeAssignment());

    render(await LearnerActivityPage(pageParams()));

    expect(mocks.getCodingAssignmentPublic).toHaveBeenCalledWith('course-1', 'content-1');
    const ide = await screen.findByTestId('mock-ide');
    expect(ide).toBeInTheDocument();
    const passedProps = JSON.parse(ide.dataset.props ?? '{}');
    expect(passedProps.testMode).toBe('public');
    expect(passedProps.maxScore).toBe(100);
    expect(passedProps.passingScore).toBe(60);
    expect(passedProps.manifestUrl).toBeUndefined();
    // FIX 1: workspaceConfig boots the assignment language + Public files
    // (hides the preset picker); FIX 2/3: no testPlan with zero tests,
    // storage namespaced per user + assessment.
    expect(passedProps.assignmentToken).toBe('user-1:assessment-1');
    expect(passedProps.testPlan).toBeUndefined();
    expect(passedProps.workspaceConfig.id).toBe('cpp');
    expect(passedProps.workspaceConfig.files).toEqual({
      'main.cpp': { encoding: 'text', content: '// starter' },
    });
    expect(screen.getByRole('button', { name: /^Submit$/ })).toBeInTheDocument();
  });

  it('falls back to LearnerActivityForm when assignment is null', async () => {
    mocks.getCodingAssignmentPublic.mockResolvedValue(null);

    render(await LearnerActivityPage(pageParams()));

    expect(mocks.getCodingAssignmentPublic).toHaveBeenCalledWith('course-1', 'content-1');
    expect(screen.queryByTestId('mock-ide')).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Submit assessment/i }),
    ).toBeInTheDocument();
  });

  it('falls back when content type is not "coding-assignment"', async () => {
    mocks.getCodingAssignmentPublic.mockResolvedValue(
      makeAssignment({ Type: 'quiz-assignment' }),
    );

    render(await LearnerActivityPage(pageParams()));

    expect(screen.queryByTestId('mock-ide')).not.toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: /Submit assessment/i }),
    ).toBeInTheDocument();
  });

  it('does not fetch coding assignment for a Quiz (non-coding type)', async () => {
    mocks.getCourseLearnerContext.mockResolvedValue(
      makeContext(
        makeAssessment({ type: 'Quiz', submissionModalities: 'StructuredAnswer' }),
      ),
    );

    render(await LearnerActivityPage(pageParams()));

    expect(mocks.getCodingAssignmentPublic).not.toHaveBeenCalled();
    expect(screen.queryByTestId('mock-ide')).not.toBeInTheDocument();
  });

  it('does not render the IDE when Code modality is undeclared even with an assignment', async () => {
    mocks.getCourseLearnerContext.mockResolvedValue(
      makeContext(makeAssessment({ submissionModalities: 'Text' })),
    );
    mocks.getCodingAssignmentPublic.mockResolvedValue(makeAssignment());

    render(await LearnerActivityPage(pageParams()));

    expect(mocks.getCodingAssignmentPublic).not.toHaveBeenCalled();
    expect(screen.queryByTestId('mock-ide')).not.toBeInTheDocument();
  });

  it('posts codePayload via submitAssessment when Submit is clicked (v1 {content, encoding} shape)', async () => {
    const modified = [{ path: 'main.cpp', content: 'int main(){}', encoding: 'text' as const }];
    stubIde(modified);
    mocks.getCodingAssignmentPublic.mockResolvedValue(makeAssignment());
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
    expect(payload).toEqual({
      'main.cpp': { content: 'int main(){}', encoding: 'text' },
    });
    expect(mocks.useRouterPush).toHaveBeenCalledWith(
      '/learn/courses/test-course/activities',
    );
  });

  it('seeds IDE with Public files only and applies setFileMeta to non-modifiable files', async () => {
    mocks.getCodingAssignmentPublic.mockResolvedValue(
      makeAssignment({
        Data: {
          Files: {
            'main.cpp': {
              Content: 'pub-mod',
              Encoding: 'text',
              Visibility: 'Public',
              Modifiable: true,
            },
            'secret.h': {
              Content: 'priv',
              Encoding: 'text',
              Visibility: 'Private',
              Modifiable: false,
            },
            'readonly.h': {
              Content: 'pub-readonly',
              Encoding: 'text',
              Visibility: 'Public',
              Modifiable: false,
            },
          },
        },
      }),
    );

    const setFilesMock = vi.fn(async () => undefined);
    const setFileMetaMock = vi.fn(async () => undefined);
    mocks.IdeRender.mockImplementation(({ ref, ...rest }) => {
      const handle = {
        getFiles: async () => [],
        getModifiedFiles: async () => [],
        setFiles: setFilesMock,
        setFileMeta: setFileMetaMock,
      };
      useLayoutEffect(() => {
        attachHandle(ref, handle);
        // eslint-disable-next-line react-hooks/exhaustive-deps
      }, []);
      return (
        <div data-testid="mock-ide" data-props={JSON.stringify(rest)}>
          mocked ide
        </div>
      );
    });

    render(await LearnerActivityPage(pageParams()));
    await screen.findByTestId('mock-ide');

    await waitFor(() => expect(setFilesMock).toHaveBeenCalledTimes(1));
    const seeded = setFilesMock.mock.calls[0][0] as Array<{ path: string }>;
    const seededPaths = seeded.map((f) => f.path);
    expect(seededPaths).toEqual(['main.cpp', 'readonly.h']);
    expect(seededPaths).not.toContain('secret.h');

    // setFileMeta called ONLY for the non-modifiable Public file.
    await waitFor(() => expect(setFileMetaMock).toHaveBeenCalledTimes(1));
    expect(setFileMetaMock).toHaveBeenCalledWith('readonly.h', { modifiable: false });
  });

  it('passes allowCreateFiles=false as a real prop when AllowStudentCreateFiles is false', async () => {
    mocks.getCodingAssignmentPublic.mockResolvedValue(
      makeAssignment({
        Environment: { Language: 'cpp', Tools: '', AllowStudentCreateFiles: false },
      }),
    );

    render(await LearnerActivityPage(pageParams()));

    const ide = await screen.findByTestId('mock-ide');
    const passedProps = JSON.parse(ide.dataset.props ?? '{}');
    expect(passedProps.allowCreateFiles).toBe(false);
    // CSS-hack deletion: no wrapper data-attribute and no injected <style> rule.
    expect(document.querySelector('[data-allow-create-files]')).toBeNull();
    expect(document.querySelector('style')).toBeNull();
  });

  it('renders the public-test estimate banner with computed score when Ide fires onTestReport', async () => {
    // 2 cases weights [2,3], both pass → passedWeight=5, totalWeight=5,
    // score = round(5/5 * 100) = 100.
    mocks.computeScore.mockReturnValue({ score: 100, passed: true, feedback: '' });
    mocks.getCodingAssignmentPublic.mockResolvedValue(
      makeAssignment({
        Tests: {
          Public: [
            { kind: 'standard', Name: 't1', Weight: 2, Stdout: 'x' },
            { kind: 'standard', Name: 't2', Weight: 3, Stdout: 'y' },
          ],
          Private: [],
        },
      }),
    );

    render(await LearnerActivityPage(pageParams()));

    const ide = await screen.findByTestId('mock-ide');
    expect(ide).toBeInTheDocument();

    // FIX 2 counterpart: with 2 standard tests the Run Tests plan IS passed.
    const passedProps = JSON.parse(ide.dataset.props ?? '{}');
    expect(passedProps.testPlan.cases).toHaveLength(2);
    expect(passedProps.testPlan.cases.map((c: { name?: string }) => c.name)).toEqual(['t1', 't2']);

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

  it('maps functional public tests into the student plan (doctest case + generated harness)', async () => {
    // Real-world shape (ai-for-games assessment-85333849): Public tests are
    // FunctionalTestGroups, not StandardTests. The plan must still carry a
    // runnable case + its generated doctest harness, or the IDE hides the
    // Test Cases tab and Run Tests button (testPlan gated on cases.length).
    mocks.getCodingAssignmentPublic.mockResolvedValue(
      makeAssignment({
        Tests: {
          Public: [
            {
              kind: 'functional',
              Name: 'sum',
              Weight: 2,
              Function: {
                FunctionName: 'test',
                Parameters: [
                  { Type: 'integer', Name: 'a' },
                  { Type: 'integer', Name: 'b' },
                ],
                ReturnType: { Type: 'integer' },
              },
              Cases: [
                {
                  Inputs: [
                    { Type: 'integer', Content: 1 },
                    { Type: 'integer', Content: 2 },
                  ],
                  Expected: { Type: 'integer', Content: 3 },
                },
              ],
            },
          ],
          Private: [],
        },
      }),
    );

    render(await LearnerActivityPage(pageParams()));

    const ide = await screen.findByTestId('mock-ide');
    const passedProps = JSON.parse(ide.dataset.props ?? '{}');
    expect(passedProps.testPlan).toBeDefined();
    expect(passedProps.testPlan.cases).toHaveLength(1);
    expect(passedProps.testPlan.cases[0].kind).toBe('doctest');
    expect(passedProps.testPlan.cases[0].name).toBe('sum');
    expect(passedProps.testPlan.cases[0].weight).toBe(2);
    expect(passedProps.testPlan.generatedFiles).toHaveLength(1);
    expect(passedProps.testPlan.generatedFiles[0].path).toMatch(/\.cpp$/);
    expect(passedProps.testPlan.generatedFiles[0].content).toContain('TEST_CASE');
  });

  it('excludes private tests from the student plan even when the server fails to strip them', async () => {
    mocks.getCodingAssignmentPublic.mockResolvedValue(
      makeAssignment({
        Tests: {
          Public: [{ kind: 'standard', Name: 'pub-1', Stdout: 'x' }],
          Private: [
            { kind: 'standard', Name: 'priv-1', Stdout: 'secret' },
            {
              kind: 'functional',
              Name: 'priv-fn',
              Function: {
                FunctionName: 'hidden',
                Parameters: [],
                ReturnType: { Type: 'integer' },
              },
              Cases: [
                {
                  Inputs: [],
                  Expected: { Type: 'integer', Content: 42 },
                },
              ],
            },
          ],
        },
      }),
    );

    render(await LearnerActivityPage(pageParams()));

    const ide = await screen.findByTestId('mock-ide');
    const passedProps = JSON.parse(ide.dataset.props ?? '{}');
    expect(passedProps.testPlan.cases).toHaveLength(1);
    expect(passedProps.testPlan.cases.map((c: { name?: string }) => c.name)).toEqual(['pub-1']);
    expect(JSON.stringify(passedProps.testPlan)).not.toContain('priv-');
    expect(JSON.stringify(passedProps.testPlan)).not.toContain('secret');
  });

  it('renders "Estimate unavailable" without crashing when computeScore throws', async () => {
    mocks.computeScore.mockImplementation(() => {
      throw new Error('NaN weight');
    });
    mocks.getCodingAssignmentPublic.mockResolvedValue(
      makeAssignment({
        Tests: {
          Public: [{ kind: 'standard', Name: 't1', Weight: NaN, Stdout: 'x' }],
          Private: [],
        },
      }),
    );

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

describe('CodingActivityClient draft/submission/seed load order', () => {
  const TOKEN = 'user-1:assessment-1';

  function makeImageAssignment() {
    return makeAssignment({
      Data: {
        Files: {
          'main.cpp': {
            Content: '// starter',
            Encoding: 'text',
            Visibility: 'Public',
            Modifiable: true,
          },
          'logo.png': {
            Content: 'aGVsbG8=',
            Encoding: 'base64',
            Visibility: 'Public',
            Modifiable: false,
          },
        },
      },
    });
  }

  function renderClient(props: {
    userId?: string;
    submissionFiles?: SeedFile[] | null;
    assignment?: ReturnType<typeof makeAssignment>;
  } = {}) {
    return render(
      <CodingActivityClient
        assessmentId="assessment-1"
        enrollmentId="enrollment-1"
        courseId="course-1"
        slug="test-course"
        assignment={(props.assignment ?? makeAssignment()) as unknown as CodingAssignmentContent}
        userId={props.userId}
        submissionFiles={props.submissionFiles ?? null}
      />,
    );
  }

  function seedDraftEntry(token: string, raw: string) {
    window.localStorage.setItem(workspaceStorageKey(token, 'cpp'), raw);
  }

  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();
    mocks.submitAssessment.mockResolvedValue({ success: true });
    stubClientIde();
  });

  it('namespaces the storage token as userId:assessmentId, falling back to the bare assessment id', async () => {
    const first = renderClient({ userId: 'user-1' });
    let ide = await screen.findByTestId('mock-ide');
    expect(JSON.parse(ide.dataset.props ?? '{}').assignmentToken).toBe(TOKEN);
    first.unmount();

    const second = renderClient();
    ide = await screen.findByTestId('mock-ide');
    expect(JSON.parse(ide.dataset.props ?? '{}').assignmentToken).toBe('assessment-1');
    second.unmount();
  });

  it('seed mode: setFiles with the seed texts, no setBaseline, no addFile', async () => {
    const handle = stubClientIde();
    renderClient({ userId: 'user-1' });
    await screen.findByTestId('mock-ide');

    await waitFor(() => expect(handle.setFiles).toHaveBeenCalledTimes(1));
    expect(handle.setFiles).toHaveBeenCalledWith([
      { path: 'main.cpp', content: '// starter' },
    ]);
    expect(handle.setBaseline).not.toHaveBeenCalled();
    expect(handle.addFile).not.toHaveBeenCalled();
  });

  it('submission mode: setFiles overlay + setBaseline pinned to the instructor seed', async () => {
    const handle = stubClientIde();
    renderClient({
      userId: 'user-1',
      submissionFiles: [
        { path: 'main.cpp', content: 'int main(){}', encoding: 'text' },
        { path: 'extra.py', content: 'print()', encoding: 'text' },
      ],
    });
    const ide = await screen.findByTestId('mock-ide');

    await waitFor(() => expect(handle.setFiles).toHaveBeenCalledTimes(1));
    expect(handle.setFiles).toHaveBeenCalledWith([
      { path: 'main.cpp', content: 'int main(){}' },
      { path: 'extra.py', content: 'print()' },
    ]);
    await waitFor(() => expect(handle.setBaseline).toHaveBeenCalledTimes(1));
    expect(handle.setBaseline).toHaveBeenCalledWith([
      { path: 'main.cpp', content: '// starter' },
    ]);
    // First paint already shows the overlay via workspaceConfig.
    const passedProps = JSON.parse(ide.dataset.props ?? '{}');
    expect(passedProps.workspaceConfig.files['main.cpp']).toEqual({
      encoding: 'text',
      content: 'int main(){}',
    });
    expect(passedProps.workspaceConfig.files['extra.py']).toEqual({
      encoding: 'text',
      content: 'print()',
    });
  });

  it('draft mode: skips setFiles, merges missing image seeds, pins baseline to the instructor seed', async () => {
    seedDraftEntry(TOKEN, JSON.stringify({ files: { 'main.cpp': { type: 'text', content: '// draft' } } }));
    const handle = stubClientIde([{ path: 'main.cpp', content: '// draft' }]);
    renderClient({ userId: 'user-1', assignment: makeImageAssignment() });
    await screen.findByTestId('mock-ide');

    await waitFor(() => expect(handle.setBaseline).toHaveBeenCalledTimes(1));
    expect(handle.setFiles).not.toHaveBeenCalled();
    expect(handle.addFile).toHaveBeenCalledWith('logo.png', 'aGVsbG8=', 'base64');
    expect(handle.setBaseline).toHaveBeenCalledWith([
      { path: 'main.cpp', content: '// starter' },
    ]);
    // readOnly meta applies in draft mode too (logo.png is non-modifiable).
    await waitFor(() =>
      expect(handle.setFileMeta).toHaveBeenCalledWith('logo.png', { modifiable: false }),
    );
  });

  it('draft detection also reads the legacy token key when the new key is absent', async () => {
    seedDraftEntry('assessment-1', JSON.stringify({ files: {} }));
    const handle = stubClientIde();
    renderClient({ userId: 'user-1' });
    await screen.findByTestId('mock-ide');

    await waitFor(() => expect(handle.setBaseline).toHaveBeenCalledTimes(1));
    expect(handle.setFiles).not.toHaveBeenCalled();
  });

  it('corrupt JSON under both keys falls back to seed mode without crashing', async () => {
    seedDraftEntry(TOKEN, '{not json');
    seedDraftEntry('assessment-1', 'also not json');
    const handle = stubClientIde();
    renderClient({ userId: 'user-1' });
    await screen.findByTestId('mock-ide');

    await waitFor(() => expect(handle.setFiles).toHaveBeenCalledTimes(1));
    expect(handle.setBaseline).not.toHaveBeenCalled();
  });

  it('reads storage exactly once per mount (new-key hit short-circuits the legacy read)', async () => {
    seedDraftEntry(TOKEN, JSON.stringify({ files: {} }));
    const getItemSpy = vi.spyOn(Storage.prototype, 'getItem');
    const handle = stubClientIde();
    renderClient({ userId: 'user-1' });
    await screen.findByTestId('mock-ide');

    await waitFor(() => expect(handle.setBaseline).toHaveBeenCalledTimes(1));
    expect(getItemSpy).toHaveBeenCalledTimes(1);
    expect(getItemSpy).toHaveBeenCalledWith(workspaceStorageKey(TOKEN, 'cpp'));
  });

  it('passes allowCreateFiles=true through as a real prop by default', async () => {
    renderClient({ userId: 'user-1' });
    const ide = await screen.findByTestId('mock-ide');
    expect(JSON.parse(ide.dataset.props ?? '{}').allowCreateFiles).toBe(true);
  });
});
