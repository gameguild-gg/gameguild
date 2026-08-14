"use client";

import { useCallback, useEffect, useRef } from "react";
import type { Monaco, OnMount } from "@monaco-editor/react";
import type { editor, IDisposable, languages, IPosition } from "monaco-editor";
import vegaLiteSchema from "vega-lite/vega-lite-schema.json";
import { MonacoCodeEditor } from "../../../shared/ui/monaco-code-editor";
import type { MonacoSurfacePreferences } from "../../../shared/ui/editor-preferences";
import {
  VegaLiteValidator,
  type VegaLiteValidationResult,
} from "./vega-lite-validator";

export function MonacoVegaLiteEditor({
  value,
  onChange,
  onValidationChange,
  height = "100%",
  theme = "light",
  options,
}: {
  value: string;
  onChange: (value: string | undefined) => void;
  onValidationChange?: (result: VegaLiteValidationResult) => void;
  height?: string | number;
  theme?: "light" | "dark";
  options?: MonacoSurfacePreferences;
}) {
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null);
  const completionRef = useRef<IDisposable | null>(null);
  const validationTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const validate = useCallback(
    (spec: string) => {
      if (validationTimer.current) clearTimeout(validationTimer.current);
      validationTimer.current = setTimeout(async () => {
        const result = await VegaLiteValidator.validateSpec(spec);
        onValidationChange?.(result);
        const model = editorRef.current?.getModel();
        if (!model) return;
        const monaco = await import("monaco-editor");
        monaco.editor.setModelMarkers(
          model,
          "vega-lite",
          result.isValid
            ? []
            : [
                {
                  startLineNumber: 1,
                  startColumn: 1,
                  endLineNumber: 1,
                  endColumn: model.getLineMaxColumn(1),
                  message: result.error ?? "Invalid Vega-Lite specification",
                  severity: monaco.MarkerSeverity.Error,
                },
              ],
        );
      }, 350);
    },
    [onValidationChange],
  );

  useEffect(() => {
    if (onValidationChange) validate(value);
    return () => {
      if (validationTimer.current) clearTimeout(validationTimer.current);
    };
  }, [onValidationChange, validate, value]);

  const beforeMount = useCallback(async (monaco: Monaco) => {
    const jsonLanguages = monaco.languages as typeof monaco.languages & {
      json?: {
        jsonDefaults?: {
          setDiagnosticsOptions: (options: unknown) => void;
        };
      };
    };
    jsonLanguages.json?.jsonDefaults?.setDiagnosticsOptions({
      validate: true,
      enableSchemaRequest: false,
      schemas: [
        {
          uri: "https://vega.github.io/schema/vega-lite/v6.json",
          fileMatch: ["inmemory://lexical-surface/vega-lite.json"],
          schema: vegaLiteSchema,
        },
      ],
    });

    completionRef.current?.dispose();
    completionRef.current = monaco.languages.registerCompletionItemProvider(
      "json",
      {
        provideCompletionItems(model: editor.ITextModel, position: IPosition) {
          const word = model.getWordUntilPosition(position);
          const range = {
            startLineNumber: position.lineNumber,
            endLineNumber: position.lineNumber,
            startColumn: word.startColumn,
            endColumn: word.endColumn,
          };
          const enumItem = (
            value: string,
            detail: string,
          ): languages.CompletionItem => ({
            label: value,
            kind: monaco.languages.CompletionItemKind.Enum,
            insertText: `"${value}"`,
            detail,
            range,
          });
          return {
            suggestions: [
              ...[
                "bar",
                "line",
                "circle",
                "point",
                "area",
                "rect",
                "arc",
                "text",
              ].map((value) => enumItem(value, "Vega-Lite mark")),
              ...[
                "quantitative",
                "temporal",
                "ordinal",
                "nominal",
                "geojson",
              ].map((value) => enumItem(value, "Vega-Lite field type")),
            ],
          };
        },
      },
    );
  }, []);

  useEffect(
    () => () => {
      completionRef.current?.dispose();
    },
    [],
  );

  const onMount: OnMount = (instance, monaco) => {
    editorRef.current = instance;
    instance.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, () => {
      void instance.getAction("editor.action.formatDocument")?.run();
    });
  };

  return (
    <MonacoCodeEditor
      path="inmemory://lexical-surface/vega-lite.json"
      language="json"
      height={height}
      value={value}
      onChange={(next) => {
        onChange(next);
        if (next && onValidationChange) validate(next);
      }}
      beforeMount={beforeMount}
      onMount={onMount}
      shikiTheme={options?.shikiTheme ?? "github"}
      isDark={theme === "dark"}
      options={{
        automaticLayout: true,
        bracketPairColorization: { enabled: true },
        folding: true,
        fontSize: options?.fontSize ?? 14,
        formatOnPaste: true,
        formatOnType: true,
        lineNumbers: (options?.lineNumbers ?? true) ? "on" : "off",
        minimap: { enabled: options?.minimap ?? false },
        quickSuggestions: { other: true, comments: false, strings: true },
        renderLineHighlight: options?.renderLineHighlight ?? "line",
        renderWhitespace: options?.renderWhitespace ?? "none",
        scrollBeyondLastLine: false,
        suggest: {
          showKeywords: true,
          showSnippets: true,
          showProperties: true,
        },
        tabSize: options?.tabSize ?? 2,
        wordWrap: (options?.wordWrap ?? true) ? "on" : "off",
      }}
    />
  );
}
