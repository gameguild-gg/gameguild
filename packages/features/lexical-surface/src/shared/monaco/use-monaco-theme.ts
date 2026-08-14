"use client";

import { useCallback, useEffect, useRef } from "react";
import type { Monaco } from "@monaco-editor/react";
import { isShikiActive, useShikiReady } from "./shiki-highlighter";
import { getShikiThemeName, type ShikiTheme } from "./shiki-themes";
import {
  registerMonacoSurface,
  type MonacoThemeHandle,
} from "./theme-coordinator";

export function useMonacoTheme({
  theme,
  isDark,
  fallbackLight = "light",
  fallbackDark = "vs-dark",
}: {
  theme: ShikiTheme;
  isDark: boolean;
  fallbackLight?: string;
  fallbackDark?: string;
}) {
  const shikiReady = useShikiReady();
  const resolveTheme = useCallback(
    () =>
      shikiReady && isShikiActive()
        ? getShikiThemeName(theme, isDark)
        : isDark
          ? fallbackDark
          : fallbackLight,
    [fallbackDark, fallbackLight, isDark, shikiReady, theme],
  );
  const resolveRef = useRef(resolveTheme);
  resolveRef.current = resolveTheme;
  const handleRef = useRef<MonacoThemeHandle | null>(null);

  const bindMonaco = useCallback((monaco: Monaco) => {
    if (!handleRef.current) {
      handleRef.current = registerMonacoSurface(monaco, () =>
        resolveRef.current(),
      );
    }
  }, []);

  useEffect(() => handleRef.current?.refresh(), [resolveTheme]);
  useEffect(
    () => () => {
      handleRef.current?.unregister();
      handleRef.current = null;
    },
    [],
  );

  return { currentTheme: resolveTheme(), bindMonaco };
}
