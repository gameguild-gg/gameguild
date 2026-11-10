import assert from "node:assert/strict";
import { after, before, test } from "node:test";
import { chromium } from "playwright";
import { observeGradingAcknowledgement } from "./coding-cycle-grading-observer.mjs";
import { evaluateGradingTransport } from "./coding-cycle-grading-transport.mjs";

let browser;
before(async () => { browser = await chromium.launch({ headless: true }); });
after(async () => { await browser?.close(); });

const markup = '<span data-testid="needs-grading-badge">1 to grade</span><div data-testid="result-release-status">Awaiting review</div>';

test("records acknowledgement in the browser before later transport events can overtake Node polling", async () => {
  const page = await browser.newPage();
  try {
    await page.setContent(markup);
    const observer = await observeGradingAcknowledgement(page);
    const cutoff = await page.evaluate(async () => {
      document.querySelector('[data-testid="needs-grading-badge"]').textContent = "0 to grade";
      document.querySelector('[data-testid="result-release-status"]').textContent = "Ready to release: 100 / 100";
      await Promise.resolve();
      const acknowledgedAt = Date.now();
      // A busy renderer delays CDP/polling while the DOM mutation already committed.
      const until = Date.now() + 75;
      while (Date.now() < until) { /* deterministic event-loop ordering */ }
      return acknowledgedAt;
    });
    const events = await observer.collect();
    assert.deepEqual(events.map((event) => event.event).sort(), ["ui:final-result", "ui:zero-badge"]);
    assert.ok(events.every((event) => Date.parse(event.at) <= cutoff), "timestamps must describe DOM observation, not later Node delivery");
    await observer.dispose();
  } finally { await page.close(); }
});

test("pending review never creates a final acknowledgement", async () => {
  const page = await browser.newPage();
  try {
    await page.setContent(markup);
    const observer = await observeGradingAcknowledgement(page);
    assert.deepEqual(await observer.collect(), []);
    await observer.dispose();
  } finally { await page.close(); }
});

test("hidden acknowledgements are ignored and each visible transition is recorded only once", async () => {
  const page = await browser.newPage();
  try {
    await page.setContent(markup);
    const observer = await observeGradingAcknowledgement(page);
    await page.evaluate(() => {
      const badge = document.querySelector('[data-testid="needs-grading-badge"]');
      const result = document.querySelector('[data-testid="result-release-status"]');
      badge.hidden = result.hidden = true;
      badge.textContent = "0 to grade";
      result.textContent = "Ready to release";
    });
    assert.deepEqual(await observer.collect(), []);
    await page.evaluate(() => {
      document.querySelector('[data-testid="needs-grading-badge"]').hidden = false;
    });
    assert.deepEqual((await observer.collect()).map((event) => event.event), ["ui:zero-badge"]);
    await page.evaluate(() => {
      document.querySelector('[data-testid="result-release-status"]').hidden = false;
    });
    await page.evaluate(() => { document.body.append(document.createElement("span")); });
    assert.equal((await observer.collect()).length, 2);
    await observer.dispose();
    assert.deepEqual(await observer.collect(), []);
  } finally { await page.close(); }
});

test("a stale final grade cannot acknowledge a new mutation", async () => {
  const page = await browser.newPage();
  try {
    await page.setContent(markup.replace("1 to grade", "0 to grade"));
    await assert.rejects(observeGradingAcknowledgement(page), /must begin with a pending review/);
  } finally { await page.close(); }
});

test("a final interface reached after transport cancellation still fails the existing causal oracle", async () => {
  const page = await browser.newPage();
  try {
    await page.setContent(markup);
    const observer = await observeGradingAcknowledgement(page);
    const failed = await page.evaluate(() => {
      const failedAt = Date.now();
      while (Date.now() <= failedAt) { /* require a strictly later DOM transition */ }
      document.querySelector('[data-testid="needs-grading-badge"]').textContent = "0 to grade";
      document.querySelector('[data-testid="result-release-status"]').textContent = "Ready to release";
      return failedAt;
    });
    const at = (offset) => new Date(failed + offset).toISOString();
    const proof = {
      mutation: { status: 200, streamComplete: false, requestId: 1, transportError: "Grading request failed: net::ERR_ABORTED" },
      canonicalQueue: { total: 1, needsGrading: 0, items: [{ status: "Graded", activeGradeRoundStatus: "finalized", requiresStaffReview: false }] },
      visibleBadge: "0 to grade", releaseStatus: "Ready to release",
      events: [
        { event: "click:submit", at: at(-100) },
        { event: "request", at: at(-90), requestId: 1 },
        { event: "response", at: at(-80), requestId: 1, status: 200 },
        { event: "requestfailed", at: at(0), requestId: 1, error: "net::ERR_ABORTED" },
        ...await observer.collect(),
      ],
    };
    assert.equal(proof.events.length, 6);
    assert.equal(evaluateGradingTransport(proof).accepted, false);
    await observer.dispose();
  } finally { await page.close(); }
});
