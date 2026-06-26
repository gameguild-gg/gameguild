import { readdirSync, readFileSync, statSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

const repoRoot = resolve(process.cwd(), '../..');

function readRepoFile(relativePath: string): string {
  return readFileSync(resolve(repoRoot, relativePath), 'utf8');
}

function readSourceFiles(relativePath: string): Array<{ file: string; source: string }> {
  const absolutePath = resolve(repoRoot, relativePath);
  return readdirSync(absolutePath).flatMap((entry) => {
    const filePath = resolve(absolutePath, entry);
    const repoRelativePath = `${relativePath}/${entry}`;
    const stats = statSync(filePath);

    if (stats.isDirectory()) {
      if (entry === 'docs') return [];
      return readSourceFiles(repoRelativePath);
    }

    if (!/\.[cm]?[jt]sx?$/.test(entry)) return [];
    return [{ file: repoRelativePath, source: readFileSync(filePath, 'utf8') }];
  });
}

describe('web runtime hardening', () => {
  it('keeps React Compiler enabled while giving the Docker build enough heap', () => {
    expect(readRepoFile('apps/web/next.config.ts')).toContain('reactCompiler: true');
    expect(readRepoFile('apps/web/Dockerfile')).toContain('NODE_OPTIONS=--max-old-space-size=4096');
  });

  it('uses a real web health probe and enables Coolify health checks', () => {
    const compose = readRepoFile('compose.coolify.yaml');

    expect(compose).toContain("fetch('http://127.0.0.1:3000/api/health')");
    expect(compose).toContain('x-coolify-healthcheck-enabled: true');
    expect(compose).toContain('x-coolify-healthcheck-path: /api/health');
  });

  it('logs web requests and route handler failures with structured runtime events', () => {
    expect(readRepoFile('apps/web/src/proxy.ts')).toContain('logWebRequest');
    expect(readRepoFile('apps/web/src/app/api/health/route.ts')).toContain('logWebRequest');
    expect(readRepoFile('apps/web/src/app/api/static-viewer/folder/[folderName]/route.ts')).toContain('logWebRequest');
    expect(readRepoFile('apps/web/src/app/api/static-viewer/file/[...path]/route.ts')).toContain('logWebRequest');
  });

  it('does not depend on the internal Next loadable alias', () => {
    expect(readRepoFile('apps/web/next.config.ts')).not.toContain(
      'next/dist/server/route-modules/app-page/vendored/contexts/loadable',
    );

    const dynamicImports = readSourceFiles('apps/web/src/components/block-content-editor').filter(({ source }) =>
      source.includes('next/dynamic'),
    );

    expect(dynamicImports.map(({ file }) => file)).toEqual([]);
  });
});
