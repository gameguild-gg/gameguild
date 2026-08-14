"use client";

import { useSyncExternalStore } from "react";

const DARK_MODE_QUERY = "(prefers-color-scheme: dark)";

function getDarkModeSnapshot(): boolean {
  if (typeof document === "undefined") return false;

  const root = document.documentElement;
  const explicitTheme = root.dataset.theme;
  if (explicitTheme === "dark" || root.classList.contains("dark")) return true;
  if (explicitTheme === "light" || root.classList.contains("light"))
    return false;

  return window.matchMedia(DARK_MODE_QUERY).matches;
}

function subscribeToDarkMode(onStoreChange: () => void): () => void {
  if (typeof document === "undefined") return () => undefined;

  const media = window.matchMedia(DARK_MODE_QUERY);
  const observer = new MutationObserver(onStoreChange);
  observer.observe(document.documentElement, {
    attributes: true,
    attributeFilter: ["class", "data-theme"],
  });
  media.addEventListener("change", onStoreChange);

  return () => {
    observer.disconnect();
    media.removeEventListener("change", onStoreChange);
  };
}

export function useDarkMode(): boolean {
  return useSyncExternalStore(
    subscribeToDarkMode,
    getDarkModeSnapshot,
    () => false,
  );
}
