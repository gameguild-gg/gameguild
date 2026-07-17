import { mkdir, writeFile } from 'node:fs/promises';
import { dirname } from 'node:path';

export async function writeBrowserEvidence(outputPath, result) {
  if (!outputPath) return;

  const evidence = {
    stats: {
      expected: result.passed ? 1 : 0,
      unexpected: result.passed ? 0 : 1,
      skipped: 0,
    },
    errors: result.errors,
    suites: [],
  };

  await mkdir(dirname(outputPath), { recursive: true });
  await writeFile(outputPath, `${JSON.stringify(evidence, null, 2)}\n`, 'utf8');
}
