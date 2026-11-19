import {
  act,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import type { ComponentProps, ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CodingAssignmentContent } from "@/lib/coding-assignment/types";

const mocks = vi.hoisted(() => ({
  buildPlan: vi.fn(),
  createWorkspace: vi.fn(),
  toDelivery: vi.fn(),
  toFileRecord: vi.fn(),
  editor: vi.fn(() => null),
  push: vi.fn(),
  seedFiles: vi.fn(),
  submit: vi.fn(),
  start: vi.fn(),
  storageKey: vi.fn(),
  banner: vi.fn(() => null),
}));

vi.mock("@/i18n/navigation", () => ({
  useRouter: () => ({ push: mocks.push }),
}));
vi.mock("@/lib/coding-assignment/coding-surface-bridge", () => ({
  codingAssignmentToLearnerDelivery: mocks.toDelivery,
  codingSubmissionToFileRecord: mocks.toFileRecord,
}));
vi.mock("@/lib/learning/grading-runtime-actions", () => ({
  startIndividualRuntimeSubmission: mocks.start,
  submitRuntimeSubmission: mocks.submit,
}));
vi.mock("@game-guild/coding-surface", () => ({
  CodingSurface: mocks.editor,
  buildCodingExecutionPlan: mocks.buildPlan,
  createCodingWorkspaceConfig: mocks.createWorkspace,
  codingWorkspaceStorageKey: mocks.storageKey,
}));
vi.mock("next/script", () => ({ default: () => null }));
vi.mock("./resolve-seed", () => ({ publicSeedFiles: mocks.seedFiles }));
vi.mock("./public-test-estimate-banner", () => ({
  PublicTestEstimateBanner: (props: ComponentProps<"div">) => {
    mocks.banner(props);
    return <div data-testid="estimate-banner" />;
  },
}));
vi.mock("@game-guild/ui/components/button", () => ({
  Button: ({
    children,
    ...props
  }: ComponentProps<"button"> & { children: ReactNode }) => (
    <button {...props}>{children}</button>
  ),
}));

import { CodingActivityClient } from "./coding-activity-client";

function assignment(language: string | undefined = "cpp") {
  return {
    Type: 'coding-assignment',
    Version: 1,
    Environment: { Language: language },
    Data: { Files: {} },
    Tests: { Public: [], Private: [] },
    Grading: { MaxScore: 100 },
  } as unknown as CodingAssignmentContent;
}

function renderActivity(
  overrides: Partial<ComponentProps<typeof CodingActivityClient>> = {},
) {
  return render(
    <CodingActivityClient
      assessmentId="assessment-1"
      enrollmentId="enrollment-1"
      courseId="course-1"
      slug="game-ai"
      assignment={assignment()}
      {...overrides}
    />,
  );
}

async function editorProps() {
  await screen.findByTestId("mock-editor");
  return mocks.editor.mock.calls.at(-1)![0] as Record<string, unknown>;
}

function readySession(
  props: Record<string, unknown>,
  delta: unknown = { schemaVersion: 1, files: [] },
) {
  const session = { getSubmission: vi.fn().mockResolvedValue(delta) };
  act(() => {
    (props.onSessionReady as (value: typeof session) => void)(session);
  });
  return session;
}

describe("CodingActivityClient", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.seedFiles.mockReturnValue([
      { path: "main.cpp", content: "// seed", encoding: "text" },
    ]);
    mocks.buildPlan.mockReturnValue({ plan: { cases: [] } });
    mocks.createWorkspace.mockImplementation((language, files) => ({
      id: `workspace-${language}`,
      files,
    }));
    mocks.storageKey.mockImplementation((scope) => JSON.stringify(scope));
    mocks.toDelivery.mockImplementation((content) => ({
      ...content,
      schemaVersion: 2,
      exerciseType: content.ExerciseType ?? "standard-io",
      publicTests: content.Tests.Public,
    }));
    mocks.toFileRecord.mockImplementation((submission) => Object.fromEntries(
      submission.files.map((file: { path: string; content: string }) => [file.path, { content: file.content, encoding: 'text' }])));
    mocks.start.mockResolvedValue({ success: true, data: {
      submissionId: 'official-submission-1',
      definitionRevisionId: 'revision-1',
      execution: { delivery: { itemOrder: ['code'], items: { code: {
        adapterKey: 'code-assessment-type', adapterVersion: '1', learnerPayload: { definition: assignment() },
      } } } },
    } });
    mocks.editor.mockImplementation(() => <div data-testid="mock-editor" />);
  });

  it("loads the browser editor and builds the default public workspace", async () => {
    renderActivity({ assignment: assignment(null as unknown as string) });
    expect(screen.getByTestId("ide-skeleton")).toBeInTheDocument();

    const props = await editorProps();
    expect(mocks.buildPlan).toHaveBeenCalledWith(expect.anything(), "public");
    expect(mocks.createWorkspace).toHaveBeenCalledWith("cpp", {
      "main.cpp": { encoding: "text", content: "// seed" },
    });
    expect(mocks.storageKey).not.toHaveBeenCalled();
    expect(props).toMatchObject({
      mode: "learner",
      manifestUrl: "/emception/manifest.json",
      maxScore: 100,
      passingScore: 60,
      enableWorkspace: false,
    });
    expect(screen.getByRole("button", { name: "Submit" })).toBeDisabled();
  });

  it("overlays prior submissions and namespaces local drafts by user", async () => {
    mocks.start.mockResolvedValue({ success: true, data: {
      submissionId: 'official-submission-1',
      definitionRevisionId: 'revision-1',
      execution: { delivery: { itemOrder: ['code'], items: { code: {
        adapterKey: 'code-assessment-type', adapterVersion: '1', learnerPayload: { definition: assignment('c') },
      } } } },
    } });
    renderActivity({
      userId: "user-1",
      tenantId: "tenant-1",
      manifestUrl: "/manifest.json",
      assignment: assignment("c"),
      submissionFiles: [
        { path: "main.cpp", content: "// restored", encoding: "text" },
        { path: "notes.txt", content: "notes", encoding: "text" },
      ],
    });

    const props = await editorProps();
    expect(mocks.createWorkspace).toHaveBeenCalledWith("c", {
      "main.cpp": { encoding: "text", content: "// restored" },
      "notes.txt": { encoding: "text", content: "notes" },
    });
    expect(mocks.storageKey).toHaveBeenCalledWith({
      tenantId: "tenant-1",
      assessmentRevisionId: "revision-1",
      actorId: "user-1",
      attemptId: "official-submission-1",
      workspaceId: "workspace-c",
    });
    expect(props.enableWorkspace).toBe(true);
    expect(props.manifestUrl).toBe("/manifest.json");
  });

  it("renders the public estimate after an editor run", async () => {
    renderActivity();
    const props = await editorProps();
    const report = { passed: 1, failed: 0, cases: [] };
    act(() => {
      (props.onPreviewRun as (value: unknown) => void)({ report });
    });

    expect(screen.getByTestId("estimate-banner")).toBeInTheDocument();
    expect(mocks.banner).toHaveBeenCalledWith(
      expect.objectContaining({
        report,
        plan: { cases: [] },
        maxScore: 100,
        passingScore: 60,
      }),
    );
  });

  it("submits a session delta once and navigates after success", async () => {
    const pending = Promise.withResolvers<{ success: boolean }>();
    mocks.submit.mockReturnValue(pending.promise);
    renderActivity();
    const props = await editorProps();
    const submission = { schemaVersion: 1, files: [
      { fileId: "file-main", path: "main.cpp", content: "int main(){}", encoding: "text" },
    ] };
    const session = readySession(props, submission);
    const submitButton = screen.getByRole("button", { name: "Submit" });
    const form = submitButton.closest("form")!;

    fireEvent.submit(form);
    await screen.findByRole("button", { name: "Submitting…" });
    fireEvent.submit(form);
    expect(mocks.submit).toHaveBeenCalledTimes(1);

    pending.resolve({ success: true });
    expect(await screen.findByRole("status")).toHaveTextContent(
      "Submission received",
    );
    expect(session.getSubmission).toHaveBeenCalledOnce();
    expect(mocks.toFileRecord).toHaveBeenCalledWith(submission);
    expect(mocks.submit).toHaveBeenCalledWith('official-submission-1', {
      schemaVersion: 1, contentType: 'coding-assignment', payloadSchema: 'code-files/v1',
      payload: { files: { 'main.cpp': { content: 'int main(){}', encoding: 'text' } } },
    }, expect.any(String));
    expect(mocks.push).toHaveBeenCalledWith(
      "/learn/courses/game-ai/activities",
    );
  });

  it("submits an empty canonical submission", async () => {
    mocks.submit.mockResolvedValue({
      success: false,
      error: "Submission rejected.",
    });
    renderActivity();
    const props = await editorProps();
    const submission = { schemaVersion: 1, files: [] };
    readySession(props, submission);

    fireEvent.click(screen.getByRole("button", { name: "Submit" }));
    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Submission rejected.",
    );
    expect(mocks.toFileRecord).toHaveBeenCalledWith(submission);
    expect(mocks.push).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Submit" })).toBeEnabled();
  });

  it('fails closed without a published Code revision', async () => {
    mocks.start.mockResolvedValue({ success: false, error: 'No published executable revision.' });
    renderActivity();
    expect(await screen.findByRole('alert')).toHaveTextContent('No published executable revision.');
    expect(mocks.editor).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled();
    expect(mocks.start).toHaveBeenCalledWith('assessment-1', 'enrollment-1', expect.any(String));
  });

  it.each(['1', '2', '3'])('uses the frozen official definition instead of the current authoring draft for adapter %s', async (adapterVersion) => {
    const frozen = assignment();
    frozen.Grading.MaxScore = 80;
    mocks.start.mockResolvedValue({ success: true, data: {
      submissionId: 'official-submission-1',
      execution: { delivery: { itemOrder: ['code'], items: { code: {
        adapterKey: 'code-assessment-type', adapterVersion, learnerPayload: { definition: frozen },
      } } } },
    } });
    renderActivity({ assignment: assignment() });
    expect(await editorProps()).toMatchObject({ definition: frozen, maxScore: 80 });
  });

  it('rejects an unknown Code adapter before opening the editor or submitting', async () => {
    mocks.start.mockResolvedValue({ success: true, data: {
      submissionId: 'official-submission-1',
      execution: { delivery: { itemOrder: ['code'], items: { code: {
        adapterKey: 'code-assessment-type', adapterVersion: '4', learnerPayload: { definition: assignment() },
      } } } },
    } });
    renderActivity();
    expect(await screen.findByRole('alert')).toHaveTextContent('unavailable adapter version');
    expect(mocks.editor).not.toHaveBeenCalled();
    expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled();
    expect(mocks.submit).not.toHaveBeenCalled();
  });

  it.each([false, true])('restores submitted files and exposes only released feedback (released=%s)', async (released) => {
    mocks.start.mockResolvedValue({ success: true, data: {
      submissionId: 'official-submission-1',
      execution: {
        delivery: { itemOrder: ['code'], items: { code: {
          adapterKey: 'code-assessment-type', adapterVersion: '1', learnerPayload: { definition: assignment() },
        } } },
        submittedResponse: { payload: { files: { 'main.cpp': { content: '// official submission', encoding: 'text' } } } },
        instructorVisibleResult: { score: 9000, maxScore: 10000, feedback: 'Private draft feedback' },
        learnerVisibleResult: released ? { score: 7500, maxScore: 10000, feedback: 'Released feedback' } : null,
      },
    } });
    renderActivity({ submissionFiles: [{ path: 'main.cpp', content: '// mutable legacy', encoding: 'text' }] });
    readySession(await editorProps());
    expect(mocks.createWorkspace).toHaveBeenLastCalledWith('cpp', {
      'main.cpp': { content: '// official submission', encoding: 'text' },
    });
    expect(screen.getByRole('status')).toHaveTextContent('Submission received');
    expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled();
    fireEvent.submit(screen.getByRole('button', { name: 'Submit' }).closest('form')!);
    expect(mocks.submit).not.toHaveBeenCalled();
    expect(screen.queryByText('Private draft feedback')).not.toBeInTheDocument();
    if (released) {
      expect(screen.getByRole('status')).toHaveTextContent('Score: 75 / 100');
      expect(screen.getByRole('status')).toHaveTextContent('Released feedback');
    } else {
      expect(screen.getByRole('status')).toHaveTextContent('when available');
      expect(screen.queryByText('Released feedback')).not.toBeInTheDocument();
    }
  });

  it.each([
    [new Error("Network unavailable."), "Network unavailable."],
    ["offline", "Unable to submit the coding activity."],
  ])(
    "shows unexpected submission failures without trapping the form",
    async (error, expected) => {
      mocks.submit.mockRejectedValue(error);
      renderActivity();
      const props = await editorProps();
      readySession(props);

      fireEvent.click(screen.getByRole("button", { name: "Submit" }));
      expect(await screen.findByRole("alert")).toHaveTextContent(expected);
      expect(screen.getByRole("button", { name: "Submit" })).toBeEnabled();
    },
  );

  it.each([
    [new Error("Editor bundle unavailable."), "Editor bundle unavailable."],
    ["editor-import-failed", "Unable to load the coding editor."],
  ])("shows a stable editor loading failure", async (error, expected) => {
    renderActivity({ loadEditor: vi.fn().mockRejectedValue(error) });
    expect(await screen.findByRole("alert")).toHaveTextContent(expected);
  });

  it("requires a fresh editor session when the enrollment changes", async () => {
    const view = renderActivity();
    readySession(await editorProps());
    expect(screen.getByRole('button', { name: 'Submit' })).toBeEnabled();
    const firstRuntime = await mocks.start.mock.results[0]!.value;
    const pending = Promise.withResolvers<typeof firstRuntime>();
    mocks.start.mockReturnValueOnce(pending.promise);

    view.rerender(<CodingActivityClient assessmentId="assessment-1" enrollmentId="enrollment-2"
      courseId="course-1" slug="game-ai" assignment={assignment()} />);
    expect(screen.queryByTestId('mock-editor')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled();
    await waitFor(() => expect(mocks.start).toHaveBeenCalledTimes(2));
    expect(mocks.start.mock.calls[1]![1]).toBe('enrollment-2');
    expect(mocks.start.mock.calls[1]![2]).not.toBe(mocks.start.mock.calls[0]![2]);

    await act(async () => {
      pending.resolve({ ...firstRuntime, data: { ...firstRuntime.data, submissionId: 'official-submission-2' } });
    });
    const nextEditor = await editorProps();
    expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled();
    readySession(nextEditor);
    expect(screen.getByRole('button', { name: 'Submit' })).toBeEnabled();
  });

  it("does not update state after unmounting while a successful editor import settles", async () => {
    const pending = Promise.withResolvers<() => null>();
    const view = renderActivity({ loadEditor: () => pending.promise });
    view.unmount();
    pending.resolve(() => null);
    await waitFor(() => expect(mocks.editor).not.toHaveBeenCalled());
  });

  it("does not update state after unmounting while a failed editor import settles", async () => {
    const pending = Promise.withResolvers<never>();
    const view = renderActivity({ loadEditor: () => pending.promise });
    view.unmount();
    pending.reject(new Error("late failure"));
    await Promise.resolve();
  });
});
