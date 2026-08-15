"use client";

import * as React from "react";
import type { AssetRepository } from "../repository/asset-repository";
import { getDefaultBrowserAssetRepository } from "../browser/browser-asset-repository";
import type { AssetProcessor } from "../processing/asset-processor";

interface AssetsContextValue {
  repository: AssetRepository;
  revision: number;
  notifyMutation: () => void;
  processors: readonly AssetProcessor[];
  provided: boolean;
}

const defaultAssetsContext: AssetsContextValue = {
  repository: getDefaultBrowserAssetRepository(),
  revision: 0,
  notifyMutation: () => undefined,
  processors: [],
  provided: false,
};

const AssetsContext = React.createContext<AssetsContextValue>(defaultAssetsContext);
const EMPTY_PROCESSORS: readonly AssetProcessor[] = [];

export interface AssetsProviderProps extends React.PropsWithChildren {
  repository?: AssetRepository;
  processors?: readonly AssetProcessor[];
}

export function AssetsProvider({ repository, processors = EMPTY_PROCESSORS, children }: AssetsProviderProps) {
  const [revision, setRevision] = React.useState(0);
  const value = React.useMemo<AssetsContextValue>(
    () => ({
      repository: repository ?? getDefaultBrowserAssetRepository(),
      revision,
      notifyMutation: () => setRevision((current) => current + 1),
      processors,
      provided: true,
    }),
    [processors, repository, revision],
  );

  return <AssetsContext.Provider value={value}>{children}</AssetsContext.Provider>;
}

export function useAssetsContext(): AssetsContextValue {
  return React.useContext(AssetsContext);
}

export function useHasAssetsProvider(): boolean {
  return useAssetsContext().provided;
}
