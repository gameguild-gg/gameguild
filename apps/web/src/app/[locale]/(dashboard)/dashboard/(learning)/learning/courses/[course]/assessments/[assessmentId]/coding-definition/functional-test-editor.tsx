"use client";

import React, { type ReactElement } from "react";
import { Plus, Trash2 } from "lucide-react";
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
import type {
  FunctionalTest,
  FunctionParameter,
  FunctionParameterWithName,
  FunctionParameterType,
  FunctionParameterValue,
  FileVisibility,
} from "@/lib/coding-assignment/client";
import { VisibilitySelect } from "./visibility-select";

/** v1 supports exactly these 4 primitive types — Array/Dictionary rejected. */
const PARAM_TYPES: { value: FunctionParameterType; label: string }[] = [
  { value: "string", label: "String" },
  { value: "boolean", label: "Boolean" },
  { value: "integer", label: "Integer" },
  { value: "float", label: "Float" },
];

export interface FunctionalTestEditorProps {
  index: number;
  test: FunctionalTest;
  visibility: FileVisibility;
  errors: Array<{ field: string; code: string; message: string }>;
  onChange: (idx: number, patch: Partial<FunctionalTest>) => void;
  onVisibilityChange: (idx: number, next: FileVisibility) => void;
  onRemove: (idx: number) => void;
}

/**
 * Form for a v1 `FunctionalTest` — function signature + expected return.
 *
 * Parameter type select is RESTRICTED to the v1 4 primitive types
 * (server-side validator rejects Array/Dictionary with
 * `functional_param_type_not_supported_v1`).
 */
