import { act, useEffect } from "react";
import { createRoot, type Root } from "react-dom/client";
import { LexicalComposer } from "@lexical/react/LexicalComposer";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import type { LexicalEditor } from "lexical";
import { afterEach, beforeAll, describe, expect, it } from "vitest";
import { EditableStatePlugin } from "./editable-state-plugin";

let root: Root | null = null;

beforeAll(() => {
  Object.assign(globalThis, { IS_REACT_ACT_ENVIRONMENT: true });
});

afterEach(() => {
  act(() => root?.unmount());
  root = null;
  document.body.replaceChildren();
});

function CaptureEditor({
  onReady,
}: {
  onReady: (editor: LexicalEditor) => void;
}) {
  const [editor] = useLexicalComposerContext();
  useEffect(() => {
    onReady(editor);
  }, [editor, onReady]);
  return null;
}

describe("EditableStatePlugin", () => {
  it("updates an existing editor when read-only state changes", async () => {
    const container = document.createElement("div");
    document.body.appendChild(container);
    root = createRoot(container);
    const capturedEditor: { current: LexicalEditor | null } = { current: null };

    const render = async (editable: boolean) => {
      await act(async () => {
        root?.render(
          <LexicalComposer
            initialConfig={{
              namespace: "editable-state-test",
              editable: true,
              onError: (error) => {
                throw error;
              },
            }}
          >
            <EditableStatePlugin editable={editable} />
            <CaptureEditor
              onReady={(nextEditor) => (capturedEditor.current = nextEditor)}
            />
          </LexicalComposer>,
        );
      });
    };

    await render(true);
    expect(capturedEditor.current?.isEditable()).toBe(true);

    await render(false);
    expect(capturedEditor.current?.isEditable()).toBe(false);

    await render(true);
    expect(capturedEditor.current?.isEditable()).toBe(true);
  });
});
