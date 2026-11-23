import { CODING_TOOLCHAIN, parseCodingAuthoringDefinition, projectCodingDefinitionForLearner } from '@game-guild/coding';
import { loader } from '@monaco-editor/react';
import { createRoot } from 'react-dom/client';
import { CodingSurface } from '../coding-surface';
import type { CodingEditorController, CodingSurfaceSession } from '../types';

/** Actual package UI, installed Monaco assets and public runtime; no API/users. */
export function createCodingWorkbenchBrowserFixture(container: HTMLElement) {
  loader.config({ paths: { vs: '/monaco/vs' } });
  const root = createRoot(container);
  let editor: CodingEditorController | null = null;
  let session: CodingSurfaceSession | null = null;
  return {
    mount(kind: 'io' | 'function', contextKey: string) {
      editor = null;
      session = null;
      const definition = parseCodingAuthoringDefinition({
        schemaVersion: 1,
        type: 'coding-assignment',
        exerciseType: kind === 'function' ? 'function' : 'standard-io',
        toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN },
        environment: { language: 'cpp', runtime: 'wasi' },
        files: [
          {
            id: 'main',
            path: '/home/user/main.cpp',
            encoding: 'text',
            mutable: true,
            visibility: 'public',
            content:
              kind === 'function'
                ? 'int sum(int left, int right) { return left + right; }'
                : [
                    '#include <cstdio>',
                    'int main() {',
                    '  char name[32];',
                    '  std::printf("Name? "); std::fflush(stdout);',
                    '  if (!std::fgets(name, sizeof(name), stdin)) return 3;',
                    '  std::printf("Hello, %s", name); std::fprintf(stderr, "input received\\n"); return 0;',
                    '}',
                  ].join('\n'),
          },
        ],
        tests:
          kind === 'function'
            ? [
                {
                  id: 'sum',
                  name: 'Adds two integers',
                  kind: 'function',
                  visibility: 'public',
                  weightUnits: 1,
                  function: {
                    name: 'sum',
                    parameters: [
                      { name: 'left', type: 'integer' },
                      { name: 'right', type: 'integer' },
                    ],
                    returnType: 'integer',
                  },
                  cases: [
                    {
                      id: 'add',
                      inputs: [
                        { type: 'integer', value: 2 },
                        { type: 'integer', value: 3 },
                      ],
                      expected: { type: 'integer', value: 5 },
                    },
                  ],
                },
              ]
            : [
                {
                  id: 'greet',
                  name: 'Greets Ada',
                  kind: 'stdio',
                  visibility: 'public',
                  weightUnits: 1,
                  stdin: 'Ada\n',
                  expected: { stdout: 'Name? Hello, Ada\n', stderr: 'input received\n', exitCode: 0 },
                },
              ],
        limits: { timeLimitMs: 3000, outputLimitBytes: 2_000_000, maxFileCount: 16 },
        entryPoint: { path: '/home/user/main.cpp', ...(kind === 'function' ? { symbol: 'sum' } : {}) },
        allowLearnerCreatedFiles: false,
      });
      root.render(
        <CodingSurface
          mode="learner"
          contextKey={contextKey}
          definition={projectCodingDefinitionForLearner(definition)}
          enableWorkspace={false}
          manifestUrl="/emception/manifest.json"
          onReady={(next) => {
            editor = next;
          }}
          onSessionReady={(next) => {
            session = next;
          }}
        />,
      );
    },
    ready: () => Boolean(editor && session),
    async getSubmission() {
      if (!session) throw new Error('Fixture editor is not ready.');
      return session.getSubmission();
    },
    unmount() {
      root.render(null);
      editor = null;
      session = null;
    },
    dispose() {
      root.unmount();
      editor = null;
      session = null;
    },
  };
}
