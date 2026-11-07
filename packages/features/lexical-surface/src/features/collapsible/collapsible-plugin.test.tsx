import { act, useEffect } from "react";
import { createRoot, type Root } from "react-dom/client";
import { LexicalComposer } from "@lexical/react/LexicalComposer";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import {
  $createParagraphNode,
  $createTextNode,
  $getRoot,
  type EditorState,
  type ElementNode,
  type LexicalEditor,
  type LexicalNode,
} from "lexical";
import { afterEach, beforeAll, describe, expect, it } from "vitest";

import {
  $createCollapsibleContainerNode,
  CollapsibleContainerNode,
} from "./collapsible-container-node";
import {
  $createCollapsibleContentNode,
  CollapsibleContentNode,
} from "./collapsible-content-node";
import {
  $createCollapsibleTitleNode,
  CollapsibleTitleNode,
} from "./collapsible-title-node";
import { CollapsiblePlugin } from "./collapsible-plugin";

let root: Root | null = null;

beforeAll(() => {
  Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
});

afterEach(() => {
  act(() => root?.unmount());
  root = null;
  document.body.replaceChildren();
});

function CaptureEditor({ onReady }: { onReady: (editor: LexicalEditor) => void }) {
  const [editor] = useLexicalComposerContext();
  useEffect(() => {
    onReady(editor);
  }, [editor, onReady]);
  return null;
}

function mountCollapsibleEditor(): LexicalEditor {
  const captured: { current: LexicalEditor | null } = { current: null };
  const container = document.createElement("div");
  document.body.appendChild(container);
  root = createRoot(container);
  act(() => {
    root!.render(
      <LexicalComposer
        initialConfig={{
          namespace: "collapsible-transform-test",
          nodes: [CollapsibleContainerNode, CollapsibleTitleNode, CollapsibleContentNode],
          onError: (error) => {
            throw error;
          },
        }}
      >
        <CollapsiblePlugin />
        <CaptureEditor onReady={(editor) => (captured.current = editor)} />
      </LexicalComposer>,
    );
  });
  return captured.current!;
}

/** Flush pending node transforms by forcing an update cycle. */
async function settle(editor: LexicalEditor): Promise<EditorState> {
  await act(async () => {
    editor.update(() => {}, { discrete: true });
    await new Promise((resolve) => setTimeout(resolve, 0));
  });
  return editor.getEditorState();
}

function describeRoot(state: EditorState): string {
  let tree = "";
  state.read(() => {
    const walk = (node: LexicalNode): string => {
      const element = node as ElementNode;
      const children = typeof element.getChildren === "function" ? element.getChildren() : [];
      if (children.length === 0) return node.getType();
      return `${node.getType()}[${children.map(walk).join(", ")}]`;
    };
    tree = walk($getRoot());
  });
  return tree;
}

describe("CollapsiblePlugin structural transforms", () => {
  it("unwraps every child of an orphaned CollapsibleContentNode", async () => {
    const editor = mountCollapsibleEditor();
    editor.update(
      () => {
        $getRoot().clear();
        // Orphaned content node (no container parent) carrying three
        // children — the unwrap loop must relocate all of them even though
        // insertBefore mutates the child list during iteration.
        const orphan = $createCollapsibleContentNode();
        for (const text of ["first", "second", "third"]) {
          orphan.append($createParagraphNode().append($createTextNode(text)));
        }
        $getRoot().append(orphan);
      },
      { discrete: true },
    );
    const state = await settle(editor);
    // Every child survives the unwrap: getChildren() returns a fresh array in
    // Lexical 0.53, so mutation during iteration cannot skip children.
    expect(describeRoot(state)).toBe(
      "root[paragraph[text], paragraph[text], paragraph[text]]",
    );
  });

  it("keeps a well-formed container untouched", async () => {
    const editor = mountCollapsibleEditor();
    editor.update(
      () => {
        $getRoot().clear();
        const container = $createCollapsibleContainerNode(true);
        container.append(
          $createCollapsibleTitleNode().append($createParagraphNode()),
          $createCollapsibleContentNode().append($createParagraphNode()),
        );
        $getRoot().append(container);
      },
      { discrete: true },
    );
    const state = await settle(editor);
    expect(describeRoot(state)).toBe(
      "root[collapsible-container[collapsible-title[paragraph], collapsible-content[paragraph]]]",
    );
  });
});
