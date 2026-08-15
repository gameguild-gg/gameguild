import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { filesToCodePayload } from '@/lib/coding-assignment/code-payload';

const mocks = vi.hoisted(() => ({
  getCourseAccessData: vi.fn(),
  getCourseLearnerContext: vi.fn(),
  getMyProjects: vi.fn(),
  auth: vi.fn(),
  getToken: vi.fn(),
  getCodingAssignmentPublic: vi.fn(),
  getMySubmissions: vi.fn(),
}));

vi.mock('@/lib/learner/courses', () => ({
  getCourseAccessData: mocks.getCourseAccessData,
}));
vi.mock('@/lib/learner/records', () => ({
  getCourseLearnerContext: mocks.getCourseLearnerContext,
  getMyProjects: mocks.getMyProjects,
}));
vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));
vi.mock('@/lib/coding-assignment/client', () => ({
  getCodingAssignmentPublic: mocks.getCodingAssignmentPublic,
}));

// Override only the module method the page calls; everything else stays real
// so co-importers (LearnerActivityForm, @/auth) keep their bindings.
vi.mock('@game-guild/client', async (importOriginal) => {
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

vi.mock('next/navigation', () => ({
  notFound: () => {
    throw new Error('not-found');
  },
}));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href }: { children: ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

// Contract per todo-4 spec: userId: string, submissionFiles: files | null.
// Mocking the client isolates this suite from the concurrent client rewrite.
vi.mock('./coding-activity-client', () => ({
  CodingActivityClient: (props: Record<string, unknown>) => (
    <div data-testid="coding-client" data-props={JSON.stringify(props)} />
  ),
}));

import LearnerActivityPage from './page';

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

function makeAssignment() {
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
    Tests: { Public: [], Private: [] },
    Grading: { MaxScore: 100, PassingScore: 60 },
  };
}

function makeSubmission(overrides: Record<string, unknown> = {}) {
  return {
    assessmentId: 'assessment-1',
    attemptNumber: 1,
    codePayload: null,
    ...overrides,
  };
}

async function renderCodingPage() {
  const page = await LearnerActivityPage({
    params: Promise.resolve({
      activityId: 'assessment-assessment-1',
      slug: 'test-course',
    }),
  });
  render(page);
  const client = await screen.findByTestId('coding-client');
  return JSON.parse(client.dataset.props ?? '{}');
}

describe('last-submission restore (server page)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getCourseAccessData.mockResolvedValue(makeReadyAccess());
    mocks.getCourseLearnerContext.mockResolvedValue(
      makeContext(makeAssessment()),
    );
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
    mocks.getToken.mockResolvedValue('token-1');
    mocks.getMyProjects.mockResolvedValue([]);
    mocks.getCodingAssignmentPublic.mockResolvedValue(makeAssignment());
    mocks.getMySubmissions.mockResolvedValue({ ok: true, data: [] });
  });

  it('fetches my submissions with the enrollment id and passes userId + restored files', async () => {
    mocks.getMySubmissions.mockResolvedValue({
      ok: true,
      data: [
        makeSubmission({
          assessmentId: 'other-assessment',
          attemptNumber: 5,
          codePayload: filesToCodePayload([{ path: 'other.py', content: 'nope' }]),
        }),
        makeSubmission({
          attemptNumber: 1,
          codePayload: filesToCodePayload([{ path: 'main.py', content: 'v1' }]),
        }),
      ],
    });

    const props = await renderCodingPage();

    expect(mocks.getMySubmissions).toHaveBeenCalledWith('enrollment-1');
    expect(mocks.auth).toHaveBeenCalledTimes(1);
    expect(props.userId).toBe('user-1');
    expect(props.submissionFiles).toEqual([
      { path: 'main.py', content: 'v1', encoding: 'text', modifiable: true },
    ]);
  });

  it('restores the max attemptNumber regardless of list order', async () => {
    mocks.getMySubmissions.mockResolvedValue({
      ok: true,
      data: [
        makeSubmission({
          attemptNumber: 3,
          codePayload: filesToCodePayload([{ path: 'main.py', content: 'v3' }]),
        }),
        makeSubmission({
          attemptNumber: 1,
          codePayload: filesToCodePayload([{ path: 'main.py', content: 'v1' }]),
        }),
        makeSubmission({ attemptNumber: 7, codePayload: null }),
      ],
    });

    const props = await renderCodingPage();

    expect(props.submissionFiles).toEqual([
      { path: 'main.py', content: 'v3', encoding: 'text', modifiable: true },
    ]);
  });

  it('renders with submissionFiles null when the fetch fails', async () => {
    mocks.getMySubmissions.mockRejectedValue(new Error('network down'));
    const errorSpy = vi
      .spyOn(console, 'error')
      .mockImplementation(() => undefined);

    const props = await renderCodingPage();

    expect(props.userId).toBe('user-1');
    expect(props.submissionFiles).toBeNull();
    errorSpy.mockRestore();
  });

  it('renders with submissionFiles null when codePayload is malformed', async () => {
    mocks.getMySubmissions.mockResolvedValue({
      ok: true,
      data: [
        makeSubmission({
          attemptNumber: 2,
          codePayload: 'not-json{{{',
        }),
      ],
    });
    const errorSpy = vi
      .spyOn(console, 'error')
      .mockImplementation(() => undefined);

    const props = await renderCodingPage();

    expect(props.submissionFiles).toBeNull();
    errorSpy.mockRestore();
  });

  it('passes null when no matching submission has a code payload', async () => {
    mocks.getMySubmissions.mockResolvedValue({
      ok: true,
      data: [
        makeSubmission({ codePayload: null }),
        makeSubmission({ assessmentId: 'other-assessment' }),
      ],
    });

    const props = await renderCodingPage();

    expect(props.submissionFiles).toBeNull();
  });
});
