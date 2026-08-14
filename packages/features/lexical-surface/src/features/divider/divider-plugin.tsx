"use client";

import { useEffect } from "react";
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
import { $createDividerLexicalNode, DividerLexicalNode } from "./divider-node";

export const INSERT_DIVIDER_LEXICAL_COMMAND: LexicalCommand<void> =
  createCommand("INSERT_DIVIDER_LEXICAL_COMMAND");

export function DividerPlugin() {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    if (!editor.hasNodes([DividerLexicalNode])) {
      throw new Error(
        "DividerPlugin: DividerLexicalNode not registered on editor",
      );
    }
    return editor.registerCommand<void>(
      INSERT_DIVIDER_LEXICAL_COMMAND,
      () => {
        const node = $createDividerLexicalNode();
        $insertNodes([node]);
        if ($isRootOrShadowRoot(node.getParentOrThrow())) {
          $wrapNodeInElement(node, $createParagraphNode).selectEnd();
        }
        return true;
      },
      COMMAND_PRIORITY_EDITOR,
    );
  }, [editor]);

  return null;
}
