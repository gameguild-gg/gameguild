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

const socialMetadataKeys = [
  'journey',
  'startedAt',
  'completedAt',
  'durationMs',
  'apiBaseUrl',
  'webBaseUrl',
  'tenantId',
  'actorIds',
  'runTag',
];

export async function writeSocialFeedEvidence(outputPath, result) {
  const requiredCapabilities = result.requiredCapabilities ?? Object.keys(result.capabilities ?? {});
  const capabilities = Object.fromEntries(
    requiredCapabilities.map((key) => [key, result.capabilities?.[key] === true]),
  );
  const errors = Array.isArray(result.errors) ? result.errors.map(String) : [];
  const passedCount = Object.values(capabilities).filter(Boolean).length;
  const passed = passedCount === requiredCapabilities.length && errors.length === 0;
  const metadata = Object.fromEntries(
    socialMetadataKeys
      .filter((key) => result.metadata?.[key] !== undefined)
      .map((key) => [key, result.metadata[key]]),
  );
  const evidence = {
    schemaVersion: 1,
    status: passed ? 'passed' : 'failed',
    capabilities,
    aggregate: {
      passed,
      requiredCount: requiredCapabilities.length,
      passedCount,
    },
    errors,
    metadata,
  };

  await mkdir(dirname(outputPath), { recursive: true });
  await writeFile(outputPath, `${JSON.stringify(evidence, null, 2)}\n`, 'utf8');
}
