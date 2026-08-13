import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { IdeHandle } from "@game-guild/emception-ui";

const routerMocks = vi.hoisted(() => ({
  push: vi.fn(),
}));

const ideMock = vi.hoisted(() => ({
  getFiles: vi.fn<
    () => Promise<Array<{ path: string; content: string }>>
  >(),
  addFile: vi.fn(),
  removeFile: vi.fn(),
  setFileMeta: vi.fn(),
}));

const putMock = vi.hoisted(() => vi.fn());

const assignmentSamplesMock = vi.hoisted(() => ({
  cpp: {
    workspaceConfig: {
      id: "assignment-cpp",
      label: "C++ Assignment",
      version: 1,
      files: {
        "/user/main.cpp": {
          encoding: "text",
          content: "// C++ assignment starter",
        },
      },
    },
    plan: {
      cases: [
        {
          kind: "stdio",
          name: "echo stdin",
          stdin: "hello world",
          expectedStdout: "hello world",
          weight: 2,
          hidden: false,
        },
      ],
      build: { sources: ["/user/main.cpp"] },
    },
  },
}));

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

vi.mock("next/navigation", () => ({
  useRouter: () => routerMocks,
}));

vi.mock("@game-guild/emception-ui", async () => {
  const React = await import("react");

  // ponytail: the page now composes StandardTest/FunctionalTestGroup editors
  // inside `testsPanelSlot`. The mock surfaces them by rendering props.testsPanelSlot
  // as children, and echoes `props.tests` back via getAuthoredState — mirroring the
  // real IDE's contract (T6/T7) where authored.tests === what the page passed in.
  // useImperativeHandle has no deps array → handle recreated every render →
  // closure always captures the latest props.tests.
  const Ide = React.forwardRef<IdeHandle, Record<string, unknown>>((props, ref) => {
    React.useImperativeHandle(ref, () => ({
      getFiles: ideMock.getFiles,
      getAuthoredState: async () => ({
        files: [{ path: "/user/main.cpp", content: "// edited starter" }],
        fileMeta: {},
        tests: props.tests,
        presetId: "cpp",
      }),
      runTests: vi.fn(),
      compileAndRun: vi.fn(),
      setFiles: vi.fn(),
      reset: vi.fn(),
      addFile: ideMock.addFile,
      removeFile: ideMock.removeFile,
      setFileMeta: ideMock.setFileMeta,
      getModifiedFiles: vi.fn(),
    }));
    return React.createElement(
      "div",
      { "data-testid": "mock-ide" },
      (props.testsPanelSlot as React.ReactNode) ?? null,
    );
  });
  Ide.displayName = "Ide";

  return {
    ASSIGNMENT_SAMPLES: assignmentSamplesMock,
    Ide,
  };
});

vi.mock("@/lib/coding-assignment/actions", async () => {
  const actual = await vi.importActual<
    typeof import("@/lib/coding-assignment/actions")
  >("@/lib/coding-assignment/actions");
  return {
    ...actual,
    putCodingAssignmentAction: putMock,
  };
});

import { CodingDefinitionEditor } from "./coding-definition-editor";
import { ASSIGNMENT_SAMPLES } from "@game-guild/emception-ui";
import { putCodingAssignmentAction } from "@/lib/coding-assignment/actions";
import type { CodingAssignmentContent } from "@/lib/coding-assignment/client";

const baseProps = {
  courseId: "course-1",
  assessmentId: "assessment-1",
  programId: "course-1",
  contentId: "content-1",
  assessmentTitle: "Echo Assignment",
  initialContent: null,
};