export function FunctionalTestEditor({
  index,
  test,
  visibility,
  errors,
  onChange,
  onVisibilityChange,
  onRemove,
}: FunctionalTestEditorProps): ReactElement {
  const errFor = (suffix: string) => errors.find((e) => e.field.endsWith(suffix));
  const fn = test.Function;

  function patchFunction(patch: Partial<typeof fn>) {
    onChange(index, { Function: { ...fn, ...patch } });
  }

  function patchParameter(pIndex: number, patch: Partial<FunctionParameterWithName>) {
    const next = fn.Parameters.map((p, i) => (i === pIndex ? { ...p, ...patch } : p));
    patchFunction({ Parameters: next });
  }

  function addParameter() {
    const next: FunctionParameterWithName = {
      Name: "",
      Type: "string",
      Content: "",
    };
    patchFunction({ Parameters: [...fn.Parameters, next] });
  }

  function removeParameter(pIndex: number) {
    patchFunction({
      Parameters: fn.Parameters.filter((_, i) => i !== pIndex),
    });
  }

  return (
    <div
      className="border rounded-md p-4 space-y-3"
      data-testid={`functional-test-${index}`}
    >
      <div className="flex items-center gap-2">
        <Badge variant="outline">functional</Badge>
        <Input
          placeholder="Test name (optional)"
          value={test.Name ?? ""}
          onChange={(e) => onChange(index, { Name: e.target.value })}
          className="flex-1"
          data-testid={`functional-name-${index}`}
        />
        <div className="w-32">
          <VisibilitySelect
            value={visibility}
            onChange={(v) => onVisibilityChange(index, v)}
            testId={`functional-visibility-${index}`}
          />
        </div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onRemove(index)}
          data-testid={`functional-remove-${index}`}
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
            data-testid={`functional-weight-${index}`}
          />
          {errFor(".Weight") && (
            <p className="text-destructive text-xs">
              {errFor(".Weight")!.message}
            </p>
          )}
        </div>
        <div className="space-y-1">
          <Label className="text-xs">Function name</Label>
          <Input
            value={fn.FunctionName}
            onChange={(e) => patchFunction({ FunctionName: e.target.value })}
            placeholder="add"
            data-testid={`functional-functionName-${index}`}
          />
          {errFor(".FunctionName") && (
            <p className="text-destructive text-xs">
              {errFor(".FunctionName")!.message}
            </p>
          )}
        </div>
      </div>

      {/* Parameters list */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label className="text-xs">Parameters</Label>
          <Button
            variant="outline"
            size="sm"
            onClick={addParameter}
            data-testid={`functional-add-param-${index}`}
          >
            <Plus className="mr-1 h-3 w-3" /> Add parameter
          </Button>
        </div>
        {fn.Parameters.length === 0 && (
          <p className="text-muted-foreground text-xs" data-testid={`functional-no-params-${index}`}>
            No parameters. Click "Add parameter".
          </p>
        )}
        {fn.Parameters.map((p, pIndex) => (
          <div
            key={pIndex}
            className="grid grid-cols-[1fr_8rem_2fr_auto] gap-2 items-end"
            data-testid={`functional-param-${index}-${pIndex}`}
          >
            <div className="space-y-1">
              <Label className="text-xs">Name</Label>
              <Input
                value={p.Name}
                onChange={(e) => patchParameter(pIndex, { Name: e.target.value })}
                placeholder="a"
                data-testid={`functional-param-name-${index}-${pIndex}`}
              />
            </div>
            <div className="space-y-1">
              <Label className="text-xs">Type</Label>
              <Select
                value={p.Type}
                onValueChange={(v) =>
                  patchParameter(pIndex, {
                    Type: v as FunctionParameterType,
                    Content: defaultContentForType(v as FunctionParameterType, p.Content),
                  })
                }
              >
                <SelectTrigger data-testid={`functional-param-type-${index}-${pIndex}`}>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {PARAM_TYPES.map((t) => (
                    <SelectItem key={t.value} value={t.value}>
                      {t.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label className="text-xs">Value</Label>
              <Input
                value={String(p.Content)}
                onChange={(e) =>
                  patchParameter(pIndex, {
                    Content: parseContent(p.Type, e.target.value),
                  })
                }
                data-testid={`functional-param-value-${index}-${pIndex}`}
              />
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => removeParameter(pIndex)}
              data-testid={`functional-param-remove-${index}-${pIndex}`}
            >
              <Trash2 className="h-3 w-3" />
            </Button>
          </div>
        ))}
      </div>

      {/* Return type + result */}
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1">
          <Label className="text-xs">Return type</Label>
          <Select
            value={fn.ReturnType.Type}
            onValueChange={(v) => {
              const nextType = v as FunctionParameterType;
              patchFunction({
                ReturnType: {
                  Type: nextType,
                  Content: defaultContentForType(nextType, fn.ReturnType.Content),
                },
              });
            }}
          >
            <SelectTrigger data-testid={`functional-returnType-${index}`}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {PARAM_TYPES.map((t) => (
                <SelectItem key={t.value} value={t.value}>
                  {t.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1">
          <Label className="text-xs">Expected result</Label>
          <Input
            value={String(test.Result.Content)}
            onChange={(e) =>
              onChange(index, {
                Result: {
                  Type: test.Result.Type,
                  Content: parseContent(test.Result.Type, e.target.value),
                },
              })
            }
            data-testid={`functional-result-${index}`}
          />
        </div>
      </div>
    </div>
  );
}

/** Coerce a string input into the wire value for the given parameter type. */
function parseContent(type: FunctionParameterType, raw: string): FunctionParameterValue {
  switch (type) {
    case "integer":
      return Number.isNaN(Number(raw)) ? 0 : Number(raw);
    case "float":
      return Number.isNaN(Number(raw)) ? 0 : Number(raw);
    case "boolean":
      return raw === "true" || raw === "1";
    case "string":
      return raw;
  }
}

/** Reset content to a sane default when the type changes. */
function defaultContentForType(
  type: FunctionParameterType,
  current: FunctionParameterValue,
): FunctionParameterValue {
  switch (type) {
    case "integer":
    case "float":
      return typeof current === "number" ? current : 0;
    case "boolean":
      return typeof current === "boolean" ? current : false;
    case "string":
      return typeof current === "string" ? current : "";
  }
}

/**
 * Helper for callers: parse a FunctionParameter from raw wire (e.g. for tests).
 * Exported so the editor's tests can construct fixtures without repeating the cast.
 */
export function makeFunctionParameter(
  type: FunctionParameterType,
  content: FunctionParameterValue,
): FunctionParameter {
  return { Type: type, Content: content };
}
