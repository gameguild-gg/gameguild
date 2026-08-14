"use client";

import { useTheme } from "next-themes";

export function useDarkMode(): boolean {
  return useTheme().resolvedTheme === "dark";
}
