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
}));

const putMock = vi.hoisted(() => vi.fn());

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
  const actual = await vi.importActual<
    typeof import("@game-guild/emception-ui")
  >("@game-guild/emception-ui");

  const Ide = React.forwardRef<IdeHandle>((_props, ref) => {
    React.useImperativeHandle(ref, () => ({
      getFiles: ideMock.getFiles,
      runTests: vi.fn(),
      compileAndRun: vi.fn(),
      setFiles: vi.fn(),
      reset: vi.fn(),
    }));
    return React.createElement("div", { "data-testid": "mock-ide" });
  });
  Ide.displayName = "Ide";

  return {
    ...actual,
    Ide,
  };
});

vi.mock("@/lib/emception/put-coding-definition", () => ({
  putCodingDefinition: putMock,
}));

import { CodingDefinitionEditor } from "./coding-definition-editor";
import { ASSIGNMENT_SAMPLES } from "@game-guild/emception-ui";
import { putCodingDefinition } from "@/lib/emception/put-coding-definition";

const baseProps = {
  courseId: "course-1",
  assessmentId: "assessment-1",
  assessmentTitle: "Echo Assignment",
  initialDefinition: null,
};

describe("CodingDefinitionEditor", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    ideMock.getFiles.mockReset();
    ideMock.getFiles.mockResolvedValue([
      { path: "/user/main.cpp", content: "// edited starter" },
    ]);
    putMock.mockReset();
    putMock.mockResolvedValue({ success: true });
  });

  it("loads the C++ sample when language changes to cpp", async () => {
    const user = userEvent.setup();
    // Start from a non-cpp language so the change handler actually fires.
    render(
      <CodingDefinitionEditor
        {...baseProps}
        initialDefinition={{
          kind: "coding",
          language: "c",
          workspaceConfig: null,
          testPlan: { cases: [] },
          maxScore: 100,
          passingScore: 60,
        }}
      />,
    );

    await user.click(screen.getByTestId("language-select"));
    await user.click(screen.getByText("C++ (clang + WASI)"));

    // Workspace config + cases should now reflect ASSIGNMENT_SAMPLES.cpp.
    const sample = ASSIGNMENT_SAMPLES.cpp;
    const preview = await screen.findByTestId("json-preview");
    const previewText = preview.textContent ?? "";

    expect(previewText).toContain('"language": "cpp"');
    expect(previewText).toContain(sample.workspaceConfig.id);
    // The sample's stdio case is loaded into the test-case builder.
    expect(previewText).toContain('"kind": "stdio"');
    expect(previewText).toContain("echo stdin");
  });

  it("adds two stdio cases (one hidden) and renders the v2 TestPlan JSON", async () => {
    const user = userEvent.setup();
    render(<CodingDefinitionEditor {...baseProps} />);

    await user.click(screen.getByTestId("add-case-stdio"));
    await user.click(screen.getByTestId("add-case-stdio"));

    // First case: stdin + expectedStdout, visible
    fireEvent.change(screen.getByTestId("case-stdin-0"), {
      target: { value: "abc" },
    });
    fireEvent.change(screen.getByTestId("case-expectedStdout-0"), {
      target: { value: "ABC" },
    });

    // Second case: stdin + expectedStdout, hidden
    fireEvent.change(screen.getByTestId("case-stdin-1"), {
      target: { value: "xyz" },
    });
    fireEvent.change(screen.getByTestId("case-expectedStdout-1"), {
      target: { value: "XYZ" },
    });
    await user.click(screen.getByTestId("case-hidden-1"));

    const previewText =
      (await screen.findByTestId("json-preview")).textContent ?? "";

    expect(previewText).toContain('"stdin": "abc"');
    expect(previewText).toContain('"expectedStdout": "ABC"');
    expect(previewText).toContain('"stdin": "xyz"');
    expect(previewText).toContain('"expectedStdout": "XYZ"');
    // Hidden flag is on case[1] only.
    expect(previewText).toMatch(/"cases":[\s\S]*"hidden": true/);
  });

  it("rejects negative weight client-side — Save disabled + error shown", async () => {
    const user = userEvent.setup();
    render(<CodingDefinitionEditor {...baseProps} />);

    await user.click(screen.getByTestId("add-case-stdio"));
    fireEvent.change(screen.getByTestId("case-weight-0"), {
      target: { value: "-5" },
    });

    expect(
      (await screen.findAllByText(/Weight must be ≥ 0/i)).length,
    ).toBeGreaterThan(0);
    expect(screen.getByTestId("save-button")).toBeDisabled();
    expect(putCodingDefinition).not.toHaveBeenCalled();
  });

  it("submits a valid form and PUTs the v2 definition", async () => {
    const user = userEvent.setup();
    render(<CodingDefinitionEditor {...baseProps} />);

    // Add a single valid stdio case.
    await user.click(screen.getByTestId("add-case-stdio"));
    fireEvent.change(screen.getByTestId("case-stdin-0"), {
      target: { value: "hi" },
    });
    fireEvent.change(screen.getByTestId("case-expectedStdout-0"), {
      target: { value: "HI" },
    });

    await user.click(screen.getByTestId("save-button"));

    await waitFor(() => {
      expect(putCodingDefinition).toHaveBeenCalledTimes(1);
    });

    const [assessmentIdArg, payloadArg] =
      putCodingDefinition.mock.calls[0];
    expect(assessmentIdArg).toBe("assessment-1");
    expect(payloadArg.kind).toBe("coding");
    expect(payloadArg.language).toBe("cpp");
    expect(payloadArg.definitionSchemaVersion).toBe(2);
    expect(payloadArg.testPlan.cases).toHaveLength(1);
    expect(payloadArg.testPlan.cases[0]).toMatchObject({
      kind: "stdio",
      stdin: "hi",
      expectedStdout: "HI",
      weight: 1,
      hidden: false,
    });

    // Editor redirects back to the assessment page after a successful PUT.
    await waitFor(() => {
      expect(routerMocks.push).toHaveBeenCalledWith(
        "/dashboard/learning/courses/course-1/assessments/assessment-1",
      );
    });
  });

  it("round-trips an existing definition — preserves case count + hidden flag", async () => {
    render(
      <CodingDefinitionEditor
        {...baseProps}
        initialDefinition={{
          kind: "coding",
          language: "cpp",
          workspaceConfig: ASSIGNMENT_SAMPLES.cpp.workspaceConfig as unknown as Record<string, unknown>,
          testPlan: {
            cases: [
              {
                kind: "stdio",
                name: "case-a",
                stdin: "1",
                expectedStdout: "1",
                weight: 1,
                hidden: false,
              },
              {
                kind: "stdio",
                name: "case-b-secret",
                stdin: "2",
                expectedStdout: "2",
                weight: 1,
                hidden: true,
              },
            ],
          },
          maxScore: 100,
          passingScore: 50,
        }}
      />,
    );

    const previewText = screen.getByTestId("json-preview").textContent ?? "";
    // Both cases preserved.
    expect(previewText.match(/"kind": "stdio"/g)).toHaveLength(2);
    // Hidden flag preserved on the second case.
    expect(previewText).toContain('"name": "case-b-secret"');
    expect(previewText).toContain('"hidden": true');
  });

  it("renders validation errors for all 4 kind-specific failure modes", async () => {
    const user = userEvent.setup();
    render(<CodingDefinitionEditor {...baseProps} />);

    // stdio without expectedStdout
    await user.click(screen.getByTestId("add-case-stdio"));
    // stdio-file without files
    await user.click(screen.getByTestId("add-case-stdio-file"));
    // clang-query without matcher/expect
    await user.click(screen.getByTestId("add-case-clang-query"));
    // doctest without sourceFiles
    await user.click(screen.getByTestId("add-case-doctest"));

    expect(
      (await screen.findAllByText(/requires non-empty expected stdout/i))
        .length,
    ).toBeGreaterThan(0);
    expect(
      screen.getAllByText(/requires inFile \+ expectedOutFile/i).length,
    ).toBeGreaterThan(0);
    expect(
      screen.getAllByText(/requires matcher \+ expect/i).length,
    ).toBeGreaterThan(0);
    expect(
      screen.getAllByText(/requires non-empty sourceFiles/i).length,
    ).toBeGreaterThan(0);
    expect(screen.getByTestId("save-button")).toBeDisabled();
  });
});
