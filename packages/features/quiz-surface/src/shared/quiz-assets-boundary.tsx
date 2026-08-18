"use client";

import type { ReactNode } from "react";
import { AssetsProvider, useHasAssetsProvider } from "@game-guild/assets/react";

export function QuizAssetsBoundary({ children }: { children: ReactNode }) {
  const hasAssetsProvider = useHasAssetsProvider();
  return hasAssetsProvider ? children : <AssetsProvider>{children}</AssetsProvider>;
}
