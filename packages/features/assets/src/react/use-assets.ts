"use client";

import * as React from "react";
import type {
  AssetImportOptions,
  AssetQuery,
  AssetRecord,
  AssetStorageStatus,
} from "../core/asset-contracts";
import { isAssetUri, type AssetUri } from "../core/asset-uri";
import { useAssetsContext } from "./assets-provider";
import { runAssetProcessingPipeline } from "../processing/processing-pipeline";

export function useAssetRepository() {
  return useAssetsContext().repository;
}

export const useAssets = useAssetRepository;

export function useAsset(uri: AssetUri | null | undefined) {
  const { repository, revision } = useAssetsContext();
  const [record, setRecord] = React.useState<AssetRecord | null>(null);
  const [loading, setLoading] = React.useState(Boolean(uri));
  const [error, setError] = React.useState<Error | null>(null);

  React.useEffect(() => {
    let active = true;
    if (!uri) {
      setRecord(null);
      setLoading(false);
      return;
    }
    setLoading(true);
    setError(null);
    void repository.get(uri).then(
      (value) => {
        if (!active) return;
        setRecord(value);
        setLoading(false);
      },
      (reason: unknown) => {
        if (!active) return;
        setError(reason instanceof Error ? reason : new Error(String(reason)));
        setLoading(false);
      },
    );
    return () => {
      active = false;
    };
  }, [repository, revision, uri]);

  return { record, loading, error };
}

export function useResolvedAssetUrl(source: string | null | undefined) {
  const repository = useAssetRepository();
  const [url, setUrl] = React.useState(source ?? "");
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<Error | null>(null);

  React.useEffect(() => {
    let active = true;
    let release: (() => void) | undefined;
    if (!source || !isAssetUri(source)) {
      setUrl(source ?? "");
      setLoading(false);
      setError(null);
      return;
    }
    setLoading(true);
    setError(null);
    void repository.createObjectUrl(source).then(
      (resolved) => {
        if (!active) {
          resolved.release();
          return;
        }
        release = resolved.release;
        setUrl(resolved.url);
        setLoading(false);
      },
      (reason: unknown) => {
        if (!active) return;
        setUrl("");
        setError(reason instanceof Error ? reason : new Error(String(reason)));
        setLoading(false);
      },
    );
    return () => {
      active = false;
      release?.();
    };
  }, [repository, source]);

  return { url, loading, error };
}

export type AssetResolutionPurpose = "url" | "blob" | "text";

export function useResolvedAsset(
  uri: AssetUri | null | undefined,
  purpose: AssetResolutionPurpose,
) {
  const repository = useAssetRepository();
  const [value, setValue] = React.useState<string | Blob | null>(null);
  const [loading, setLoading] = React.useState(Boolean(uri));
  const [error, setError] = React.useState<Error | null>(null);

  React.useEffect(() => {
    let active = true;
    let release: (() => void) | undefined;
    if (!uri) {
      setValue(null);
      setLoading(false);
      setError(null);
      return;
    }
    setLoading(true);
    setError(null);
    const resolution = purpose === "url"
      ? repository.createObjectUrl(uri).then((result) => {
          release = result.release;
          return result.url;
        })
      : purpose === "text"
        ? repository.readText(uri)
        : repository.readBlob(uri);
    void resolution.then(
      (result) => {
        if (!active) {
          release?.();
          return;
        }
        setValue(result);
        setLoading(false);
      },
      (reason: unknown) => {
        if (!active) return;
        setError(reason instanceof Error ? reason : new Error(String(reason)));
        setLoading(false);
      },
    );
    return () => {
      active = false;
      release?.();
    };
  }, [purpose, repository, uri]);

  return { value, loading, error };
}

export function useAssetLibrary(query: AssetQuery = {}) {
  const { repository, revision } = useAssetsContext();
  const [items, setItems] = React.useState<AssetRecord[]>([]);
  const [loading, setLoading] = React.useState(true);
  const [error, setError] = React.useState<Error | null>(null);
  const serializedQuery = JSON.stringify(query);

  React.useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    void repository.list(query).then(
      (page) => {
        if (!active) return;
        setItems(page.items);
        setLoading(false);
      },
      (reason: unknown) => {
        if (!active) return;
        setError(reason instanceof Error ? reason : new Error(String(reason)));
        setLoading(false);
      },
    );
    return () => {
      active = false;
    };
  }, [repository, revision, serializedQuery]);

  return { items, loading, error };
}

export function useAssetUpload() {
  const { repository, notifyMutation, processors } = useAssetsContext();
  const [uploading, setUploading] = React.useState(false);
  const [progress, setProgress] = React.useState({ completed: 0, total: 0 });
  const [error, setError] = React.useState<Error | null>(null);

  const importFiles = React.useCallback(
    async (files: readonly File[], options?: AssetImportOptions) => {
      setUploading(true);
      setProgress({ completed: 0, total: files.length });
      setError(null);
      try {
        const records: AssetRecord[] = [];
        for (const file of files) {
          const processed = await runAssetProcessingPipeline(
            { blob: file, name: file.name, mimeType: file.type },
            processors,
            { signal: options?.signal },
          );
          records.push(await repository.importBlob(processed.blob, {
            ...options,
            name: processed.name,
            mimeType: processed.mimeType,
          }));
          setProgress((current) => ({ ...current, completed: current.completed + 1 }));
        }
        notifyMutation();
        return records;
      } catch (reason) {
        const nextError = reason instanceof Error ? reason : new Error(String(reason));
        setError(nextError);
        throw nextError;
      } finally {
        setUploading(false);
      }
    },
    [notifyMutation, processors, repository],
  );

  return { importFiles, uploading, progress, error };
}

export function useAssetStorageStatus() {
  const { repository, revision } = useAssetsContext();
  const [status, setStatus] = React.useState<AssetStorageStatus | null>(null);
  const [error, setError] = React.useState<Error | null>(null);

  React.useEffect(() => {
    let active = true;
    void repository.getStorageStatus().then(
      (value) => active && setStatus(value),
      (reason: unknown) =>
        active && setError(reason instanceof Error ? reason : new Error(String(reason))),
    );
    return () => {
      active = false;
    };
  }, [repository, revision]);

  return { status, error };
}
