import type { SerializedEditorState } from "lexical";
import { buildInitialEditorState } from "../schema/initial-editor-state";
import { LEXICAL_SURFACE_NODES } from "../schema/nodes";
import { LEXICAL_SURFACE_THEME } from "../schema/theme";

export function createSurfaceConfig({
  namespace,
  readOnly,
  initialState,
}: {
  namespace: string;
  readOnly: boolean;
  initialState: SerializedEditorState | null;
}) {
  return {
    namespace,
    nodes: LEXICAL_SURFACE_NODES,
    theme: LEXICAL_SURFACE_THEME,
    editable: !readOnly,
    editorState: buildInitialEditorState(initialState),
    onError: (error: Error) => {
      console.error(`[${namespace}]`, error);
    },
  };
}
