"use client";

import { useEffect, useState } from "react";
import type { Monaco } from "@monaco-editor/react";
import type { Highlighter } from "shiki";
import { SHIKI_THEME_NAMES } from "./shiki-themes";

let loaderConfigured = false;
let loaderPromise: Promise<void> | null = null;
let highlighter: Highlighter | null = null;
let highlighterPromise: Promise<Highlighter> | null = null;
let appliedToMonaco = false;
let bindingPromise: Promise<void> | null = null;
const readyListeners = new Set<() => void>();

async function configureLocalMonaco(monaco?: Monaco): Promise<void> {
  if (loaderConfigured || typeof window === "undefined") return;
  if (!loaderPromise) {
    loaderPromise = Promise.all([
      import("@monaco-editor/react"),
      monaco ? Promise.resolve(monaco) : import("monaco-editor"),
    ])
      .then(([monacoReact, monacoRuntime]) => {
        monacoReact.loader.config({ monaco: monacoRuntime as Monaco });
        loaderConfigured = true;
      })
      .catch((error) => {
        loaderPromise = null;
        throw error;
      });
  }
  await loaderPromise;
}

void configureLocalMonaco();

async function getHighlighter(): Promise<Highlighter> {
  if (highlighter) return highlighter;
  if (!highlighterPromise) {
    highlighterPromise = import("shiki")
      .then(({ createHighlighter }) =>
        createHighlighter({
          themes: SHIKI_THEME_NAMES,
          langs: ["json", "mermaid"],
        }),
      )
      .then((loaded) => {
        highlighter = loaded;
        return loaded;
      });
  }
  return highlighterPromise;
}

export function isShikiActive(): boolean {
  return appliedToMonaco;
}

export async function ensureShikiLoaded(monaco: Monaco): Promise<void> {
  if (appliedToMonaco) return;
  if (!bindingPromise) {
    bindingPromise = (async () => {
      try {
        await configureLocalMonaco(monaco);
        const [{ shikiToMonaco }, loadedHighlighter] = await Promise.all([
          import("@shikijs/monaco"),
          getHighlighter(),
        ]);
        shikiToMonaco(loadedHighlighter, monaco);
        appliedToMonaco = true;
        readyListeners.forEach((listener) => listener());
      } catch (error) {
        bindingPromise = null;
        console.error("Failed to initialize Shiki for Monaco", error);
      }
    })();
  }
  await bindingPromise;
}

export function useShikiReady(): boolean {
  const [ready, setReady] = useState(appliedToMonaco);

  useEffect(() => {
    if (appliedToMonaco) {
      setReady(true);
      return;
    }
    const listener = () => setReady(true);
    readyListeners.add(listener);
    return () => {
      readyListeners.delete(listener);
    };
  }, []);

  return ready;
}
