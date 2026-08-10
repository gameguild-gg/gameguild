"use client";

import React, {
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
  type GradingCase,
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import {
  putCodingDefinition,
  type CodingLanguageId,
} from "@/lib/emception/put-coding-definition";
import type { CodingDefinition } from "@/lib/learning/queries/assessments";
import {
  buildPreview,
  makeEmptyCase,
  validateAll,
  type CaseKind,
} from "./validation";
import { CaseEditor } from "./case-editor";

// ponytail: direct import (matches grade-client pattern). The IDE manages
// its own worker boot client-side; Next's transpilePackages list already
// includes @game-guild/emception-ui (Task 8) so this resolves at build time.

const LANGUAGE_OPTIONS: { value: CodingLanguageId; label: string }[] = [
  { value: "cpp", label: "C++ (clang + WASI)" },
  { value: "c", label: "C (clang + WASI)" },
  { value: "sdl-cpp", label: "C++ + SDL3 (emcc)" },
  { value: "raylib-cpp", label: "C++ + raylib (emcc)" },
];

const KIND_OPTIONS: { value: CaseKind; label: string }[] = [
  { value: "stdio", label: "Stdin → Stdout" },
  { value: "stdio-file", label: "File → Output File" },
  { value: "clang-query", label: "clang-query matcher" },
  { value: "doctest", label: "doctest source files" },
  { value: "custom", label: "Custom (instructor-graded)" },
];

interface EditorProps {
  courseId: string;
  assessmentId: string;
  assessmentTitle: string;
  initialDefinition: CodingDefinition | null;
}

export function CodingDefinitionEditor({
  courseId,
  assessmentId,
  assessmentTitle,
  initialDefinition,
}: EditorProps): ReactElement {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const ideRef = useRef<IdeHandle | null>(null);

  const initialLang = (initialDefinition?.language as CodingLanguageId) ?? "cpp";
  const [language, setLanguage] = useState<CodingLanguageId>(initialLang);
  const [workspaceConfig, setWorkspaceConfig] = useState<WorkspaceConfig | null>(
    (initialDefinition?.workspaceConfig as WorkspaceConfig | null) ?? null,
  );
  const [cases, setCases] = useState<GradingCase[]>(
    (initialDefinition?.testPlan?.cases as GradingCase[] | undefined) ?? [],
  );
  const [maxScore, setMaxScore] = useState<number>(
    initialDefinition?.maxScore ?? 100,
  );
  const [passingScore, setPassingScore] = useState<number>(
    initialDefinition?.passingScore ?? 60,
  );
  const [build, setBuild] = useState<Record<string, unknown> | null>(
    (initialDefinition?.testPlan?.build as Record<string, unknown> | null) ??
      null,
  );

  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  const validationErrors = useMemo(
    () => validateAll({ maxScore, passingScore, cases }),
    [maxScore, passingScore, cases],
  );
  const isValid = validationErrors.length === 0;

  const preview = useMemo(
    () =>
      buildPreview({
        language,
        workspaceConfig,
        cases,
        build,
        maxScore,
        passingScore,
      }),
    [language, workspaceConfig, cases, build, maxScore, passingScore],
  );

  function handleLanguageChange(next: CodingLanguageId) {
    setLanguage(next);
    const sample = ASSIGNMENT_SAMPLES[next as CodingLanguage];
    if (sample) {
      setWorkspaceConfig(sample.workspaceConfig);
      setCases(sample.plan.cases.map((c) => ({ ...c })));
      setBuild((sample.plan.build as Record<string, unknown>) ?? null);
    }
  }

  function handleAddCase(kind: CaseKind) {
    setCases((prev) => [...prev, makeEmptyCase(kind)]);
  }

  function handleRemoveCase(idx: number) {
    setCases((prev) => prev.filter((_, i) => i !== idx));
  }

  function handleCaseChange(idx: number, patch: Partial<GradingCase>) {
    setCases((prev) =>
      prev.map((c, i) => (i === idx ? { ...c, ...patch } : c)),
    );
  }

  async function handleSave() {
    if (!isValid) {
      setError("Resolve validation errors before saving.");
      return;
    }
    setError(null);
    setSaved(false);

    let workspaceOut = workspaceConfig;
    if (ideRef.current && workspaceConfig) {
      try {
        const files = await ideRef.current.getFiles();
        const filesMap: WorkspaceConfig["files"] = {};
        for (const f of files) {
          filesMap[f.path] = { encoding: "text", content: f.content };
        }
        workspaceOut = { ...workspaceConfig, files: filesMap };
        setWorkspaceConfig(workspaceOut);
      } catch {
        // ponytail: IDE not yet booted — keep the authored workspaceConfig.
      }
    }

    const payload = buildPreview({
      language,
      workspaceConfig: workspaceOut,
      cases,
      build,
      maxScore,
      passingScore,
    });

    startTransition(async () => {
      const result = await putCodingDefinition(assessmentId, payload, courseId);
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
                  onValueChange={(v) =>
                    handleLanguageChange(v as CodingLanguageId)
                  }
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
                  the test cases from the assignment template.
                </p>
              </div>
            </CardContent>
          </Card>

          {workspaceConfig && (
            <Card>
              <CardHeader>
                <CardTitle>Starter files</CardTitle>
              </CardHeader>
              <CardContent>
                <div data-testid="ide-mount">
                  <Ide ref={ideRef} workspaceConfig={workspaceConfig} />
                </div>
              </CardContent>
            </Card>
          )}

          <Card>
            <CardHeader>
              <CardTitle>Test cases</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {cases.length === 0 && (
                <p
                  className="text-muted-foreground text-sm"
                  data-testid="empty-cases"
                >
                  No test cases yet. Add one below.
                </p>
              )}
              {cases.map((c, i) => (
                <CaseEditor
                  key={i}
                  index={i}
                  caseData={c}
                  onChange={handleCaseChange}
                  onRemove={handleRemoveCase}
                  errors={validationErrors.filter((e) =>
                    e.field.startsWith(`cases[${i}]`),
                  )}
                />
              ))}
              <div className="flex flex-wrap gap-2">
                {KIND_OPTIONS.map((k) => (
                  <Button
                    key={k.value}
                    variant="outline"
                    size="sm"
                    onClick={() => handleAddCase(k.value)}
                    data-testid={`add-case-${k.value}`}
                  >
                    <Plus className="mr-1 h-3 w-3" /> {k.label}
                  </Button>
                ))}
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
                {validationErrors.find((e) => e.field === "maxScore") && (
                  <p className="text-destructive text-xs">
                    {
                      validationErrors.find((e) => e.field === "maxScore")!
                        .message
                    }
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
                {validationErrors.find((e) => e.field === "passingScore") && (
                  <p className="text-destructive text-xs">
                    {
                      validationErrors.find((e) => e.field === "passingScore")!
                        .message
                    }
                  </p>
                )}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>v2 Definition preview</CardTitle>
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
              {validationErrors.length > 0 && (
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
