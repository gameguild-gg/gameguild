/**
 * Pure validation + payload helpers for the coding-definition editor.
 *
 * Extracted from `coding-definition-editor.tsx` so the rules can be
 * unit-tested without rendering React, and so the editor file stays
 * under the LOC ceiling. Error codes mirror the Task 6 backend
 * FluentValidation rules exactly — see `CodingAssignmentDefinitionValidator.cs`.
 */

import type { GradingCase, WorkspaceConfig } from "@game-guild/emception-ui";
import type { CodingLanguageId, CodingDefinitionPayload } from "@/lib/emception/put-coding-definition";

export type CaseKind =
  | "stdio"
  | "stdio-file"
  | "clang-query"
  | "doctest"
  | "custom";

export interface ValidationError {
  field: string;
  code: string;
  message: string;
}

export function makeEmptyCase(kind: CaseKind): GradingCase {
  switch (kind) {
    case "stdio":
      return {
        kind,
        name: "",
        stdin: "",
        expectedStdout: "",
        weight: 1,
        hidden: false,
      };
    case "stdio-file":
      return {
        kind,
        name: "",
        inFile: "",
        expectedOutFile: "",
        weight: 1,
        hidden: false,
      };
    case "clang-query":
      return {
        kind,
        name: "",
        matcher: "",
        expect: "found",
        weight: 1,
        hidden: false,
      };
    case "doctest":
      return {
        kind,
        name: "",
        sourceFiles: [],
        weight: 1,
        hidden: false,
      };
    case "custom":
      return { kind, name: "", weight: 1, hidden: false };
  }
}

/**
 * Mirror of Task 6 FluentValidation rules — runs on the client before the
 * PUT so the backend never sees a malformed payload. Error codes match the
 * backend's `WithErrorCode` strings exactly so failure surfaces line up.
 */
export function validateAll(state: {
  maxScore: number;
  passingScore: number;
  cases: GradingCase[];
}): ValidationError[] {
  const errors: ValidationError[] = [];

  if (!(state.maxScore > 0)) {
    errors.push({
      field: "maxScore",
      code: "max_score_positive",
      message: "Max score must be greater than 0.",
    });
  }
  if (state.passingScore < 0) {
    errors.push({
      field: "passingScore",
      code: "passing_score_non_negative",
      message: "Passing score must be ≥ 0.",
    });
  }
  if (state.passingScore > state.maxScore) {
    errors.push({
      field: "passingScore",
      code: "passing_score_within_max",
      message: "Passing score cannot exceed max score.",
    });
  }

  if (state.cases.length === 0) {
    errors.push({
      field: "cases",
      code: "at_least_one_case",
      message: "At least one test case is required.",
    });
    return errors;
  }

  state.cases.forEach((c, i) => {
    const path = `cases[${i}]`;
    if (c.weight != null && c.weight < 0) {
      errors.push({
        field: `${path}.weight`,
        code: "weight_non_negative",
        message: "Weight must be ≥ 0.",
      });
    }
    switch (c.kind) {
      case "stdio":
        if (
          !c.expectedStdout ||
          (typeof c.expectedStdout === "string" &&
            c.expectedStdout.length === 0)
        ) {
          errors.push({
            field: `${path}.expectedStdout`,
            code: "stdio_expected_stdout_required",
            message: "Stdio case requires non-empty expected stdout.",
          });
        }
        break;
      case "stdio-file":
        if (!c.inFile || !c.expectedOutFile) {
          errors.push({
            field: `${path}`,
            code: "stdio_file_fields_required",
            message: "Stdio-file case requires inFile + expectedOutFile.",
          });
        }
        break;
      case "clang-query":
        if (!c.matcher || !c.expect) {
          errors.push({
            field: `${path}`,
            code: "clang_query_fields_required",
            message: "ClangQuery case requires matcher + expect.",
          });
        }
        break;
      case "doctest":
        if (!c.sourceFiles || c.sourceFiles.length === 0) {
          errors.push({
            field: `${path}.sourceFiles`,
            code: "doctest_source_files_required",
            message: "Doctest case requires non-empty sourceFiles.",
          });
        }
        break;
      case "custom":
        break;
    }
  });

  return errors;
}

/** Build the v2 definition object shown in the live JSON preview and sent on Save. */
export function buildPreview(state: {
  language: CodingLanguageId;
  workspaceConfig: WorkspaceConfig | null;
  cases: GradingCase[];
  build: Record<string, unknown> | null;
  maxScore: number;
  passingScore: number;
}): CodingDefinitionPayload {
  return {
    kind: "coding",
    language: state.language,
    workspaceConfig:
      (state.workspaceConfig as unknown as Record<string, unknown>) ?? {},
    testPlan: {
      build: state.build ?? undefined,
      cases: state.cases.map((c) =>
        stripUndefined(c as unknown as Record<string, unknown>),
      ),
    },
    maxScore: state.maxScore,
    passingScore: state.passingScore,
    definitionSchemaVersion: 2,
  };
}

function stripUndefined<T extends Record<string, unknown>>(
  obj: T,
): Partial<T> {
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(obj)) {
    if (v !== undefined) out[k] = v;
  }
  return out as Partial<T>;
}
