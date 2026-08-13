import { act, useEffect, type ComponentType } from "react";
import { createRoot, type Root } from "react-dom/client";
import { LexicalComposer } from "@lexical/react/LexicalComposer";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import type {
  Klass,
  LexicalCommand,
  LexicalEditor,
  LexicalNode,
} from "lexical";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";

vi.mock("../features/mermaid/mermaid-editor", () => ({
  MermaidEditor: () => <div data-testid="built-in-mermaid-editor" />,
}));

vi.mock("../features/vega-lite/vega-lite-editor", () => ({
  VegaLiteEditor: () => <div data-testid="built-in-vega-editor" />,
}));

import {
  INSERT_MERMAID_LEXICAL_COMMAND,
  MermaidPlugin,
} from "../features/mermaid/mermaid-plugin";
import { MermaidLexicalNode } from "../features/mermaid/mermaid-node";
import {
  INSERT_VEGA_LITE_LEXICAL_COMMAND,
  VegaLitePlugin,
} from "../features/vega-lite/vega-lite-plugin";
import { VegaLiteLexicalNode } from "../features/vega-lite/vega-lite-node";

let root: Root | null = null;

beforeAll(() => {
  Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
});

afterEach(() => {
  act(() => root?.unmount());
  root = null;
  document.body.innerHTML = "";
});

function CaptureEditor({
  onReady,
}: {
  onReady: (editor: LexicalEditor) => void;
}) {
  const [editor] = useLexicalComposerContext();
  useEffect(() => onReady(editor), [editor, onReady]);
  return null;
}

async function openBuiltInEditor(
  Plugin: ComponentType,
  node: Klass<LexicalNode>,
  command: LexicalCommand<void>,
) {
  const container = document.createElement("div");
  document.body.appendChild(container);
  root = createRoot(container);
  let editor: LexicalEditor | null = null;

  await act(async () => {
    root?.render(
      <LexicalComposer
        initialConfig={{
          namespace: "diagram-insertion-test",
          nodes: [node],
          onError: (error) => {
            throw error;
          },
        }}
      >
        <Plugin />
        <CaptureEditor
          onReady={(nextEditor) => {
            editor = nextEditor;
          }}
        />
      </LexicalComposer>,
    );
  });

  expect(editor).not.toBeNull();
  await act(async () => {
    editor?.dispatchCommand(command, undefined);
  });
}

describe("diagram insertion", () => {
  it("opens the built-in Mermaid editor without a host adapter", async () => {
    await openBuiltInEditor(
      MermaidPlugin,
      MermaidLexicalNode,
      INSERT_MERMAID_LEXICAL_COMMAND,
    );

    expect(
      document.querySelector('[data-testid="built-in-mermaid-editor"]'),
    ).not.toBeNull();
  });

  it("opens the built-in Vega-Lite editor without a host adapter", async () => {
    await openBuiltInEditor(
      VegaLitePlugin,
      VegaLiteLexicalNode,
      INSERT_VEGA_LITE_LEXICAL_COMMAND,
    );

    expect(
      document.querySelector('[data-testid="built-in-vega-editor"]'),
    ).not.toBeNull();
  });
});
