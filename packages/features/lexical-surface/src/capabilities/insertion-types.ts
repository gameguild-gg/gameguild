import type * as React from "react";
import type { LexicalEditor } from "lexical";
import type { LexicalSurfaceFeatures } from "./feature-flags";

export type InsertionSurface = "toolbar" | "picker";

export type InsertionDialogDefinition = {
  title: string;
  contentClassName?: string;
  render: (options: {
    activeEditor: LexicalEditor;
    onClose: () => void;
  }) => React.ReactNode;
};

export type InsertionDefinition = {
  id: string;
  feature: keyof LexicalSurfaceFeatures;
  label: string;
  keywords: readonly string[];
  Icon: React.ComponentType<{
    className?: string;
    style?: React.CSSProperties;
  }>;
  execute?: (editor: LexicalEditor) => void;
  dialog?: InsertionDialogDefinition;
  surfaces: readonly InsertionSurface[];
};
