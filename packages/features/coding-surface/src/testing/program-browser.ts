import { CODING_TOOLCHAIN, type CodingLearnerDelivery } from '@game-guild/coding';
import { createCodingProgramSession } from '../program-session';

export type ProgramBrowserControl = 'prompt' | 'compile-error' | 'infinite';

const prompt = `#include <stdio.h>
int main() {
  char name[80];
  printf("Name? "); fflush(stdout);
  if (!fgets(name, sizeof(name), stdin)) return 3;
  fprintf(stderr, "input received\\n");
  printf("Hello, %s", name);
  return 0;
}
`;

/** Synthetic runtime calibration, not a course fixture or academic judge. */
export function createProgramBrowserFixture(candidateKey: string, control: ProgramBrowserControl = 'prompt') {
  const source = control === 'prompt' ? prompt : control === 'compile-error' ? 'int main( { missing' : 'int main() { volatile unsigned counter = 0; while (1) { counter = counter + 1; } }';
  const delivery: CodingLearnerDelivery = {
    schemaVersion: 1, type: 'coding-assignment', exerciseType: 'standard-io',
    environment: { language: 'cpp', runtime: 'wasi' }, toolchain: { key: 'cpp-emception', ...CODING_TOOLCHAIN },
    files: [{ id: 'main', path: '/home/user/main.cpp', encoding: 'text', content: source, mutable: true }],
    publicTests: [], limits: { timeLimitMs: control === 'infinite' ? 500 : 30_000, outputLimitBytes: 2_000_000, maxFileCount: 16 },
    entryPoint: { path: '/home/user/main.cpp' }, allowLearnerCreatedFiles: false,
  };
  return createCodingProgramSession({ candidateKey, delivery, manifestUrl: '/emception/manifest.json' });
}
