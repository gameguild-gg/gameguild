import { parseCodingAuthoringDraft, type CodingAuthoringDraft, type CodingExerciseType } from '@game-guild/coding';
import { loader } from '@monaco-editor/react';
import { useState } from 'react';
import { createRoot } from 'react-dom/client';
import { CodingAuthoringSurface } from '../authoring-surface';
import { exerciseValidationFixture } from './exercise-validation-fixtures';

/** Actual owned authoring/solving components and public runtime; no host/API. */
export function createCodingExerciseValidationBrowserFixture(container: HTMLElement) {
  loader.config({ paths: { vs: '/monaco/vs' } });
  const root = createRoot(container);
  let snapshot: CodingAuthoringDraft | null = null;
  let generation = 0;
  let identity = 0;
  function Host({ initial, contextKey }: { readonly initial: CodingAuthoringDraft; readonly contextKey: string }) {
    const [draft, setDraft] = useState(initial);
    return (
      <CodingAuthoringSurface
        contextKey={contextKey}
        draft={draft}
        initialTask="validate"
        manifestUrl="/emception/manifest.json"
        createId={(kind) => `${kind}-${++identity}`}
        onChange={(next) => {
          snapshot = parseCodingAuthoringDraft(next);
          setDraft(next);
        }}
      />
    );
  }
  return {
    mount(family: CodingExerciseType) {
      snapshot = exerciseValidationFixture(family);
      const contextKey = `exercise-check-${++generation}`;
      root.render(<Host key={contextKey} initial={snapshot} contextKey={contextKey} />);
      return contextKey;
    },
    getDraft() {
      if (!snapshot) throw new Error('Author fixture is not mounted.');
      return parseCodingAuthoringDraft(snapshot);
    },
    async inspectEditorModels() {
      const monaco: unknown = await loader.init();
      if (
        typeof monaco !== 'object' ||
        monaco === null ||
        !('editor' in monaco) ||
        typeof monaco.editor !== 'object' ||
        monaco.editor === null ||
        !('getModels' in monaco.editor) ||
        typeof monaco.editor.getModels !== 'function'
      )
        throw new TypeError('Monaco editor API is unavailable.');
      const models: unknown = monaco.editor.getModels();
      if (!Array.isArray(models)) throw new TypeError('Monaco models are unavailable.');
      return models.map((model: unknown) => {
        if (typeof model !== 'object' || model === null || !('getValue' in model) || typeof model.getValue !== 'function')
          throw new TypeError('Invalid Monaco model.');
        const value: unknown = model.getValue();
        if (typeof value !== 'string') throw new TypeError('Invalid Monaco source value.');
        return value;
      });
    },
    unmount() {
      root.render(null);
      snapshot = null;
    },
    dispose() {
      root.unmount();
      snapshot = null;
    },
  };
}
