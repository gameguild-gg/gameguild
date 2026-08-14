import { afterEach, describe, expect, it, vi } from "vitest";
import { openSafeUrl, sanitizeUrl } from "./safe-url";
import { sanitizeSvg } from "./sanitize-svg";

afterEach(() => {
  vi.restoreAllMocks();
});

describe("safe URLs", () => {
  it.each([
    "javascript:alert(1)",
    "data:text/html,<script>alert(1)</script>",
    "vbscript:msgbox(1)",
  ])("rejects the unsafe URL %s", (url) => {
    expect(sanitizeUrl(url)).toBe("about:blank");
  });

  it.each([
    "https://gameguild.gg",
    "/dashboard",
    "#section",
    "mailto:a@b.test",
  ])("preserves the safe URL %s", (url) => {
    expect(sanitizeUrl(url)).toBe(url);
  });

  it("opens safe URLs without an opener", () => {
    const opened = { opener: window } as unknown as Window;
    const open = vi.spyOn(window, "open").mockReturnValue(opened);

    expect(openSafeUrl("https://gameguild.gg")).toBe(opened);
    expect(open).toHaveBeenCalledWith(
      "https://gameguild.gg",
      "_blank",
      "noopener,noreferrer",
    );
    expect(opened.opener).toBeNull();
  });

  it("does not open unsafe URLs", () => {
    const open = vi.spyOn(window, "open");
    expect(openSafeUrl("javascript:alert(1)")).toBeNull();
    expect(open).not.toHaveBeenCalled();
  });
});

describe("SVG sanitization", () => {
  it("removes executable and embedded content", () => {
    const result = sanitizeSvg(`
      <svg xmlns="http://www.w3.org/2000/svg" onload="alert(1)">
        <script>alert(1)</script>
        <foreignObject><iframe src="https://example.test"></iframe></foreignObject>
        <a href="javascript:alert(1)"><text>unsafe</text></a>
        <rect id="chart" width="10" height="10" />
      </svg>
    `);

    expect(result).not.toMatch(
      /script|foreignObject|iframe|onload|javascript:/i,
    );
    expect(result).toContain('id="chart"');
  });
});
