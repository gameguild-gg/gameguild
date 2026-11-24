import { CODING_DRAFT_TYPE, CODING_TOOLCHAIN, parseCodingAuthoringDraft, type CodingAuthoringDraft, type CodingExerciseType } from '@game-guild/coding';
import { loader } from '@monaco-editor/react';
import { useState } from 'react';
import { createRoot } from 'react-dom/client';
import { CodingAuthoringSurface } from '../authoring-surface';

/** Real authoring component and Monaco, without a runtime, host, users or API. */
export function createCodingAuthoringBrowserFixture(container: HTMLElement) {
  loader.config({ paths: { vs: '/monaco/vs' } });
  const root = createRoot(container);
  let snapshot: CodingAuthoringDraft | null = null;
  let generation = 0;
  let identity = 0;
  function Harness({ initial, contextKey }: { readonly initial: CodingAuthoringDraft; readonly contextKey: string }) {
    const [draft, setDraft] = useState(initial);
    return (
      <CodingAuthoringSurface
        draft={draft}
        contextKey={contextKey}
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
      const initial = parseCodingAuthoringDraft({
        type: CODING_DRAFT_TYPE,
        schemaVersion: 1,
        exerciseType: family,
        problem: { statement: '', examples: [], constraints: [] },
        environment: { language: 'cpp', runtime: 'wasi' },
        toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN },
        files: [],
        entryPoint: { path: '' },
        functionSignature: null,
        tests: [],
        validationSolutions: [],
        limits: { timeLimitMs: '3000', outputLimitBytes: '2000000', memoryLimitBytes: '', maxFileCount: '16' },
        allowLearnerCreatedFiles: false,
      });
      snapshot = initial;
      const contextKey = `authoring-${++generation}`;
      root.render(<Harness key={contextKey} initial={initial} contextKey={contextKey} />);
      return contextKey;
    },
    getDraft() {
      if (!snapshot) throw new Error('The authoring fixture is not mounted.');
      return parseCodingAuthoringDraft(snapshot);
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
