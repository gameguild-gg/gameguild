"use client";

import React, {
  useEffect,
  useMemo,
  useRef,
  useState,
  useTransition,
  type ReactElement,
} from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft, Loader2, Plus, Save } from "lucide-react";
import {
  ASSIGNMENT_SAMPLES,
  Ide,
  type CodingLanguage,
  type IdeHandle,
  type WorkspaceConfig,
} from "@game-guild/emception-ui";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import {
  putCodingAssignmentAction,
  type CodingAssignmentContent,
  type CodingEnvironment,
  type FileVisibility,
  type FunctionParameterType,
  type StandardTest,
  type FunctionalTestGroup,
  type Test,
} from "@/lib/coding-assignment/actions";
import type { FileEncoding } from "@/lib/coding-assignment/types";
// ponytail: same import path + cast bridge as the speedgrader code-grader-panel. The web
// CodingAssignmentContent uses readonly arrays; the emception mapper input
// uses mutable arrays. Wire shape is identical at runtime.
import { buildTestPlan } from "emception/testing";
import type {
  CodingAssignmentContent as EmceptionAssignmentContent,
} from "emception/testing";
import { StandardTestEditor } from "./standard-test-editor";
import { FunctionalTestEditor } from "./functional-test-editor";

// ponytail: direct import (matches code-grader-panel pattern). The IDE manages
// its own worker boot client-side; Next's transpilePackages list already
// includes @game-guild/emception-ui so this resolves at build time.

const LANGUAGE_OPTIONS: { value: CodingLanguage; label: string }[] = [
  { value: "cpp", label: "C++ (clang + WASI)" },
  { value: "c", label: "C (clang + WASI)" },
  { value: "sdl-cpp", label: "C++ + SDL3 (emcc)" },
  { value: "raylib-cpp", label: "C++ + raylib (emcc)" },
];

const DEFAULT_TOOLS: Record<CodingLanguage, string> = {
  cpp: "clang",
  c: "clang",
  "sdl-cpp": "emcc",
  "raylib-cpp": "emcc",
};

const FUNCTION_NAME_RE = /^[A-Za-z_][A-Za-z0-9_]*$/;

const AUTOSAVE_DELAY_MS = 30_000;

/** In-memory file row mirrored from initialContent + IDE getAuthoredState(). */
interface AssignmentFileRow {
  path: string;
  content: string;
  encoding: FileEncoding;
  visibility: FileVisibility;
  modifiable: boolean;
}

interface EditorProps {
  courseId: string;
  assessmentId: string;
  programId: string;
  contentId: string | null;
  assessmentTitle: string;
  initialContent: CodingAssignmentContent | null;
}

interface TestRow {
  test: Test;
  visibility: FileVisibility;
}

