import type { LexicalEditor, SerializedEditorState } from "lexical";

/**
 * Builds an `initialConfig.editorState` value for `LexicalComposer` from a
 * `SerializedEditorState` object (our canonical storage format). Returns
 * `undefined` when there is nothing to seed, so Lexical creates an empty
 * editor state.
 */
export function buildInitialEditorState(
  state: SerializedEditorState | null | undefined,
): undefined | ((editor: LexicalEditor) => void) {
  if (!state) return undefined;
  return (editor: LexicalEditor) => {
    const parsed = editor.parseEditorState(state);
    editor.setEditorState(parsed);
  };
}

/**
 * Returns the input `SerializedEditorState` with any persisted `selection`
 * stripped. Lexical, even with `editable: false`, restores that selection
 * on mount, which can auto-scroll the page to where the selection was
 * \u2014 causing the page to jump every time a read-only Lexical editor
 * hydrates (e.g. opening a project in the studio).
 */
export function stripSelection(
  state: SerializedEditorState | null | undefined,
): SerializedEditorState | null {
  if (!state) return null;
  const clone = { ...state } as SerializedEditorState & { selection?: unknown };
  if ("selection" in clone) delete clone.selection;
  return clone;
}
