"use client";

import * as React from "react";
import { Check, HardDrive, Search, Upload, X } from "lucide-react";
import { Button } from "@game-guild/ui/components/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog";
import { Input } from "@game-guild/ui/components/input";
import { cn } from "@game-guild/ui/lib/utils";
import type { AssetKind, AssetRecord, AssetScope } from "../core/asset-contracts";
import { validateAssetFile } from "../core/file-validation";
import { AssetPreview } from "./asset-preview";
import { useAssetLibrary, useAssetUpload } from "./use-assets";
import { AssetStorageStatus } from "./asset-storage-status";

export interface AssetPickerDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSelect: (assets: AssetRecord | AssetRecord[]) => void;
  title?: string;
  description?: string;
  accept?: string;
  kinds?: readonly AssetKind[];
  multiple?: boolean;
  scope?: AssetScope;
  maxSizeBytes?: number;
  includeRemote?: boolean;
}

function formatBytes(size: number): string {
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${(size / (1024 * 1024)).toFixed(1)} MB`;
}

export function AssetPickerDialog({
  open,
  onOpenChange,
  onSelect,
  title = "Choose assets",
  description = "Upload files or select assets stored in this browser.",
  accept,
  kinds,
  multiple = false,
  scope,
  maxSizeBytes,
  includeRemote = false,
}: AssetPickerDialogProps) {
  const [search, setSearch] = React.useState("");
  const [selected, setSelected] = React.useState<AssetRecord[]>([]);
  const inputRef = React.useRef<HTMLInputElement>(null);
  const uploadController = React.useRef<AbortController | null>(null);
  const [dragging, setDragging] = React.useState(false);
  const [validationError, setValidationError] = React.useState<string | null>(null);
  const { importFiles, uploading, progress, error: uploadError } = useAssetUpload();
  const { items, loading, error: listError } = useAssetLibrary({
    search,
    kinds,
    scope,
    includeRemote,
    limit: 200,
  });

  React.useEffect(() => {
    if (!open) {
      uploadController.current?.abort();
      uploadController.current = null;
      setSelected([]);
      setDragging(false);
      setValidationError(null);
    }
  }, [open]);

  const handleFiles = async (files: FileList | readonly File[] | null) => {
    if (!files?.length) return;
    const candidates = Array.from(files);
    const issue = candidates.flatMap((file) =>
      validateAssetFile(file, { accept, kinds, maxSizeBytes }),
    )[0];
    if (issue) {
      setValidationError(issue.message);
      return;
    }
    setValidationError(null);
    const controller = new AbortController();
    uploadController.current = controller;
    try {
      const records = await importFiles(candidates, { scope, signal: controller.signal });
      setSelected(multiple ? records : records.slice(0, 1));
    } catch (reason) {
      if (reason instanceof DOMException && reason.name === "AbortError") setValidationError(null);
      else setValidationError(reason instanceof Error ? reason.message : "Asset import failed.");
    } finally {
      if (uploadController.current === controller) uploadController.current = null;
      if (inputRef.current) inputRef.current.value = "";
    }
  };

  const toggle = (asset: AssetRecord) => {
    setSelected((current) => {
      if (!multiple) return [asset];
      return current.some((item) => item.uri === asset.uri)
        ? current.filter((item) => item.uri !== asset.uri)
        : [...current, asset];
    });
  };

  const confirm = () => {
    if (!selected.length) return;
    onSelect(multiple ? selected : selected[0]!);
    onOpenChange(false);
  };

  const error = validationError ?? uploadError?.message ?? listError?.message;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="flex max-h-[min(760px,90vh)] max-w-4xl flex-col overflow-hidden p-0">
        <DialogHeader className="border-b px-5 py-4">
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>

        <div className="flex min-h-0 flex-1 flex-col gap-3 px-5 py-4">
          <div
            className={cn(
              "flex flex-wrap items-center gap-2 rounded-md border border-dashed p-2 transition-colors",
              dragging && "border-primary bg-primary/5",
            )}
            onDragEnter={(event) => {
              event.preventDefault();
              setDragging(true);
            }}
            onDragOver={(event) => event.preventDefault()}
            onDragLeave={(event) => {
              if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setDragging(false);
            }}
            onDrop={(event) => {
              event.preventDefault();
              setDragging(false);
              void handleFiles(event.dataTransfer.files);
            }}
          >
            <div className="relative min-w-48 flex-1">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search assets"
                className="pl-9"
              />
            </div>
            <input
              ref={inputRef}
              type="file"
              accept={accept}
              multiple={multiple}
              className="sr-only"
              onChange={(event) => void handleFiles(event.target.files)}
            />
            <Button
              type="button"
              variant="outline"
              disabled={uploading}
              onClick={() => inputRef.current?.click()}
            >
              <Upload className="h-4 w-4" />
              {uploading ? `Importing ${progress.completed}/${progress.total}` : "Upload"}
            </Button>
            {uploading && (
              <Button
                type="button"
                variant="ghost"
                size="icon"
                title="Cancel import"
                aria-label="Cancel import"
                onClick={() => uploadController.current?.abort()}
              >
                <X className="h-4 w-4" />
              </Button>
            )}
            <span className="sr-only" role="status" aria-live="polite">
              {uploading ? "Importing assets" : ""}
            </span>
          </div>

          {error && (
            <p role="alert" className="text-sm text-destructive">
              {error}
            </p>
          )}

          <div className="min-h-64 flex-1 overflow-y-auto rounded-md border">
            {loading ? (
              <div className="grid min-h-64 place-items-center text-sm text-muted-foreground">
                Loading assets...
              </div>
            ) : items.length === 0 ? (
              <div className="grid min-h-64 place-items-center px-6 text-center text-sm text-muted-foreground">
                <div>
                  <HardDrive className="mx-auto mb-3 h-8 w-8" />
                  No matching assets in this browser.
                </div>
              </div>
            ) : (
              <div className="grid grid-cols-2 gap-2 p-2 sm:grid-cols-3 md:grid-cols-4">
                {items.map((asset) => {
                  const active = selected.some((item) => item.uri === asset.uri);
                  return (
                    <button
                      key={asset.uri}
                      type="button"
                      onClick={() => toggle(asset)}
                      className={cn(
                        "relative min-w-0 overflow-hidden rounded-md border bg-background text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                        active && "border-primary ring-1 ring-primary",
                      )}
                    >
                      <div className="grid aspect-video place-items-center overflow-hidden bg-muted">
                        <AssetPreview
                          asset={asset}
                          className="h-full w-full object-contain p-2"
                        />
                      </div>
                      <div className="min-w-0 p-2">
                        <p className="truncate text-sm font-medium">{asset.name}</p>
                        <p className="text-xs text-muted-foreground">
                          {formatBytes(asset.size)} · {asset.availability === "local-only" ? "Local only" : "Remote"}
                        </p>
                      </div>
                      {active && (
                        <span className="absolute right-2 top-2 grid h-6 w-6 place-items-center rounded-full bg-primary text-primary-foreground">
                          <Check className="h-4 w-4" />
                        </span>
                      )}
                    </button>
                  );
                })}
              </div>
            )}
          </div>
        </div>

        <DialogFooter className="border-t px-5 py-4 sm:justify-between">
          <AssetStorageStatus className="mr-auto" />
          <div className="flex items-center gap-2">
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="button" disabled={!selected.length} onClick={confirm}>
              Select{multiple && selected.length ? ` (${selected.length})` : ""}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
