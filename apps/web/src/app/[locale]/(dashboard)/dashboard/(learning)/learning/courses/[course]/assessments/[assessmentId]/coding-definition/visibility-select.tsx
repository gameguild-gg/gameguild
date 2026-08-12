"use client";

import React, { type ReactElement } from "react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import type { FileVisibility } from "@/lib/coding-assignment/client";

interface VisibilitySelectProps {
  value: FileVisibility;
  onChange: (next: FileVisibility) => void;
  /** Stable id suffix so Radix Select can wire trigger ↔ content for SRs. */
  id?: string;
  testId?: string;
}

/**
 * Shared `Public` | `Private` selector for tests AND files (unified per draft).
 * Wire values are PascalCase per v1 contract.
 */
export function VisibilitySelect({
  value,
  onChange,
  id,
  testId,
}: VisibilitySelectProps): ReactElement {
  return (
    <Select value={value} onValueChange={(v) => onChange(v as FileVisibility)}>
      <SelectTrigger id={id} data-testid={testId}>
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="Public">Public</SelectItem>
        <SelectItem value="Private">Private</SelectItem>
      </SelectContent>
    </Select>
  );
}
