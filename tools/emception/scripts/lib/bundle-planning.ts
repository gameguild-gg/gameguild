export function pruneFullyDeduplicatedBundles(
  bundleFiles: Map<string, string[]>,
  manifestFiles: Readonly<Record<string, { readonly symlink?: string }>>,
): string[] {
  const removed: string[] = [];
  for (const [name, files] of bundleFiles) {
    if (files.length > 0 && files.every((filename) => Boolean(manifestFiles[filename]?.symlink))) {
      bundleFiles.delete(name);
      removed.push(name);
    }
  }
  return removed;
}
