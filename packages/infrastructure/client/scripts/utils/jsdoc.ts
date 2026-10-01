/** Format multi-line OpenAPI text as safe JSDoc content lines. */
export function formatJsDocLines(value: string, indent = ''): string[] {
  const lines = value.replace(/\r\n?/g, '\n').split('\n');
  while (lines.length > 0 && !lines[0].trim()) lines.shift();
  while (lines.length > 0 && !lines[lines.length - 1].trim()) lines.pop();
  if (lines.length === 0) return [];

  const indentation = Math.min(
    ...lines.filter((line) => line.trim()).map((line) => line.match(/^[\t ]*/)?.[0].length ?? 0)
  );
  return lines.map((line) => {
    const content = line.slice(indentation).trimEnd().replace(/\*\//g, '* /');
    return content ? `${indent} * ${content}` : `${indent} *`;
  });
}
