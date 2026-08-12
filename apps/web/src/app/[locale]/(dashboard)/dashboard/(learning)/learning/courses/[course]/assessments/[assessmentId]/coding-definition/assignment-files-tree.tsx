"use client";

import React, { type ReactElement } from "react";
import { Plus, Trash2 } from "lucide-react";
import { Button } from "@game-guild/ui/components/button";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import { Switch } from "@game-guild/ui/components/switch";
import type { FileVisibility } from "@/lib/coding-assignment/client";
import { VisibilitySelect } from "./visibility-select";

/** In-memory file row surfaced by the authoring tree. */
export interface AssignmentFileRow {
  path: string;
  content: string;
  visibility: FileVisibility;
  modifiable: boolean;
}

export interface AssignmentFilesTreeProps {
  files: AssignmentFileRow[];
  onChange: (path: string, patch: Partial<AssignmentFileRow>) => void;
  onAdd: (path: string, content: string) => void;
  onRemove: (path: string) => void;
}

/**
 * Per-file authoring panel — drives the parent state which in turn calls
 * `IdeHandle.addFile/removeFile/setFileMeta`. The instructor authoring view
 * always allows Add File (the workspace `AllowStudentCreateFiles` gate is for
 * students — Task 9 — not the author).
 */
export function AssignmentFilesTree({
  files,
  onChange,
  onAdd,
  onRemove,
}: AssignmentFilesTreeProps): ReactElement {
  const [adding, setAdding] = React.useState(false);
  const [newPath, setNewPath] = React.useState("");
  const [newContent, setNewContent] = React.useState("");

  function confirmAdd() {
    const path = newPath.trim();
    if (!path) return;
    onAdd(path, newContent);
    setAdding(false);
    setNewPath("");
    setNewContent("");
  }

  return (
    <div className="space-y-3" data-testid="assignment-files-tree">
      {files.length === 0 && (
        <p className="text-muted-foreground text-sm" data-testid="no-files">
          No starter files yet. Add one below.
        </p>
      )}
      {files.map((f) => (
        <div
          key={f.path}
          className="grid grid-cols-[1fr_8rem_auto_auto] gap-2 items-center border rounded-md p-2"
          data-testid={`file-row-${f.path}`}
        >
          <div className="truncate" data-testid={`file-path-${f.path}`}>
            {f.path}
          </div>
          <div data-testid={`file-visibility-${f.path}`}>
            <VisibilitySelect
              value={f.visibility}
              onChange={(v) => onChange(f.path, { visibility: v })}
              testId={`file-visibility-select-${f.path}`}
            />
          </div>
          <label
            className="flex items-center gap-1 text-xs"
            data-testid={`file-modifiable-label-${f.path}`}
          >
            <Switch
              checked={f.modifiable}
              onCheckedChange={(v) => onChange(f.path, { modifiable: v })}
              data-testid={`file-modifiable-${f.path}`}
            />
            Modifiable
          </label>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => onRemove(f.path)}
            data-testid={`file-remove-${f.path}`}
          >
            <Trash2 className="h-3 w-3" />
          </Button>
        </div>
      ))}

      {adding ? (
        <div className="space-y-2 border rounded-md p-3" data-testid="file-add-form">
          <div className="space-y-1">
            <Label className="text-xs">Path</Label>
            <Input
              value={newPath}
              onChange={(e) => setNewPath(e.target.value)}
              placeholder="/user/main.cpp"
              data-testid="file-add-path"
            />
          </div>
          <div className="space-y-1">
            <Label className="text-xs">Initial content</Label>
            <Input
              value={newContent}
              onChange={(e) => setNewContent(e.target.value)}
              data-testid="file-add-content"
            />
          </div>
          <div className="flex gap-2">
            <Button size="sm" onClick={confirmAdd} data-testid="file-add-confirm">
              Add
            </Button>
            <Button
              size="sm"
              variant="outline"
              onClick={() => setAdding(false)}
              data-testid="file-add-cancel"
            >
              Cancel
            </Button>
          </div>
        </div>
      ) : (
        <Button
          variant="outline"
          size="sm"
          onClick={() => setAdding(true)}
          data-testid="file-add-button"
        >
          <Plus className="mr-1 h-3 w-3" /> Add file
        </Button>
      )}
    </div>
  );
}
