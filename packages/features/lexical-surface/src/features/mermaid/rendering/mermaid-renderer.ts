import { sanitizeSvg } from "../../../shared/security/sanitize-svg";
import { getMermaidConfigWithDarkTheme } from "../theme/mermaid-dark-themes";

const DARK_THEMES = new Set([
  "default-dark",
  "forest-dark",
  "neutral-dark",
  "base-dark",
]);

let renderQueue: Promise<void> = Promise.resolve();
let renderId = 0;

export function getMermaidRenderConfig(theme: string) {
  if (DARK_THEMES.has(theme)) {
    return getMermaidConfigWithDarkTheme(
      theme as "default-dark" | "forest-dark" | "neutral-dark" | "base-dark",
    );
  }

  return {
    startOnLoad: false,
    theme: theme as "default" | "dark" | "forest" | "neutral" | "base",
    securityLevel: "strict" as const,
    htmlLabels: false,
    fontFamily: "inherit",
    flowchart: { useMaxWidth: true, htmlLabels: false },
    logLevel: "error" as const,
    suppressErrorRendering: true,
  };
}

export function renderMermaidSvg(code: string, theme: string): Promise<string> {
  const task = renderQueue.then(async () => {
    const mermaid = (await import("mermaid")).default;
    mermaid.initialize(getMermaidRenderConfig(theme));

    const id = `mermaid-viewer-${++renderId}`;
    const { svg } = await mermaid.render(id, code);
    if (!svg) throw new Error("No SVG content generated");
    return sanitizeSvg(svg);
  });

  renderQueue = task.then(
    () => undefined,
    () => undefined,
  );
  return task;
}
