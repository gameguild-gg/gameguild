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
  CodingAssessmentEditor,
  type CodingLanguage,
} from "@game-guild/emception-ui";
import { createAssessmentWorkspaceConfig } from "@game-guild/emception-ui/assessment/presets";
import type { WorkspaceConfig } from "emception";
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
import { StandardTestEditor } from "./standard-test-editor";
import { FunctionalTestEditor } from "./functional-test-editor";
import { useLearningBase } from '@/lib/learning/use-learning-base';

// ponytail: direct import (matches code-grader-panel pattern). The IDE manages
// its own worker boot client-side; Next's transpilePackages list already
// includes @game-guild/emception-ui so this resolves at build time.

const LANGUAGE_OPTIONS: { value: CodingLanguage; label: string }[] = [
  { value: "cpp", label: "C++ (clang + WASI)" },
  { value: "c", label: "C (clang + WASI)" },
  { value: "sdl-cpp", label: "C++ + SDL3 (emcc)" },
  { value: "raylib-cpp", label: "C++ + raylib (emcc)" },
  { value: "allegro-cpp", label: "C++ + Allegro 5 (clang + wasm-ld)" },
];

const DEFAULT_TOOLS: Record<CodingLanguage, string> = {
  cpp: "clang",
  c: "clang",
  "sdl-cpp": "emcc",
  "raylib-cpp": "emcc",
  "allegro-cpp": "clang",
};

const FUNCTION_NAME_RE = /^[A-Za-z_][A-Za-z0-9_]*$/;

const AUTOSAVE_DELAY_MS = 30_000;

/** In-memory file row mirrored from the definition and neutral IDE controller. */
interface AssignmentFileRow {
  path: string;
  content: string;
  encoding: FileEncoding;
  visibility: FileVisibility;
  modifiable: boolean;
}

/** The subset of the neutral IDE controller required by GameGuild authoring. */
interface AssessmentIdeController {
  getFiles(): Promise<readonly AssessmentWorkspaceFile[]>;
  replaceFiles(files: readonly AssessmentWorkspaceFile[]): Promise<void>;
}

interface AssessmentWorkspaceFile {
  path: string;
  type: "text" | "image";
  content: string;
}

interface AssessmentIdeExtension {
  id: string;
  toolbarEnd?: () => React.ReactNode;
  explorerFooter?: () => React.ReactNode;
  bottomPanel?: () => React.ReactNode;
}

/**
 * Reconcile source files from the neutral IDE controller while retaining the
 * assessment policy owned by GameGuild. New text files are deliberately public
 * and modifiable until the author changes their policy in the host extension.
 */
function reconcileAssignmentFiles(
  rows: readonly AssignmentFileRow[],
  liveFiles: readonly AssessmentWorkspaceFile[],
): AssignmentFileRow[] {
  const liveTextByPath = new Map(
    liveFiles
      .filter((file) => file.type === "text")
      .map((file) => [file.path, file]),
  );
  const knownPaths = new Set(rows.map((row) => row.path));
  const refreshedRows = rows
    .filter((row) => row.encoding !== "text" || liveTextByPath.has(row.path))
    .map((row) => ({
      ...row,
      content: liveTextByPath.get(row.path)?.content ?? row.content,
    }));
  const additions = [...liveTextByPath.values()]
    .filter((file) => !knownPaths.has(file.path))
    .map<AssignmentFileRow>((file) => ({
      path: file.path,
      content: file.content,
      encoding: "text",
      visibility: "Public",
      modifiable: true,
    }));

  return [...refreshedRows, ...additions];
}

