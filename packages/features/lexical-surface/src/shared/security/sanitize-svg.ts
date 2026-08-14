import DOMPurify from "dompurify";

export function sanitizeSvg(svg: string): string {
  return DOMPurify.sanitize(svg, {
    USE_PROFILES: { svg: true, svgFilters: true },
    FORBID_TAGS: ["foreignObject", "iframe", "object", "embed", "script"],
  });
}
