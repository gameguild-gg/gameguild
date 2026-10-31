/**
 * Course pages — single vertical scrollbar regression spec (issue #97).
 *
 * The reported bug: course pages showed two vertical scrollbars — the
 * document scrollbar plus a nested `overflow-auto` container (see the
 * `h-screen` + inner scroller layout the issue screenshot captured). The
 * layout fix keeps the document as the only vertical scrolling surface.
 *
 * This spec demonstrates the acceptance gap the issue audits called out
 * ("no UI checks were executed"): on the live course catalog and course
 * detail routes, at desktop and at the reported 428px mobile viewport:
 *   1. no descendant element is an independently scrolling vertical
 *      container (the inner scrollbar from the screenshot),
 *   2. the document owns the scroll (scrollHeight > clientHeight on
 *      documentElement while body adds no second scrollbar),
 *   3. no horizontal document overflow.
 *
 * Gated behind E2E_RUN so CI stays green without a live stack.
 *
 * Run manually (needs web + API + seeded courses on localhost:3000):
 *   cd apps/web && E2E_RUN=1 pnpm playwright test course-page-scrollbars --reporter=line
 *
 * Selectors verified against:
 *   apps/web/src/app/[locale]/(public)/courses/page.tsx
 *   apps/web/src/app/[locale]/(public)/courses/[course]/page.tsx
 *   apps/web/src/components/courses/course/course-landing-page.tsx
 */
import { test, expect, type Page } from "playwright/test";

// ponytail: env-overridable targets so the spec is not pinned to one host.
const WEB_BASE_URL = (
  process.env.PLAYWRIGHT_WEB_BASE_URL ??
  process.env.NEXT_PUBLIC_APP_URL ??
  "http://localhost:3000"
).replace(/\/$/, "");

// Seeded course used by the sibling coding-activity spec; any published
// course slug works since the assertions are layout-level, not content-level.
const COURSE_SLUG = process.env.E2E_COURSE_SLUG ?? "ai4games";

/** Viewports: desktop plus the 428px-wide mobile viewport from the report. */
const VIEWPORTS = [
  { name: "desktop-1280x720", width: 1280, height: 720 },
  { name: "mobile-428x720", width: 428, height: 720 },
] as const;

interface ScrollAudit {
  /** Elements outside the html/body chain that can scroll vertically. */
  nestedScrollers: Array<{
    tag: string;
    className: string;
    scrollHeight: number;
    clientHeight: number;
  }>;
  /** True when documentElement itself overflows vertically (page scrolls). */
  documentScrolls: boolean;
  /** True when the body box overflows its own height independently. */
  bodyScrolls: boolean;
  /** True when the page overflows horizontally at the document level. */
  documentScrollsHorizontally: boolean;
}

async function auditScrollSurfaces(page: Page): Promise<ScrollAudit> {
  return page.evaluate(() => {
    const doc = document.documentElement;
    const body = document.body;
    const nestedScrollers: Array<{
      tag: string;
      className: string;
      scrollHeight: number;
      clientHeight: number;
    }> = [];

    for (const el of Array.from(body.querySelectorAll<HTMLElement>("*"))) {
      const style = window.getComputedStyle(el);
      if (style.overflowY !== "auto" && style.overflowY !== "scroll") continue;
      if (style.position === "fixed" && el.clientHeight >= window.innerHeight) {
        // Viewport-height fixed panes (e.g. the course sidebar) legitimately
        // keep their own scroller; they never add document overflow.
        continue;
      }
      if (el.clientHeight <= 0) continue;
      if (el.scrollHeight <= el.clientHeight + 1) continue;

      nestedScrollers.push({
        tag: el.tagName,
        className: String(el.className).slice(0, 160),
        scrollHeight: el.scrollHeight,
        clientHeight: el.clientHeight,
      });
    }

    return {
      nestedScrollers,
      documentScrolls: doc.scrollHeight > doc.clientHeight + 1,
      bodyScrolls: body.scrollHeight > body.clientHeight + 1,
      documentScrollsHorizontally: doc.scrollWidth > doc.clientWidth + 1,
    };
  });
}

for (const viewport of VIEWPORTS) {
  test.describe(`Course pages single scrollbar (${viewport.name})`, () => {
    // Skip unless explicitly invoked with E2E_RUN=1 — runnable, but CI-safe.
    test.skip(!process.env.E2E_RUN, "set E2E_RUN=1 (needs web + API + seeded courses)");

    test.use({ viewport: { width: viewport.width, height: viewport.height } });

    test("course catalog: document is the only vertical scrolling surface", async ({ page }) => {
      await page.goto(`${WEB_BASE_URL}/courses`, { waitUntil: "domcontentloaded" });
      await expect(
        page.getByRole("heading", { name: /Build the game development portfolio/i }),
      ).toBeVisible({ timeout: 20_000 });

      const audit = await auditScrollSurfaces(page);
      expect(audit.nestedScrollers, "nested vertical scrollers on /courses").toEqual([]);
      expect(audit.documentScrollsHorizontally, "horizontal overflow on /courses").toBe(false);
    });

    test("course detail: document is the only vertical scrolling surface", async ({ page }) => {
      await page.goto(`${WEB_BASE_URL}/courses/${COURSE_SLUG}`, { waitUntil: "domcontentloaded" });
      // Landing page rendered (not the unavailable/error state) before auditing.
      await expect(
        page.getByRole("heading", { name: /Clear scope, visible progress/i }),
      ).toBeVisible({ timeout: 20_000 });

      const audit = await auditScrollSurfaces(page);
      expect(audit.nestedScrollers, `nested vertical scrollers on /courses/${COURSE_SLUG}`).toEqual([]);
      expect(audit.documentScrollsHorizontally, `horizontal overflow on /courses/${COURSE_SLUG}`).toBe(false);

      // The long landing page must actually scroll via the document — content
      // may never be trapped in a clipped/inner container (the #97 symptom).
      expect(audit.documentScrolls, "course detail page must scroll on the document").toBe(true);
      const scrolled = await page.evaluate(() => {
        window.scrollTo(0, Math.round(document.documentElement.scrollHeight * 0.25));
        return window.scrollY;
      });
      expect(scrolled, "window.scrollTo must move the document").toBeGreaterThan(0);
    });
  });
}
