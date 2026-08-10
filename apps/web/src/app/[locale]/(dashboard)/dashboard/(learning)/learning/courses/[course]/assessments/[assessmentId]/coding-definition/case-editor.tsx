"use client";

import React, { type ReactElement } from "react";
import { Trash2 } from "lucide-react";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import { Textarea } from "@game-guild/ui/components/textarea";
import { Switch } from "@game-guild/ui/components/switch";
import type { GradingCase } from "@game-guild/emception-ui";
import type { ValidationError } from "./validation";

interface CaseEditorProps {
  index: number;
  caseData: GradingCase;
  onChange: (idx: number, patch: Partial<GradingCase>) => void;
  onRemove: (idx: number) => void;
  errors: ValidationError[];
}

export function CaseEditor({
  index,
  caseData,
  onChange,
  onRemove,
  errors,
}: CaseEditorProps): ReactElement {
  const errFor = (suffix: string) =>
    errors.find((e) => e.field.endsWith(suffix));

  return (
    <div
      className="border rounded-md p-4 space-y-3"
      data-testid={`case-${index}`}
    >
      <div className="flex items-center gap-2">
        <Badge variant="outline">{caseData.kind}</Badge>
        <Input
          placeholder="Case name (optional)"
          value={caseData.name ?? ""}
          onChange={(e) => onChange(index, { name: e.target.value })}
          className="flex-1"
          data-testid={`case-name-${index}`}
        />
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onRemove(index)}
          data-testid={`remove-case-${index}`}
        >
          <Trash2 className="h-3 w-3" />
        </Button>
      </div>

      <div className="grid grid-cols-3 gap-2">
        <div className="space-y-1">
          <Label className="text-xs">Weight</Label>
          <Input
            type="number"
            min={0}
            step="any"
            value={caseData.weight ?? 0}
            onChange={(e) => onChange(index, { weight: Number(e.target.value) })}
            data-testid={`case-weight-${index}`}
          />
          {errFor(".weight") && (
            <p className="text-destructive text-xs">
              {errFor(".weight")!.message}
            </p>
          )}
        </div>
        <div className="space-y-1 flex items-end">
          <label className="flex items-center gap-2 text-sm">
            <Switch
              checked={!!caseData.hidden}
              onCheckedChange={(v) => onChange(index, { hidden: v })}
              data-testid={`case-hidden-${index}`}
            />
            Hidden
          </label>
        </div>
      </div>

      {caseData.kind === "stdio" && (
        <div className="space-y-2">
          <div>
            <Label className="text-xs">stdin</Label>
            <Textarea
              rows={2}
              value={caseData.stdin ?? ""}
              onChange={(e) => onChange(index, { stdin: e.target.value })}
              data-testid={`case-stdin-${index}`}
            />
          </div>
          <div>
            <Label className="text-xs">Expected stdout</Label>
            <Textarea
              rows={2}
              value={
                typeof caseData.expectedStdout === "string"
                  ? caseData.expectedStdout
                  : ""
              }
              onChange={(e) =>
                onChange(index, { expectedStdout: e.target.value })
              }
              data-testid={`case-expectedStdout-${index}`}
            />
            {errFor(".expectedStdout") && (
              <p className="text-destructive text-xs">
                {errFor(".expectedStdout")!.message}
              </p>
            )}
          </div>
        </div>
      )}

      {caseData.kind === "stdio-file" && (
        <div className="grid grid-cols-2 gap-2">
          <div>
            <Label className="text-xs">Input file path</Label>
            <Input
              value={caseData.inFile ?? ""}
              onChange={(e) => onChange(index, { inFile: e.target.value })}
              data-testid={`case-inFile-${index}`}
            />
          </div>
          <div>
            <Label className="text-xs">Expected output file path</Label>
            <Input
              value={caseData.expectedOutFile ?? ""}
              onChange={(e) =>
                onChange(index, { expectedOutFile: e.target.value })
              }
              data-testid={`case-expectedOutFile-${index}`}
            />
          </div>
          {errFor("") && (
            <p className="text-destructive text-xs col-span-2">
              {errFor("")!.message}
            </p>
          )}
        </div>
      )}

      {caseData.kind === "clang-query" && (
        <div className="grid grid-cols-2 gap-2">
          <div>
            <Label className="text-xs">Matcher</Label>
            <Input
              value={caseData.matcher ?? ""}
              onChange={(e) => onChange(index, { matcher: e.target.value })}
              data-testid={`case-matcher-${index}`}
            />
          </div>
          <div>
            <Label className="text-xs">Expect</Label>
            <Input
              value={
                typeof caseData.expect === "string"
                  ? caseData.expect
                  : JSON.stringify(caseData.expect ?? "")
              }
              onChange={(e) =>
                onChange(index, {
                  expect: e.target.value as "found" | "not-found",
                })
              }
              data-testid={`case-expect-${index}`}
            />
          </div>
          {errFor("") && (
            <p className="text-destructive text-xs col-span-2">
              {errFor("")!.message}
            </p>
          )}
        </div>
      )}

      {caseData.kind === "doctest" && (
        <div>
          <Label className="text-xs">Source files (comma-separated paths)</Label>
          <Input
            value={(caseData.sourceFiles ?? []).join(", ")}
            onChange={(e) =>
              onChange(index, {
                sourceFiles: e.target.value
                  .split(",")
                  .map((s) => s.trim())
                  .filter(Boolean),
              })
            }
            data-testid={`case-sourceFiles-${index}`}
          />
          {errFor(".sourceFiles") && (
            <p className="text-destructive text-xs">
              {errFor(".sourceFiles")!.message}
            </p>
          )}
        </div>
      )}

      {caseData.kind === "custom" && (
        <p className="text-muted-foreground text-xs">
          Custom cases are instructor-graded; no automated matcher. Add the
          grader rubric in the assessment description.
        </p>
      )}
    </div>
  );
}
