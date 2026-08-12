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
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import { Switch } from "@game-guild/ui/components/switch";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import {
  putCodingAssignmentAction,
  type CodingAssignmentContent,
  type CodingEnvironment,
  type FileVisibility,
  type FunctionParameter,
  type FunctionParameterWithName,
  type FunctionParameterType,
  type FunctionParameterValue,
  type StandardTest,
  type FunctionalTest,
  type Test,
} from "@/lib/coding-assignment/actions";
import { AssignmentFilesTree, type AssignmentFileRow } from "./assignment-files-tree";
import { StandardTestEditor } from "./standard-test-editor";
import { FunctionalTestEditor } from "./functional-test-editor";

// ponytail: direct import (matches grade-client pattern). The IDE manages
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
  const [maxScore, setMaxScore] = useState<number>(initialContent?.Grading.MaxScore ?? 100);
  const [passingScore, setPassingScore] = useState<number>(
    initialContent?.Grading.PassingScore ?? 60,
  );

  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  // ── WorkspaceConfig for the IDE — derive from language preset + files ──
  const workspaceConfig = useMemo<WorkspaceConfig | null>(() => {
    const sample = ASSIGNMENT_SAMPLES[language as CodingLanguage];
    if (!sample) return null;
    const files: WorkspaceConfig["files"] = {};
    for (const row of fileRows) {
      files[row.path] = { encoding: "text", content: row.content };
    }
    // ponytail: if no files yet, fall back to the preset so the IDE has something to boot with.
    return {
      ...sample.workspaceConfig,
      files: Object.keys(files).length > 0 ? files : sample.workspaceConfig.files,
    };
  }, [language, fileRows]);

  // ── Apply per-file meta to the IDE whenever it changes ──
  useEffect(() => {
    const ref = ideRef.current;
    if (!ref) return;
    for (const row of fileRows) {
      void ref.setFileMeta(row.path, {
        visibility: row.visibility,
        modifiable: row.modifiable,
      });
    }
  }, [fileRows]);

  // ── Validation ──
  const validationErrors = useMemo(
    () => validateAll({ maxScore, passingScore, testRows, fileRows }),
    [maxScore, passingScore, testRows, fileRows],
  );
  const isValid = validationErrors.length === 0;

  // ── Preview ──
  const preview = useMemo<CodingAssignmentContent>(
    () =>
      buildContent({
        language,
        allowStudentCreateFiles,
        fileRows,
        testRows,
        maxScore,
        passingScore,
      }),
    [language, allowStudentCreateFiles, fileRows, testRows, maxScore, passingScore],
  );

  // ── Handlers ──
  function handleLanguageChange(next: CodingLanguage) {
    setLanguage(next);
    const sample = ASSIGNMENT_SAMPLES[next as CodingLanguage];
    if (sample) {
      const rows: AssignmentFileRow[] = Object.entries(
        sample.workspaceConfig.files,
      ).map(([path, bundle]) => ({
        path,
        content: bundle.content,
        visibility: "Public" as FileVisibility,
        modifiable: true,
      }));
      setFileRows(rows);
      // ponytail: seed a single visible stdio case so the editor isn't empty.
      setTestRows([
        {
          test: {
            kind: "standard",
            Name: "echo stdin",
            Stdin: "hello",
            Stdout: "hello",
            Weight: 1,
          },
          visibility: "Public",
        },
      ]);
    }
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
            ReturnType: { Type: "integer", Content: 0 },
          },
          Result: { Type: "integer", Content: 0 },
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

  function handleFileAdd(path: string, content: string) {
    setFileRows((prev) => [
      ...prev,
      { path, content, visibility: "Public", modifiable: true },
    ]);
    void ideRef.current?.addFile(path, content);
  }

  function handleFileChange(path: string, patch: Partial<AssignmentFileRow>) {
    setFileRows((prev) =>
      prev.map((row) => (row.path === path ? { ...row, ...patch } : row)),
    );
    const current = fileRows.find((r) => r.path === path);
    if (current && (patch.visibility || patch.modifiable !== undefined)) {
      void ideRef.current?.setFileMeta(path, {
        visibility: patch.visibility ?? current.visibility,
        modifiable: patch.modifiable ?? current.modifiable,
      });
    }
  }

  async function handleFileRemove(path: string) {
    setFileRows((prev) => prev.filter((row) => row.path !== path));
    await ideRef.current?.removeFile(path);
  }

  async function handleSave() {
    if (!isValid || !contentId) {
      setError(contentId ? "Resolve validation errors before saving." : "No content item linked to this assessment.");
      return;
    }
    setError(null);
    setSaved(false);

    // Refresh file contents from the IDE before assembling the payload.
    let liveFileRows = fileRows;
    if (ideRef.current) {
      try {
        const liveFiles = await ideRef.current.getFiles();
        const liveMap = new Map(liveFiles.map((f) => [f.path, f.content]));
        liveFileRows = fileRows.map((row) => ({
          ...row,
          content: liveMap.get(row.path) ?? row.content,
        }));
      } catch {
        // ponytail: IDE not yet booted — keep authored content.
      }
    }

    const content = buildContent({
      language,
      allowStudentCreateFiles,
      fileRows: liveFileRows,
      testRows,
      maxScore,
      passingScore,
    });

    startTransition(async () => {
      const result = await putCodingAssignmentAction(programId, contentId, content);
      if (!result.success) {
        setError(result.error);
        return;
      }
      setSaved(true);
      router.push(
        `/dashboard/learning/courses/${encodeURIComponent(courseId)}/assessments/${assessmentId}`,
      );
    });
  }

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
        <Badge variant="secondary">{language}</Badge>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          <Card>
            <CardHeader>
              <CardTitle>Language preset</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-2">
                <Label htmlFor="language">Language / preset</Label>
                <Select
                  value={language}
                  onValueChange={(v) => handleLanguageChange(v as CodingLanguage)}
                >
                  <SelectTrigger id="language" data-testid="language-select">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {LANGUAGE_OPTIONS.map((o) => (
                      <SelectItem key={o.value} value={o.value}>
                        {o.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-muted-foreground text-xs">
                  Selecting a preset loads starter files into the IDE and seeds
                  a default StandardTest case from the assignment template.
                </p>
              </div>
            </CardContent>
          </Card>

          {workspaceConfig && (
            <Card>
              <CardHeader>
                <CardTitle>Starter files</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <AssignmentFilesTree
                  files={fileRows}
                  onAdd={handleFileAdd}
                  onChange={handleFileChange}
                  onRemove={handleFileRemove}
                />
                <div data-testid="ide-mount">
                  <Ide ref={ideRef} workspaceConfig={workspaceConfig} />
                </div>
                <div className="flex items-center gap-2">
                  <Switch
                    id="allow-student-create"
                    checked={allowStudentCreateFiles}
                    onCheckedChange={setAllowStudentCreateFiles}
                    data-testid="allow-student-create"
                  />
                  <Label htmlFor="allow-student-create" className="text-sm">
                    Allow students to create new files
                  </Label>
                </div>
              </CardContent>
            </Card>
          )}

          <Card>
            <CardHeader>
              <CardTitle>Tests</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
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
                    test={row.test as FunctionalTest}
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
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Scoring</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="max-score">Max score</Label>
                <Input
                  id="max-score"
                  type="number"
                  min={1}
                  value={maxScore}
                  onChange={(e) => setMaxScore(Number(e.target.value))}
                  data-testid="max-score"
                />
                {validationErrors.find((e) => e.field === "Grading.MaxScore") && (
                  <p className="text-destructive text-xs">
                    {validationErrors.find((e) => e.field === "Grading.MaxScore")!.message}
                  </p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="passing-score">Passing score</Label>
                <Input
                  id="passing-score"
                  type="number"
                  min={0}
                  value={passingScore}
                  onChange={(e) => setPassingScore(Number(e.target.value))}
                  data-testid="passing-score"
                />
                {validationErrors.find((e) => e.field === "Grading.PassingScore") && (
                  <p className="text-destructive text-xs">
                    {validationErrors.find((e) => e.field === "Grading.PassingScore")!.message}
                  </p>
                )}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>v1 CodingAssignment preview</CardTitle>
            </CardHeader>
            <CardContent>
              <pre
                data-testid="json-preview"
                className="text-xs bg-muted rounded p-3 overflow-auto max-h-96"
              >
                {JSON.stringify(preview, null, 2)}
              </pre>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Save</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {error && <p className="text-destructive text-sm">{error}</p>}
              {saved && (
                <p className="text-sm text-green-600">Saved successfully.</p>
              )}
              {stdioErrorCount > 0 && (
                <ul className="text-destructive text-xs list-disc pl-4 space-y-1">
                  {validationErrors.slice(0, 5).map((e, i) => (
                    <li key={i}>{e.message}</li>
                  ))}
                </ul>
              )}
              <Button
                className="w-full"
                onClick={handleSave}
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
              <Button
                variant="outline"
                className="w-full"
                onClick={handleBack}
              >
                Cancel
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

// ── Validation helpers ────────────────────────────────────────────────────

interface ValidationState {
  maxScore: number;
  passingScore: number;
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

  if (!(state.maxScore > 0)) {
    errors.push({
      field: "Grading.MaxScore",
      code: "max_score_positive",
      message: "Max score must be greater than 0.",
    });
  }
  if (state.passingScore < 0) {
    errors.push({
      field: "Grading.PassingScore",
      code: "passing_score_non_negative",
      message: "Passing score must be ≥ 0.",
    });
  }
  if (state.passingScore > state.maxScore) {
    errors.push({
      field: "Grading.PassingScore",
      code: "passing_score_within_max",
      message: "Passing score cannot exceed max score.",
    });
  }

  if (state.testRows.length === 0) {
    errors.push({
      field: "Tests",
      code: "at_least_one_test",
      message: "At least one test is required.",
    });
  }

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
      const fn = t as FunctionalTest;
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
      if (!isV1ParamType(fn.Result.Type)) {
        errors.push({
          field: `${path}.Result`,
          code: "functional_param_type_not_supported_v1",
          message: `Result type "${fn.Result.Type}" is not supported in v1.`,
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
  passingScore: number;
}

function buildContent(args: BuildArgs): CodingAssignmentContent {
  const files: Record<string, { Content: string; Encoding: "text"; Visibility: FileVisibility; Modifiable: boolean }> = {};
  for (const row of args.fileRows) {
    files[row.path] = {
      Content: row.content,
      Encoding: "text",
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
    Grading: { MaxScore: args.maxScore, PassingScore: args.passingScore },
  };
}

// Helpers re-exported for tests.
export const __testHelpers = {
  parseContent(type: FunctionParameterType, raw: string): FunctionParameterValue {
    switch (type) {
      case "integer":
      case "float":
        return Number.isNaN(Number(raw)) ? 0 : Number(raw);
      case "boolean":
        return raw === "true" || raw === "1";
      case "string":
        return raw;
    }
  },
  makeParameter(name: string, type: FunctionParameterType, content: FunctionParameterValue): FunctionParameterWithName {
    return { Name: name, Type: type, Content: content };
  },
  makeParameterType(t: string): FunctionParameterType {
    if (!isV1ParamType(t)) throw new Error(`unsupported type: ${t}`);
    return t;
  },
  makeFunctionParameter(type: FunctionParameterType, content: FunctionParameterValue): FunctionParameter {
    return { Type: type, Content: content };
  },
};
