import { existsSync, mkdirSync, rmSync } from 'node:fs';

export function cleanGeneratedOutput(outputDirectory: string): void {
  if (existsSync(outputDirectory)) {
    rmSync(outputDirectory, { force: true, recursive: true });
  }

  mkdirSync(outputDirectory, { recursive: true });
}
