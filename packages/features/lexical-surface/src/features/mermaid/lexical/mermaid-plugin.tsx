"use client";

import { useEffect, useState } from "react";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { $wrapNodeInElement } from "@lexical/utils";
import {
  $createParagraphNode,
  $insertNodes,
  $isRootOrShadowRoot,
  COMMAND_PRIORITY_EDITOR,
  createCommand,
  type LexicalCommand,
} from "lexical";
import { MermaidEditor } from "../editor/mermaid-editor";
import { $createMermaidLexicalNode, MermaidLexicalNode } from "./mermaid-node";
import type { MermaidData } from "../mermaid-data";
import type {
  MermaidDiagramType,
  MermaidThemeName,
  MermaidThemeMode,
} from "./mermaid-node";

export const INSERT_MERMAID_LEXICAL_COMMAND: LexicalCommand<void> =
  createCommand("INSERT_MERMAID_LEXICAL_COMMAND");

export function MermaidPlugin() {
  const [editor] = useLexicalComposerContext();
  const [isModalOpen, setModalOpen] = useState(false);
  useEffect(() => {
    if (!editor.hasNodes([MermaidLexicalNode])) {
      throw new Error(
        "MermaidPlugin: MermaidLexicalNode not registered on editor",
      );
    }
    return editor.registerCommand<void>(
      INSERT_MERMAID_LEXICAL_COMMAND,
      () => {
        setModalOpen(true);
        return true;
      },
      COMMAND_PRIORITY_EDITOR,
    );
  }, [editor]);

  const handleSave = (data: MermaidData) => {
    editor.update(() => {
      const node = $createMermaidLexicalNode(
        data.code,
        data.type as MermaidDiagramType,
      );
      node.setTheme((data.theme || "default") as MermaidThemeName);
      node.setThemeMode((data.themeMode || "system") as MermaidThemeMode);
      node.setTitle(data.title || "");
      node.setCaption(data.caption || "");
      node.setSize(data.size ?? 100);

      $insertNodes([node]);
      if ($isRootOrShadowRoot(node.getParentOrThrow())) {
        $wrapNodeInElement(node, $createParagraphNode).selectEnd();
      }
    });
    setModalOpen(false);
  };

  const handleCancel = () => {
    setModalOpen(false);
  };

  return isModalOpen ? (
    <MermaidEditor onSave={handleSave} onCancel={handleCancel} />
  ) : null;
}
