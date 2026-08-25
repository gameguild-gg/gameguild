/** Convert a native Windows path for commands interpreted by Git Bash/MSYS. */
export function toMsysPath(value: string, platform: NodeJS.Platform = process.platform): string {
  const normalized = value.replaceAll('\\', '/');
  if (platform !== 'win32') return normalized;
  const drive = normalized.match(/^([A-Za-z]):\/(.*)$/);
  return drive ? `/${drive[1].toLowerCase()}/${drive[2]}` : normalized;
}

/** Rewrite MSYS source references for native Windows tools such as mingw32-make. */
export function rewriteMsysPathReferences(
  content: string,
  nativePath: string,
  platform: NodeJS.Platform = process.platform,
): string {
  if (platform !== 'win32') return content;
  return content.replaceAll(toMsysPath(nativePath, platform), nativePath.replaceAll('\\', '/'));
}
