import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";

const assessmentEditorMock = vi.hoisted(() => ({
  props: null as {
    mode?: string;
    definition?: unknown;
    workspaceConfig?: { files?: Record<string, unknown> };
    onPreviewRun?: (result: {
      report: { cases: unknown[] };
      score: { score: number };
    }) => void;
  } | null,
}));

vi.mock("@game-guild/coding-surface", () => ({
  CodingSurface: (props: typeof assessmentEditorMock.props) => {
    assessmentEditorMock.props = props;
    return (
      <button
        data-testid="mock-run-full-tests"
        type="button"
        onClick={() =>
          props?.onPreviewRun?.({
            report: { cases: [] },
            score: { score: 67 },
          })
        }
      >
        Run full tests
      </button>
    );
  },
  createCodingWorkspaceConfig: vi.fn(
    (_language: string, files: Record<string, unknown>) => ({
      id: "cpp",
      label: "C++",
      compile: { tool: "clang", args: [], output: "main.wasm" },
      run: { type: "wasi-terminal" },
      features: {},
      files,
    }),
  ),
}));

import {
  CodeGraderPanel,
  mergeWorkspaceWithSubmission,
} from "./code-grader-panel";
import type { CodingAssignmentContent } from "@/lib/coding-assignment/client";

const assignment: CodingAssignmentContent = {
  Type: "coding-assignment",
  Version: 1,
  ExerciseType: "standard-io",
  Environment: {
    Language: "cpp",
    Tools: "clang",
    AllowStudentCreateFiles: true,
  },
  Data: {
    Files: {
      "/home/user/main.cpp": {
        Content: "// starter",
        Encoding: "text",
        Visibility: "Public",
        Modifiable: true,
      },
      "/home/user/secret.cpp": {
        Content: "// instructor only",
        Encoding: "text",
        Visibility: "Private",
        Modifiable: false,
      },
    },
  },
  Tests: {
    Public: [
      { kind: "standard", Name: "echo", Weight: 1, Stdin: "", Stdout: "" },
    ],
    Private: [],
  },
  Grading: { MaxScore: 100 },
};

describe("CodeGraderPanel", () => {
  it("keeps private workspace files out of the grader UI and delegates full execution", () => {
    const onComputedScore = vi.fn();
    render(
      <CodeGraderPanel
        assignment={assignment}
        submittedFiles={[
          { path: "/home/user/main.cpp", content: "// submission" },
          { path: "/home/user/secret.cpp", content: "// malicious override" },
        ]}
        maxScore={100}
        manifestUrl="/cdn/manifest.json"
        submissionId="submission-1"
        onComputedScore={onComputedScore}
      />,
    );

    expect(assessmentEditorMock.props?.mode).toBe("grader");
    expect(assessmentEditorMock.props?.definition).toMatchObject({
      schemaVersion: 2,
      exerciseType: "standard-io",
      files: expect.arrayContaining([
        expect.objectContaining({ path: "/home/user/main.cpp", content: "// submission" }),
        expect.objectContaining({ path: "/home/user/secret.cpp", content: "// instructor only", visibility: "private" }),
      ]),
    });
    expect(assessmentEditorMock.props?.workspaceConfig?.files).toEqual({
      "/home/user/main.cpp": { encoding: "text", content: "// submission" },
    });

    fireEvent.click(screen.getByTestId("mock-run-full-tests"));
    expect(onComputedScore).toHaveBeenCalledWith(
      expect.objectContaining({ score: 67 }),
    );
  });

  it("never lets a submitted file override an instructor-private file", () => {
    const merged = mergeWorkspaceWithSubmission(assignment, [
      { path: "/home/user/secret.cpp", content: "// malicious override" },
    ]);

    expect(merged).toContainEqual({
      path: "/home/user/secret.cpp",
      content: "// instructor only",
    });
  });
});
