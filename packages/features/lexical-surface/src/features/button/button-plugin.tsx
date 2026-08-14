/**
 * ButtonPlugin — registers INSERT_BUTTON_LEXICAL_COMMAND.
 */
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
import type { ButtonActionType, ButtonVariant } from "./button-node";
import { $createButtonLexicalNode, ButtonLexicalNode } from "./button-node";

export type ButtonPayload = {
  text?: string;
  url?: string;
  actionType?: ButtonActionType;
  variant?: ButtonVariant;
};

export const INSERT_BUTTON_LEXICAL_COMMAND: LexicalCommand<
  ButtonPayload | undefined
> = createCommand("INSERT_BUTTON_LEXICAL_COMMAND");

export function ButtonPlugin() {
  const [editor] = useLexicalComposerContext();

  useEffect(() => {
    if (!editor.hasNodes([ButtonLexicalNode])) {
      throw new Error(
        "ButtonPlugin: ButtonLexicalNode not registered on editor",
      );
    }
    return editor.registerCommand<ButtonPayload | undefined>(
      INSERT_BUTTON_LEXICAL_COMMAND,
      (payload) => {
        const node = $createButtonLexicalNode(
          payload?.text ?? "Click me",
          payload?.url ?? "",
          payload?.actionType ?? "url",
          payload?.variant ?? "solid",
        );
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