export function CodingDefinitionEditor({
  courseId,
  assessmentId,
  programId,
  contentId,
  assessmentTitle,
  initialContent,
}: EditorProps): ReactElement {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const ideRef = useRef<IdeHandle | null>(null);

  // ── Editor state ──
  const initialLang = (initialContent?.Environment.Language as CodingLanguage) ?? "cpp";
  const [language, setLanguage] = useState<CodingLanguage>(initialLang);
  const [allowStudentCreateFiles, setAllowStudentCreateFiles] = useState<boolean>(
    initialContent?.Environment.AllowStudentCreateFiles ?? false,
  );
  const [fileRows, setFileRows] = useState<AssignmentFileRow[]>(() =>
    initialContent
      ? Object.entries(initialContent.Data.Files).map(([path, meta]) => ({
          path,
          content: meta.Content,
          encoding: meta.Encoding ?? "text",
          visibility: meta.Visibility,
          modifiable: meta.Modifiable,
        }))
      : [],
  );
  const [testRows, setTestRows] = useState<TestRow[]>(() =>
    initialContent
      ? [
          ...initialContent.Tests.Public.map((test) => ({ test, visibility: "Public" as FileVisibility })),
          ...initialContent.Tests.Private.map((test) => ({ test, visibility: "Private" as FileVisibility })),
        ]
      : [],
  );
  const maxScore = initialContent?.Grading.MaxScore ?? 100;

  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  // ── Autosave bookkeeping ──
  const autosaveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const dirtyRef = useRef(false);
  const savingRef = useRef(false);
  const hydratedRef = useRef(false);
  const seededRef = useRef(false);
  const performSaveRef = useRef<() => Promise<void>>(async () => {});

  // ── WorkspaceConfig for the IDE — derive from language preset + files ──
  const workspaceConfig = useMemo<WorkspaceConfig | null>(() => {
    const sample = ASSIGNMENT_SAMPLES[language as CodingLanguage];
    if (!sample) return null;
    const files: WorkspaceConfig["files"] = {};
    for (const row of fileRows) {
      files[row.path] = { encoding: row.encoding, content: row.content };
    }
    // ponytail: if no files yet, fall back to the preset so the IDE has something to boot with.
    return {
      ...sample.workspaceConfig,
      files: Object.keys(files).length > 0 ? files : sample.workspaceConfig.files,
    };
  }, [language, fileRows]);

  // ── IDE authoring props ──
  const fileMeta = useMemo(
    () =>
      Object.fromEntries(
        fileRows.map((r) => [
          r.path,
          { visibility: r.visibility, modifiable: r.modifiable },
        ]),
      ) as Record<string, { visibility: FileVisibility; modifiable: boolean }>,
    [fileRows],
  );

  const testSuite = useMemo<{ Public: Test[]; Private: Test[] }>(() => {
    const Public: Test[] = [];
    const Private: Test[] = [];
    for (const row of testRows) {
      (row.visibility === "Public" ? Public : Private).push(row.test);
    }
    return { Public, Private };
  }, [testRows]);

  // ── Derived test plan for in-IDE "Run Tests" ──
  // Rebuilds the v1 content from current rows + runs buildTestPlan so the Ide
  // gets a fresh GradingPlan on every authoring edit. Returns undefined when
  // there are no tests or the plan cannot be built (e.g. a functional group
  // with zero cases mid-authoring) — the Ide then hides the Run Tests button.
  const authoredTestPlan = useMemo(() => {
    if (testRows.length === 0) return undefined;
    try {
      const content = buildContent({
        language,
        allowStudentCreateFiles,
        fileRows,
        testRows,
        maxScore,
      });
      const { plan, generatedFiles } = buildTestPlan(
        content as unknown as EmceptionAssignmentContent,
        { mode: "full" },
      );
      return { ...plan, generatedFiles };
    } catch {
      return undefined;
    }
  }, [language, allowStudentCreateFiles, fileRows, testRows, maxScore]);

  // ── Auto-seed default sample on first mount when no initialContent ──
  // The Language preset Card used to host this seed via handleLanguageChange;
  // the page no longer has that surface (preset picker moved into the IDE
  // header). Seed once on mount so fileRows + testRows aren't empty.
  useEffect(() => {
    if (initialContent) return;
    seededRef.current = true;
    handleLanguageChange(initialLang);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ── Validation ──
  const validationErrors = useMemo(
    () => validateAll({ testRows, fileRows }),
    [testRows, fileRows],
  );
  const isValid = validationErrors.length === 0;

  // ── Handlers ──
  function handleLanguageChange(next: CodingLanguage) {
    setLanguage(next);
    const sample = ASSIGNMENT_SAMPLES[next as CodingLanguage];
    if (!sample) return;
    const rows: AssignmentFileRow[] = Object.entries(
      sample.workspaceConfig.files,
    ).map(([path, bundle]) => ({
      path,
      content: bundle.content,
      encoding: bundle.encoding,
      visibility: "Public" as FileVisibility,
      modifiable: true,
    }));
    setFileRows(rows);
    // ponytail: do NOT seed testRows here — authoring flows expect add-*
    // clicks to produce row index 0. Seeding files only keeps the IDE useful
    // on first mount without breaking test-row assumptions.
  }

  function handleAddStandard() {
    setTestRows((prev) => [
      ...prev,
      {
        test: {
          kind: "standard",
          Name: "",
          Stdin: "",
          Stdout: "",
          Weight: 1,
        },
        visibility: "Public",
      },
    ]);
  }

  function handleAddFunctional() {
    setTestRows((prev) => [
      ...prev,
      {
        test: {
          kind: "functional",
          Name: "",
          Weight: 1,
          Function: {
            FunctionName: "",
            Parameters: [],
            ReturnType: { Type: "integer" },
          },
          Cases: [],
        },
        visibility: "Public",
      },
    ]);
  }

  function handleTestChange(idx: number, patch: Partial<Test>) {
    setTestRows((prev) =>
      prev.map((row, i) =>
        i === idx ? { ...row, test: { ...row.test, ...patch } as Test } : row,
      ),
    );
  }

  function handleTestVisibilityChange(idx: number, next: FileVisibility) {
    setTestRows((prev) =>
      prev.map((row, i) => (i === idx ? { ...row, visibility: next } : row)),
    );
  }

  function handleRemoveTest(idx: number) {
    setTestRows((prev) => prev.filter((_, i) => i !== idx));
  }

  function handleFileMetaChange(
    path: string,
    patch: Partial<{ visibility: FileVisibility; modifiable: boolean }>,
  ) {
    setFileRows((prev) =>
      prev.map((row) => (row.path === path ? { ...row, ...patch } : row)),
    );
  }

  // ponytail: IDE doesn't mutate tests outside the slot; kept for API parity.
  // If a future IDE feature needs to push tests inward, narrow + setTestRows here.
  function handleTestsChange(_next: unknown) {
    /* no-op */
  }

  async function performSave() {
    if (savingRef.current) return;
    if (!isValid || !contentId) {
      setError(contentId ? "Resolve validation errors before saving." : "No content item linked to this assessment.");
      return;
    }
    savingRef.current = true;
    setError(null);
    setSaved(false);

    // Pull the latest authored state from the IDE: files + fileMeta + tests + presetId.
    let liveFileRows = fileRows;
    let liveTestRows = testRows;
    let liveLanguage = language;

    if (ideRef.current) {
      try {
        const authored = await ideRef.current.getAuthoredState();
        const liveMap = new Map(authored.files.map((f) => [f.path, f]));
        const knownPaths = new Set(fileRows.map((r) => r.path));
        // Refresh content + meta of known rows.
        const refreshed = fileRows.map((row) => ({
          ...row,
          content: liveMap.get(row.path)?.content ?? row.content,
          encoding: liveMap.get(row.path)?.encoding ?? row.encoding,
          visibility: authored.fileMeta[row.path]?.visibility ?? row.visibility,
          modifiable: authored.fileMeta[row.path]?.modifiable ?? row.modifiable,
        }));
        // Carry IDE-only additions (files created in-frame via FileExplorer).
        const additions: AssignmentFileRow[] = authored.files
          .filter((f) => !knownPaths.has(f.path))
          .map((f) => ({
            path: f.path,
            content: f.content,
            encoding: f.encoding ?? "text",
            visibility: (authored.fileMeta[f.path]?.visibility ?? "Public") as FileVisibility,
            modifiable: authored.fileMeta[f.path]?.modifiable ?? true,
          }));
        liveFileRows = [...refreshed, ...additions];

        // authored.tests is opaque (echoed from our `tests` prop); narrow to TestSuite.
        const suite = authored.tests as { Public?: Test[]; Private?: Test[] } | undefined;
        if (suite) {
          liveTestRows = [
            ...(suite.Public ?? []).map((test) => ({ test, visibility: "Public" as FileVisibility })),
            ...(suite.Private ?? []).map((test) => ({ test, visibility: "Private" as FileVisibility })),
          ];
        }

        // Guard: authored.presetId is the workspaceConfig id — only trust it
        // when it maps to a known language, otherwise DEFAULT_TOOLS lookup
        // yields undefined and the backend rejects the payload.
        if (
          authored.presetId &&
          LANGUAGE_OPTIONS.some((o) => o.value === authored.presetId)
        ) {
          liveLanguage = authored.presetId as CodingLanguage;
        }
      } catch {
        // ponytail: IDE not booted — keep authored state from React.
      }
    }

    const content = buildContent({
      language: liveLanguage,
      allowStudentCreateFiles,
      fileRows: liveFileRows,
      testRows: liveTestRows,
      maxScore,
    });

    // 10MB budget — matches the backend's files_too_large FluentValidation rule.
    if (JSON.stringify(content).length > 10_000_000) {
      setError(
        "Assignment exceeds 10MB total (texts+images+tests). Remove some files.",
      );
      savingRef.current = false;
      return;
    }

    startTransition(async () => {
      try {
        const result = await putCodingAssignmentAction(programId, contentId, content);
        if (!result.success) {
          setError(result.error);
          return;
        }
        setSaved(true);
        dirtyRef.current = false;
        if (autosaveTimerRef.current) {
          clearTimeout(autosaveTimerRef.current);
          autosaveTimerRef.current = null;
        }
      } finally {
        savingRef.current = false;
      }
    });
  }

  useEffect(() => {
    performSaveRef.current = performSave;
  });

  // Debounced autosave: 30s after the LAST authored change. The cleanup clears
  // the pending timer on the next change (reset) and on unmount (teardown).
  // ponytail: a change landing while a save is in flight gets its timer
  // canceled by the success path — tiny window, next change re-arms.
  useEffect(() => {
    if (!hydratedRef.current) {
      hydratedRef.current = true;
      return;
    }
    if (seededRef.current) {
      seededRef.current = false;
      return;
    }
    dirtyRef.current = true;
    autosaveTimerRef.current = setTimeout(() => {
      autosaveTimerRef.current = null;
      if (!isValid || !contentId) return;
      void performSaveRef.current();
    }, AUTOSAVE_DELAY_MS);
    return () => {
      if (autosaveTimerRef.current) {
        clearTimeout(autosaveTimerRef.current);
        autosaveTimerRef.current = null;
      }
    };
  }, [testRows, fileRows, language, allowStudentCreateFiles, isValid, contentId]);

  function handleBack() {
    router.push(
      `/dashboard/learning/courses/${encodeURIComponent(courseId)}/assessments/${assessmentId}`,
    );
  }

  const stdioErrorCount = validationErrors.length;

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="sm" onClick={handleBack}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back
        </Button>
        <div className="flex-1">
          <p className="text-muted-foreground text-sm">
            Coding Definition Editor
          </p>
          <h1 className="text-2xl font-bold">{assessmentTitle}</h1>
        </div>
        {saved && <p className="text-sm text-green-600">Saved.</p>}
        {error && <p className="text-destructive text-sm">{error}</p>}
        <Badge variant="secondary">{language}</Badge>
        <Button
          onClick={performSave}
          disabled={!isValid || isPending}
          data-testid="save-button"
        >
          {isPending ? (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          ) : (
            <Save className="mr-2 h-4 w-4" />
          )}
          Save Definition
        </Button>
      </div>

      {stdioErrorCount > 0 && (
        <div className="text-destructive text-sm space-y-1">
          {validationErrors.slice(0, 5).map((e, i) => (
            <p key={i}>{e.message}</p>
          ))}
        </div>
      )}

      {workspaceConfig && (
        <Card>
          <CardHeader>
            <CardTitle>Workspace</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div data-testid="ide-mount" className="h-[70vh] min-h-[500px]">
              <Ide
                ref={ideRef}
                workspaceConfig={workspaceConfig}
                assignmentToken={assessmentId}
                presetOptions={LANGUAGE_OPTIONS}
                onPresetChange={(v) => handleLanguageChange(v as CodingLanguage)}
                fileMeta={fileMeta}
                onFileMetaChange={handleFileMetaChange}
                allowCreateFiles={allowStudentCreateFiles}
                onAllowCreateFilesChange={setAllowStudentCreateFiles}
                tests={testSuite}
                onTestsChange={handleTestsChange}
                testPlan={authoredTestPlan}
                testMode="full"
                maxScore={maxScore}
                testsPanelSlot={
                  <div className="space-y-4">
                    {testRows.length === 0 && (
                      <p
                        className="text-muted-foreground text-sm"
                        data-testid="empty-tests"
                      >
                        No tests yet. Add a Standard or Functional test below.
                      </p>
                    )}
                    {testRows.map((row, i) =>
                      row.test.kind === "standard" ? (
                        <StandardTestEditor
                          key={i}
                          index={i}
                          test={row.test as StandardTest}
                          visibility={row.visibility}
                          errors={validationErrors.filter((e) =>
                            e.field.startsWith(`tests[${i}]`),
                          )}
                          onChange={handleTestChange}
                          onVisibilityChange={handleTestVisibilityChange}
                          onRemove={handleRemoveTest}
                        />
                      ) : (
                        <FunctionalTestEditor
                          key={i}
                          index={i}
                          test={row.test as FunctionalTestGroup}
                          visibility={row.visibility}
                          errors={validationErrors.filter((e) =>
                            e.field.startsWith(`tests[${i}]`),
                          )}
                          onChange={handleTestChange}
                          onVisibilityChange={handleTestVisibilityChange}
                          onRemove={handleRemoveTest}
                        />
                      ),
                    )}
                    <div className="flex flex-wrap gap-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleAddStandard}
                        data-testid="add-standard"
                      >
                        <Plus className="mr-1 h-3 w-3" /> Standard test
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleAddFunctional}
                        data-testid="add-functional"
                      >
                        <Plus className="mr-1 h-3 w-3" /> Functional test
                      </Button>
                    </div>
                  </div>
                }
              />
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

// ── Validation helpers ────────────────────────────────────────────────────

interface ValidationState {
  testRows: TestRow[];
  fileRows: AssignmentFileRow[];
}

interface ValidationErr {
  field: string;
  code: string;
  message: string;
}

/**
 * Mirror of Task 1 FluentValidation rules — runs on the client before PUT so
 * the backend never sees a malformed payload. Error codes match the backend's
 * `WithErrorCode` strings exactly.
 */
function validateAll(state: ValidationState): ValidationErr[] {
  const errors: ValidationErr[] = [];

  // Per-file: Private + Modifiable makes no sense.
  state.fileRows.forEach((f, i) => {
    if (f.visibility === "Private" && f.modifiable) {
      errors.push({
        field: `Data.Files[${i}]`,
        code: "private_file_not_modifiable",
        message: `File "${f.path}" cannot be both Private and Modifiable.`,
      });
    }
  });

  // Per-test rules
  state.testRows.forEach((row, i) => {
    const path = `tests[${i}]`;
    const t = row.test;
    if (t.Weight != null && t.Weight < 0) {
      errors.push({
        field: `${path}.Weight`,
        code: "weight_non_negative",
        message: "Weight must be ≥ 0.",
      });
    }
    if (t.kind === "standard") {
      const std = t as StandardTest;
      if (!std.Stdout || std.Stdout.length === 0) {
        errors.push({
          field: `${path}.Stdout`,
          code: "standard_stdout_required",
          message: "Standard test requires non-empty Stdout.",
        });
      }
    } else if (t.kind === "functional") {
      const fn = t as FunctionalTestGroup;
      if (!FUNCTION_NAME_RE.test(fn.Function.FunctionName)) {
        errors.push({
          field: `${path}.Function.FunctionName`,
          code: "invalid_function_name",
          message:
            "Function name must match ^[A-Za-z_][A-Za-z0-9_]*$ (C/C++ global scope).",
        });
      }
      for (const p of fn.Function.Parameters) {
        // The select restricts to v1 4 types — this catches a hand-built payload.
        if (!isV1ParamType(p.Type)) {
          errors.push({
            field: `${path}.Function.Parameters`,
            code: "functional_param_type_not_supported_v1",
            message: `Parameter type "${p.Type}" is not supported in v1.`,
          });
        }
      }
      if (!isV1ParamType(fn.Function.ReturnType.Type)) {
        errors.push({
          field: `${path}.Function.ReturnType`,
          code: "functional_param_type_not_supported_v1",
          message: `Return type "${fn.Function.ReturnType.Type}" is not supported in v1.`,
        });
      }
      if (fn.Cases.length === 0) {
        errors.push({
          field: `${path}.Cases`,
          code: "at_least_one_case",
          message: "Functional test requires at least one case.",
        });
      }
    }
  });

  return errors;
}

function isV1ParamType(t: string): t is FunctionParameterType {
  return t === "string" || t === "boolean" || t === "integer" || t === "float";
}

// ── Payload assembly ──────────────────────────────────────────────────────

interface BuildArgs {
  language: CodingLanguage;
  allowStudentCreateFiles: boolean;
  fileRows: AssignmentFileRow[];
  testRows: TestRow[];
  maxScore: number;
}

function buildContent(args: BuildArgs): CodingAssignmentContent {
  const files: Record<string, { Content: string; Encoding: FileEncoding; Visibility: FileVisibility; Modifiable: boolean }> = {};
  for (const row of args.fileRows) {
    files[row.path] = {
      Content: row.content,
      Encoding: row.encoding,
      Visibility: row.visibility,
      Modifiable: row.modifiable,
    };
  }

  const pub: Test[] = [];
  const priv: Test[] = [];
  for (const row of args.testRows) {
    (row.visibility === "Public" ? pub : priv).push(row.test);
  }

  const environment: CodingEnvironment = {
    Language: args.language,
    Tools: DEFAULT_TOOLS[args.language],
    LibBundle: null,
    AllowStudentCreateFiles: args.allowStudentCreateFiles,
  };

  return {
    Type: "coding-assignment",
    Version: 1,
    Environment: environment,
    Data: { Files: files },
    Tests: { Public: pub, Private: priv },
    Grading: { MaxScore: args.maxScore },
  };
}
