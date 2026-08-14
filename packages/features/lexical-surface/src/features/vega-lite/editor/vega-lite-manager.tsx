"use client";

import { useState } from "react";
import type { AssetRecord } from "@game-guild/assets";
import { AssetPickerDialog } from "@game-guild/assets/react";
import { Button } from "@game-guild/ui/components/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@game-guild/ui/components/dropdown-menu";
import { Check, Copy, FileJson, FileText, Trash2 } from "lucide-react";
import type { VegaDataAttachment } from "../vega-lite-data";

interface VegaLiteManagerProps {
  attachments: Record<string, VegaDataAttachment>;
  onAttachmentsChange: (attachments: Record<string, VegaDataAttachment>) => void;
}

function asAttachment(asset: AssetRecord): VegaDataAttachment | null {
  const json = asset.mimeType === "application/json" || asset.name.endsWith(".json");
  const csv = asset.mimeType === "text/csv" || asset.name.endsWith(".csv");
  if (!json && !csv) return null;
  return {
    name: asset.name,
    assetUri: asset.uri,
    mimeType: json ? "application/json" : "text/csv",
    size: asset.size,
  };
}

export function VegaLiteManager({
  attachments,
  onAttachmentsChange,
}: VegaLiteManagerProps) {
  const [open, setOpen] = useState(false);
  const [copiedFile, setCopiedFile] = useState<string | null>(null);

  const addAssets = (value: AssetRecord | AssetRecord[]) => {
    const next = { ...attachments };
    for (const asset of Array.isArray(value) ? value : [value]) {
      const attachment = asAttachment(asset);
      if (attachment) next[attachment.name] = attachment;
    }
    onAttachmentsChange(next);
  };

  const remove = (filename: string) => {
    const next = { ...attachments };
    delete next[filename];
    onAttachmentsChange(next);
  };

  const copyUrl = async (filename: string) => {
    await navigator.clipboard.writeText(`data:${filename}`);
    setCopiedFile(filename);
    window.setTimeout(() => setCopiedFile(null), 1500);
  };

  const count = Object.keys(attachments).length;

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" size="sm" className="gap-2">
            <FileText className="h-4 w-4" />
            Data
            {count > 0 && (
              <span className="ml-1 rounded-full bg-blue-500 px-2 py-0.5 text-xs text-white">
                {count}
              </span>
            )}
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="z-[70] w-80">
          <DropdownMenuLabel>Chart datasets</DropdownMenuLabel>
          {Object.entries(attachments).map(([filename, attachment]) => (
            <div key={filename} className="flex items-center px-1">
              <DropdownMenuItem
                className="min-w-0 flex-1"
                onSelect={() => void copyUrl(filename)}
              >
                {attachment.mimeType === "application/json" ? (
                  <FileJson className="h-4 w-4" />
                ) : (
                  <FileText className="h-4 w-4" />
                )}
                <span className="truncate">{filename}</span>
                {copiedFile === filename ? (
                  <Check className="ml-auto h-4 w-4" />
                ) : (
                  <Copy className="ml-auto h-4 w-4" />
                )}
              </DropdownMenuItem>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-8 w-8 text-destructive"
                aria-label={`Remove ${filename}`}
                onClick={() => remove(filename)}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>
          ))}
          {count > 0 && <DropdownMenuSeparator />}
          <DropdownMenuItem onSelect={() => setOpen(true)}>
            <FileText className="h-4 w-4" />
            Add datasets
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <AssetPickerDialog
        open={open}
        onOpenChange={setOpen}
        onSelect={addAssets}
        title="Chart datasets"
        description="Upload or select CSV and JSON datasets. Reference them as data:filename in the Vega-Lite specification."
        accept=".csv,.json,text/csv,application/json"
        kinds={["dataset"]}
        maxSizeBytes={25 * 1024 * 1024}
        multiple
      />

    </>
  );
}
