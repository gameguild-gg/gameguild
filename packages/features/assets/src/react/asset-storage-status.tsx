"use client";

import * as React from "react";
import { Database, ShieldCheck } from "lucide-react";
import { Button } from "@game-guild/ui/components/button";
import { useAssetRepository, useAssetStorageStatus } from "./use-assets";

export interface AssetStorageStatusProps {
  className?: string;
  allowPersistenceRequest?: boolean;
}

function formatBytes(size: number): string {
  if (size < 1024 * 1024) return `${Math.max(1, Math.round(size / 1024))} KB`;
  return `${(size / (1024 * 1024)).toFixed(1)} MB`;
}

export function AssetStorageStatus({
  className,
  allowPersistenceRequest = true,
}: AssetStorageStatusProps) {
  const repository = useAssetRepository();
  const { status, error } = useAssetStorageStatus();
  const [persisted, setPersisted] = React.useState<boolean | null>(null);

  if (error) return <span className={className}>Storage unavailable</span>;
  if (!status) return <span className={className}>Checking storage...</span>;
  const durable = persisted ?? status.persisted;

  return (
    <div className={`flex min-w-0 items-center gap-2 text-xs text-muted-foreground ${className ?? ""}`}>
      <Database className="h-4 w-4 shrink-0" />
      <span className="truncate">
        {status.backend === "memory" ? "Temporary memory" : "Browser database"}
        {status.localBytes ? ` · ${formatBytes(status.localBytes)}` : ""}
      </span>
      {durable ? (
        <span className="inline-flex items-center gap-1"><ShieldCheck className="h-3.5 w-3.5" /> Persistent</span>
      ) : allowPersistenceRequest && status.persisted === false ? (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="h-7 px-2 text-xs"
          onClick={() => void repository.requestPersistentStorage().then((result) => setPersisted(result.persisted))}
        >
          Keep on device
        </Button>
      ) : null}
    </div>
  );
}