describe("CodingDefinitionEditor", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    ideMock.getFiles.mockReset();
    ideMock.getFiles.mockResolvedValue([
      { path: "/user/main.cpp", content: "// edited starter" },
    ]);
    ideMock.addFile.mockResolvedValue(undefined);
    ideMock.removeFile.mockResolvedValue(undefined);
    ideMock.setFileMeta.mockResolvedValue(undefined);
    putMock.mockReset();
    putMock.mockResolvedValue({ success: true });
  });

  it("auto-seeds the C++ sample on mount when initialContent is null", async () => {
    render(<CodingDefinitionEditor {...baseProps} initialContent={null} />);

    const sample = ASSIGNMENT_SAMPLES.cpp;
    const preview = await screen.findByTestId("json-preview");
    const previewText = preview.textContent ?? "";

    // v1 root shape
    expect(previewText).toContain('"Type": "coding-assignment"');
    expect(previewText).toContain('"Version": 1');
    expect(previewText).toContain('"Language": "cpp"');
    // Sample's main.cpp is seeded as a Public file
    expect(previewText).toContain("/user/main.cpp");
    expect(sample.workspaceConfig.id).toBeDefined();
  });

  it("adds two standard tests (one Private) and renders the v1 TestSuite JSON", async () => {
    const user = userEvent.setup();
    render(<CodingDefinitionEditor {...baseProps} />);

    await user.click(screen.getByTestId("add-standard"));
    await user.click(screen.getByTestId("add-standard"));

    // First test: stdin + Stdout, Public
    fireEvent.change(screen.getByTestId("standard-stdin-0"), {
      target: { value: "abc" },
    });
    fireEvent.change(screen.getByTestId("standard-stdout-0"), {
      target: { value: "ABC" },
    });

    // Second test: stdin + Stdout, Private
    fireEvent.change(screen.getByTestId("standard-stdin-1"), {
      target: { value: "xyz" },
    });
    fireEvent.change(screen.getByTestId("standard-stdout-1"), {
      target: { value: "XYZ" },
    });
    await user.click(screen.getByTestId("standard-visibility-1"));
    await user.click(screen.getByRole("option", { name: "Private" }));

    const previewText =
      (await screen.findByTestId("json-preview")).textContent ?? "";

    // v1 wire format — PascalCase
    expect(previewText).toContain('"Stdin": "abc"');
    expect(previewText).toContain('"Stdout": "ABC"');
    expect(previewText).toContain('"Stdin": "xyz"');
    expect(previewText).toContain('"Stdout": "XYZ"');
    // Tests bucket split by Visibility: Public has 1, Private has 1.
    expect(previewText).toMatch(/"Public":[\s\S]*"kind": "standard"/);
    expect(previewText).toMatch(/"Private":[\s\S]*"kind": "standard"/);
  });

  it("rejects negative weight client-side — Save disabled + error shown", async () => {
    const user = userEvent.setup();
    render(<CodingDefinitionEditor {...baseProps} />);

    await user.click(screen.getByTestId("add-standard"));
    fireEvent.change(screen.getByTestId("standard-weight-0"), {
      target: { value: "-5" },
    });

    expect(
      (await screen.findAllByText(/Weight must be ≥ 0/i)).length,
    ).toBeGreaterThan(0);
    expect(screen.getByTestId("save-button")).toBeDisabled();
    expect(putCodingAssignmentAction).not.toHaveBeenCalled();
  });

  it("submits a valid form and PUTs the v1 CodingAssignmentContent", async () => {
    const user = userEvent.setup();
    render(<CodingDefinitionEditor {...baseProps} />);

    await user.click(screen.getByTestId("add-standard"));
    fireEvent.change(screen.getByTestId("standard-stdin-0"), {
      target: { value: "hi" },
    });
    fireEvent.change(screen.getByTestId("standard-stdout-0"), {
      target: { value: "HI" },
    });

    await user.click(screen.getByTestId("save-button"));

    await waitFor(() => {
      expect(putCodingAssignmentAction).toHaveBeenCalledTimes(1);
    });

    const [programIdArg, contentIdArg, payloadArg] = putCodingAssignmentAction.mock.calls[0];
    expect(programIdArg).toBe("course-1");
    expect(contentIdArg).toBe("content-1");
    expect(payloadArg.Type).toBe("coding-assignment");
    expect(payloadArg.Version).toBe(1);
    expect(payloadArg.Environment.Language).toBe("cpp");
    expect(payloadArg.Tests.Public).toHaveLength(1);
    expect(payloadArg.Tests.Public[0]).toMatchObject({
      kind: "standard",
      Stdin: "hi",
      Stdout: "HI",
      Weight: 1,
    });
    // PassingScore was removed from GradingConfig by T4.
    expect(payloadArg.Grading).toEqual({ MaxScore: 100 });

    // Editor redirects back to the assessment page after a successful PUT.
    await waitFor(() => {
      expect(routerMocks.push).toHaveBeenCalledWith(
        "/dashboard/learning/courses/course-1/assessments/assessment-1",
      );
    });
  });

  it("round-trips an existing v1 CodingAssignmentContent — preserves buckets", () => {
    const initialContent: CodingAssignmentContent = {
      Type: "coding-assignment",
      Version: 1,
      Environment: {
        Language: "cpp",
        Tools: "clang",
        LibBundle: null,
        AllowStudentCreateFiles: false,
      },
      Data: {
        Files: {
          "/user/main.cpp": {
            Content: "int main() {}",
            Encoding: "text",
            Visibility: "Public",
            Modifiable: true,
          },
        },
      },
      Tests: {
        Public: [
          {
            kind: "standard",
            Name: "case-a",
            Stdin: "1",
            Stdout: "1",
            Weight: 1,
          },
        ],
        Private: [
          {
            kind: "standard",
            Name: "case-b-secret",
            Stdin: "2",
            Stdout: "2",
            Weight: 1,
          },
        ],
      },
      Grading: { MaxScore: 100 },
    };

    render(
      <CodingDefinitionEditor
        {...baseProps}
        initialContent={initialContent}
      />,
    );

    const previewText = screen.getByTestId("json-preview").textContent ?? "";
    // Both standard cases preserved across buckets.
    expect(previewText.match(/"kind": "standard"/g)).toHaveLength(2);
    expect(previewText).toContain('"Name": "case-b-secret"');
    // Buckets present
    expect(previewText).toContain('"Public":');
    expect(previewText).toContain('"Private":');
  });

  it("rejects a FunctionalTest with bad function name — Save disabled", async () => {
    const user = userEvent.setup();
    render(<CodingDefinitionEditor {...baseProps} />);

    await user.click(screen.getByTestId("add-functional"));
    fireEvent.change(screen.getByTestId("functional-functionName-0"), {
      target: { value: "bad+name" },
    });

    expect(
      (await screen.findAllByText(/Function name must match/i)).length,
    ).toBeGreaterThan(0);
    expect(screen.getByTestId("save-button")).toBeDisabled();
  });

  it("functional editor: signature parameter + case input — defaults to v1 types only", async () => {
    const user = userEvent.setup();
    render(<CodingDefinitionEditor {...baseProps} />);

    await user.click(screen.getByTestId("add-functional"));
    fireEvent.change(screen.getByTestId("functional-functionName-0"), {
      target: { value: "add" },
    });

    // Add a signature parameter named "a", type Integer.
    await user.click(screen.getByTestId("functional-add-param-0"));
    fireEvent.change(screen.getByTestId("functional-param-name-0-0"), {
      target: { value: "a" },
    });
    await user.click(screen.getByTestId("functional-param-type-0-0"));
    await user.click(screen.getByRole("option", { name: "Integer" }));

    // Add a case and supply its input value — values live in cases now (T11),
    // not on signature parameters.
    await user.click(screen.getByTestId("functional-add-case-0"));
    fireEvent.change(screen.getByTestId("functional-case-input-value-0-0-0"), {
      target: { value: "2" },
    });

    const previewText = screen.getByTestId("json-preview").textContent ?? "";
    expect(previewText).toContain('"FunctionName": "add"');
    expect(previewText).toContain('"Type": "integer"');
    expect(previewText).toContain('"Name": "a"');
    expect(previewText).toContain('"Content": 2');
  });
});