interface EditorProps {
  courseId: string;
  assessmentId: string;
  assessmentSlug: string;
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
  assessmentSlug,
  programId,
  contentId,
  assessmentTitle,
  initialContent,
}: EditorProps): ReactElement {
  const learningBase = useLearningBase();
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const ideControllerRef = useRef<AssessmentIdeController | null>(null);

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
  const workspaceConfig = useMemo(() => {
    const sample = ASSIGNMENT_SAMPLES[language as CodingLanguage];
    if (!sample) return null;
    const files: WorkspaceConfig["files"] = {};
    for (const row of fileRows) {
      files[row.path] = { encoding: row.encoding, content: row.content };
    }
    // ponytail: if no files yet, fall back to the preset so the IDE has something to boot with.
    return createAssessmentWorkspaceConfig(
      language,
      Object.keys(files).length > 0 ? files : sample.workspaceConfig.files,
    );
  }, [language, fileRows]);


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
    const controller = ideControllerRef.current;
    if (controller) {
      void controller
        .replaceFiles(
          rows.map(({ path, content }) => ({ path, content, type: "text" as const })),
        )
        .catch((replaceError) => {
          setError(
            replaceError instanceof Error
              ? `Could not switch editor workspace: ${replaceError.message}`
              : "Could not switch editor workspace.",
          );
        });
    }
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

  async function synchronizeFilePolicies(controller: AssessmentIdeController) {
    try {
      const liveFiles = await controller.getFiles();
      setFileRows((previous) => reconcileAssignmentFiles(previous, liveFiles));
      setError(null);
    } catch (readError) {
      setError(
        readError instanceof Error
          ? `Could not read editor files: ${readError.message}`
          : "Could not read editor files.",
      );
    }
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

    // Pull source edits from the neutral controller. Assessment policy stays in
    // this component, so the IDE never owns visibility, tests, or language.
    let liveFileRows = fileRows;
    if (ideControllerRef.current) {
      try {
        const liveFiles = await ideControllerRef.current.getFiles();
        liveFileRows = reconcileAssignmentFiles(fileRows, liveFiles);
      } catch (readError) {
        setError(
          readError instanceof Error
            ? `Could not read editor files: ${readError.message}`
            : "Could not read editor files.",
        );
        savingRef.current = false;
        return;
      }
    }

    const content = buildContent({
      language,
      allowStudentCreateFiles,
      fileRows: liveFileRows,
      testRows,
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
      `${learningBase}/courses/${encodeURIComponent(courseId)}/assessments/${assessmentSlug}`,
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
              <CodingAssessmentEditor
                mode="author"
                definition={buildContent({
                  language,
                  allowStudentCreateFiles,
                  fileRows,
                  testRows,
                  maxScore,
                })}
                workspaceConfig={workspaceConfig}
                maxScore={maxScore}
                onReady={(controller) => {
                  ideControllerRef.current = controller;
                }}
                extensions={[
                  {
                    id: "gameguild-assessment-authoring",
                    toolbarEnd: () => (
                      <div className="flex items-center gap-2">
                        <select
                          aria-label="Workspace preset"
                          data-testid="preset-picker"
                          value={language}
                          onChange={(event) =>
                            handleLanguageChange(event.target.value as CodingLanguage)
                          }
                        >
                          {LANGUAGE_OPTIONS.map((option) => (
                            <option key={option.value} value={option.value}>
                              {option.label}
                            </option>
                          ))}
                        </select>
                        <button
                          type="button"
                          data-testid="allow-student-create"
                          onClick={() =>
                            setAllowStudentCreateFiles((value) => !value)
                          }
                        >
                          {allowStudentCreateFiles ? "🔓" : "🔒"}
                        </button>
                      </div>
                    ),
                    explorerFooter: () => (
                      <section
                        aria-label="Assignment file policies"
                        className="space-y-2 border-t border-border p-2 text-xs"
                      >
                        <div className="flex items-center justify-between gap-2">
                          <strong>File policies</strong>
                          <button
                            type="button"
                            data-testid="sync-file-policies"
                            onClick={() => {
                              const controller = ideControllerRef.current;
                              if (!controller) {
                                setError("Editor is still loading. Try again shortly.");
                                return;
                              }
                              void synchronizeFilePolicies(controller);
                            }}
                          >
                            Sync files
                          </button>
                        </div>
                        {fileRows.map((row) => (
                          <div key={row.path} className="space-y-1">
                            <p className="truncate font-mono" title={row.path}>
                              {row.path}
                            </p>
                            <div className="flex items-center gap-2">
                              <select
                                aria-label={`Visibility for ${row.path}`}
                                value={row.visibility}
                                onChange={(event) =>
                                  setFileRows((previous) =>
                                    previous.map((file) =>
                                      file.path === row.path
                                        ? {
                                            ...file,
                                            visibility: event.target.value as FileVisibility,
                                          }
                                        : file,
                                    ),
                                  )
                                }
                              >
                                <option value="Public">Public</option>
                                <option value="Private">Private</option>
                              </select>
                              <label className="flex items-center gap-1">
                                <input
                                  aria-label={`Student can edit ${row.path}`}
                                  type="checkbox"
                                  checked={row.modifiable}
                                  onChange={(event) =>
                                    setFileRows((previous) =>
                                      previous.map((file) =>
                                        file.path === row.path
                                          ? { ...file, modifiable: event.target.checked }
                                          : file,
                                      ),
                                    )
                                  }
                                />
                                Editable
                              </label>
                            </div>
                          </div>
                        ))}
                      </section>
                    ),
                    bottomPanel: () => (
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
                    ),
                  },
                ] satisfies readonly AssessmentIdeExtension[]}
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
    LibBundle: args.language === "allegro-cpp" ? "allegro" : null,
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
