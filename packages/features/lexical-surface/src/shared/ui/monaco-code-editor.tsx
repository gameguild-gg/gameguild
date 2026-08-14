"use client";

import { lazy } from "react";
import type { EditorProps, OnMount } from "@monaco-editor/react";
import { ClientOnlyLazy } from "../client-only-lazy";
import { ensureShikiLoaded } from "../monaco/shiki-highlighter";
import type { ShikiTheme } from "../monaco/shiki-themes";
import { useMonacoTheme } from "../monaco/use-monaco-theme";

const LazyMonacoEditor = lazy(async () => ({
  default: (await import("@monaco-editor/react")).default,
}));

type MonacoCodeEditorProps = Omit<EditorProps, "theme"> & {
  shikiTheme: ShikiTheme;
  isDark: boolean;
  fallbackLight?: string;
  fallbackDark?: string;
};

export function MonacoCodeEditor({
  shikiTheme,
  isDark,
  fallbackLight,
  fallbackDark,
  beforeMount,
  onMount,
  ...props
}: MonacoCodeEditorProps) {
  const { currentTheme, bindMonaco } = useMonacoTheme({
    theme: shikiTheme,
    isDark,
    fallbackLight,
    fallbackDark,
  });
  const handleBeforeMount: EditorProps["beforeMount"] = (monaco) => {
    void ensureShikiLoaded(monaco);
    void beforeMount?.(monaco);
  };
  const handleMount: OnMount = (editor, monaco) => {
    bindMonaco(monaco);
    onMount?.(editor, monaco);
  };

  return (
    <ClientOnlyLazy
      component={LazyMonacoEditor}
      props={{
        ...props,
        beforeMount: handleBeforeMount,
        onMount: handleMount,
        theme: currentTheme,
      }}
      fallback={
        <div className="flex h-full min-h-64 items-center justify-center bg-gray-950 text-sm text-gray-400">
          Loading editor...
        </div>
      }
    />
  );
}
