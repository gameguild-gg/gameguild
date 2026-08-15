"use client";

import React, { type ReactElement } from "react";
import { Trash2 } from "lucide-react";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import { Textarea } from "@game-guild/ui/components/textarea";
import type { StandardTest, FileVisibility } from "@/lib/coding-assignment/client";
import { VisibilitySelect } from "./visibility-select";

export interface StandardTestEditorProps {
  index: number;
  test: StandardTest;
  visibility: FileVisibility;
  errors: Array<{ field: string; code: string; message: string }>;
  onChange: (idx: number, patch: Partial<StandardTest>) => void;
  onVisibilityChange: (idx: number, next: FileVisibility) => void;
  onRemove: (idx: number) => void;
}

/**
 * Form for a v1 `StandardTest` (inline stdin/stdout/stderr/exitCode).
 * Wire shape: PascalCase fields, lowercase `kind: "standard"` discriminator.
 */
export function StandardTestEditor({
  index,
  test,
  visibility,
  errors,
  onChange,
  onVisibilityChange,
  onRemove,
}: StandardTestEditorProps): ReactElement {
  const errFor = (suffix: string) => errors.find((e) => e.field.endsWith(suffix));

  return (
    <div
      className="border rounded-md p-4 space-y-3"
      data-testid={`standard-test-${index}`}
    >
      <div className="flex items-center gap-2">
        <Badge variant="outline">standard</Badge>
        <Input
          placeholder="Test name (optional)"
          value={test.Name ?? ""}
          onChange={(e) => onChange(index, { Name: e.target.value })}
          className="flex-1"
          data-testid={`standard-name-${index}`}
        />
        <div className="w-32">
          <VisibilitySelect
            value={visibility}
            onChange={(v) => onVisibilityChange(index, v)}
            testId={`standard-visibility-${index}`}
          />
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onRemove(index)}
          data-testid={`standard-remove-${index}`}
        >
          <Trash2 className="h-3 w-3" />
        </Button>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <Label className="text-xs">Weight</Label>
          <Input
            type="number"
            min={0}
            step="any"
            value={test.Weight ?? 1}
            onChange={(e) => onChange(index, { Weight: Number(e.target.value) })}
            data-testid={`standard-weight-${index}`}
          />
          {errFor(".Weight") && (
            <p className="text-destructive text-xs">
              {errFor(".Weight")!.message}
            </p>
          )}
        </div>
        <div className="space-y-1">
          <Label className="text-xs">Exit code (optional)</Label>
          <Input
            type="number"
            value={test.ExitCode ?? ""}
            onChange={(e) =>
              onChange(index, {
                ExitCode:
                  e.target.value === "" ? null : Number(e.target.value),
              })
            }
            data-testid={`standard-exitCode-${index}`}
          />
        </div>
      </div>

      <div className="space-y-2">
        <div>
          <Label className="text-xs">stdin (optional)</Label>
          <Textarea
            rows={2}
            value={test.Stdin ?? ""}
            onChange={(e) => onChange(index, { Stdin: e.target.value })}
            data-testid={`standard-stdin-${index}`}
          />
        </div>
        <div>
          <Label className="text-xs">Expected stdout</Label>
          <Textarea
            rows={2}
            value={test.Stdout}
            onChange={(e) => onChange(index, { Stdout: e.target.value })}
            data-testid={`standard-stdout-${index}`}
          />
          {errFor(".Stdout") && (
            <p className="text-destructive text-xs">
              {errFor(".Stdout")!.message}
            </p>
          )}
        </div>
        <div>
          <Label className="text-xs">Expected stderr (optional)</Label>
          <Textarea
            rows={2}
            value={test.Stderr ?? ""}
            onChange={(e) => onChange(index, { Stderr: e.target.value })}
            data-testid={`standard-stderr-${index}`}
          />
        </div>
      </div>
    </div>
  );
}
