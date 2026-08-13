"use client";

import { useCallback, useEffect, useRef } from "react";
import type { Monaco, OnMount } from "@monaco-editor/react";
import type { editor, IDisposable } from "monaco-editor";
import { MonacoCodeEditor } from "../../shared/ui/monaco-code-editor";
import type { MonacoSurfacePreferences } from "../../shared/ui/editor-preferences";
import { createMermaidCompletionProvider } from "./mermaid-completion-provider";
import {
  mermaidLanguageConfig,
  mermaidTheme,
  mermaidTokensProvider,
} from "./mermaid-language";
import {
  MermaidValidator,
  type MermaidValidationResult,
} from "./mermaid-validator";

export function MonacoMermaidEditor({
  value,
  onChange,
  onValidationChange,
  height = "100%",
  theme = "light",
  options,
}: {
  value: string;
  onChange: (value: string | undefined) => void;
  onValidationChange?: (result: MermaidValidationResult) => void;
  height?: string | number;
  theme?: "light" | "dark";
  options?: MonacoSurfacePreferences;
}) {
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null);
  const completionRef = useRef<IDisposable | null>(null);
  const validationTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const validate = useCallback(
    (code: string) => {
      if (validationTimer.current) clearTimeout(validationTimer.current);
      validationTimer.current = setTimeout(async () => {
        const quickResult = MermaidValidator.quickValidate(code);
        const result = quickResult.isValid
          ? await MermaidValidator.validateCode(code)
          : quickResult;
        onValidationChange?.(result);

        const model = editorRef.current?.getModel();
        if (!model) return;
        const monaco = await import("monaco-editor");
        monaco.editor.setModelMarkers(
          model,
          "mermaid",
          result.isValid
            ? []
            : [
                {
                  startLineNumber: result.line ?? 1,
                  startColumn: 1,
                  endLineNumber: result.line ?? 1,
                  endColumn: model.getLineMaxColumn(result.line ?? 1),
                  message: result.error ?? "Invalid Mermaid syntax",
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

  useEffect(
    () => () => {
      completionRef.current?.dispose();
    },
    [],
  );

  const beforeMount = useCallback((monaco: Monaco) => {
    monaco.languages.register({ id: "mermaid" });
    monaco.languages.setLanguageConfiguration("mermaid", mermaidLanguageConfig);
    monaco.languages.setMonarchTokensProvider("mermaid", mermaidTokensProvider);
    monaco.editor.defineTheme("mermaid-light", mermaidTheme);
    monaco.editor.defineTheme("mermaid-dark", {
      ...mermaidTheme,
      base: "vs-dark",
      colors: {
        ...mermaidTheme.colors,
        "editor.background": "#0d1117",
        "editor.foreground": "#f0f6fc",
      },
    });
    completionRef.current?.dispose();
    completionRef.current = monaco.languages.registerCompletionItemProvider(
      "mermaid",
      createMermaidCompletionProvider(monaco),
    );
  }, []);

  const onMount: OnMount = (instance) => {
    editorRef.current = instance;
  };

  return (
    <MonacoCodeEditor
      language="mermaid"
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
      fallbackLight="mermaid-light"
      fallbackDark="mermaid-dark"
      options={{
        automaticLayout: true,
        folding: true,
        fontFamily: "Monaco, Menlo, 'Ubuntu Mono', monospace",
        fontSize: options?.fontSize ?? 14,
        formatOnPaste: true,
        formatOnType: true,
        lineDecorationsWidth: 10,
        lineNumbers: (options?.lineNumbers ?? true) ? "on" : "off",
        lineNumbersMinChars: 3,
        minimap: { enabled: options?.minimap ?? false },
        quickSuggestions: true,
        renderLineHighlight: options?.renderLineHighlight ?? "line",
        renderWhitespace: options?.renderWhitespace ?? "none",
        scrollBeyondLastLine: false,
        suggestOnTriggerCharacters: true,
        tabSize: options?.tabSize ?? 2,
        wordBasedSuggestions: "off",
        wordWrap: (options?.wordWrap ?? true) ? "on" : "off",
      }}
    />
  );
}
