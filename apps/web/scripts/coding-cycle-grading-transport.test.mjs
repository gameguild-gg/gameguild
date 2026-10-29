import assert from "node:assert/strict";
import test from "node:test";
import { evaluateGradingTransport } from "./coding-cycle-grading-transport.mjs";

// Sanitized witness from run40905796681, artifact12921926638
// (sha256:b307619301bd5b6e1ded560693a285654c925b3d118669bacdb85000018d416a).
// Expectations describe the confirmed grade, not the remaining RSC stream.
const observed = {
  mutation: {
    status: 200,
    requestId: 1,
    streamComplete: false,
    transportError: "Grading request failed: net::ERR_ABORTED",
  },
  canonicalQueue: {
    total: 1,
    needsGrading: 0,
    items: [{ status: "Graded", activeGradeRoundStatus: "finalized", requiresStaffReview: false }],
  },
  visibleBadge: "0 to grade",
  releaseStatus: "Ready to releaseFinal result: 100 / 100",
  events: [
    { event: "click:submit", at: "2026-10-29T18:16:15.648Z" },
    { event: "request", at: "2026-10-29T18:16:15.716Z", requestId: 1 },
    { event: "response", at: "2026-10-29T18:16:30.591Z", requestId: 1, status: 200 },
    { event: "ui:zero-badge", at: "2026-10-29T18:16:33.335Z" },
    { event: "ui:final-result", at: "2026-10-29T18:16:33.335Z" },
    { event: "requestfailed", at: "2026-10-29T18:16:34.409Z", requestId: 1, error: "net::ERR_ABORTED" },
  ],
};

const evidence = () => structuredClone(observed);

test("a completed response retains the existing transport acceptance", () => {
  const proof = evidence();
  proof.mutation.streamComplete = true;
  proof.mutation.bodyBytes = 62760;
  proof.mutation.transportError = null;
  proof.events = proof.events.filter((event) => event.event !== "requestfailed");
  assert.equal(evaluateGradingTransport(proof).accepted, true);
});

test("a cancellation after acknowledged final grading does not invalidate that grade", () => {
  assert.equal(evaluateGradingTransport(evidence()).accepted, true);
});

test("a cancellation before the visible acknowledgement remains a failure", () => {
  const proof = evidence();
  proof.events.find((event) => event.event === "ui:final-result").at = "2026-10-29T18:16:35.000Z";
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("a missing canonical queue cannot justify an aborted response", () => {
  const proof = evidence();
  proof.canonicalQueue = null;
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("remaining staff review keeps an aborted response failed", () => {
  const proof = evidence();
  proof.canonicalQueue.needsGrading = 1;
  proof.canonicalQueue.items[0].requiresStaffReview = true;
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("a different queue population cannot attest the fixture grade", () => {
  const proof = evidence();
  proof.canonicalQueue.total = 2;
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("an unfinished grade round keeps an aborted response failed", () => {
  const proof = evidence();
  proof.canonicalQueue.items[0].activeGradeRoundStatus = "awaiting-review";
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("a stale pending badge keeps an aborted response failed", () => {
  const proof = evidence();
  proof.visibleBadge = "1 to grade";
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("a missing final-result view keeps an aborted response failed", () => {
  const proof = evidence();
  proof.releaseStatus = "Grading in progress";
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("another request's cancellation cannot attest the observed mutation", () => {
  const proof = evidence();
  proof.events.find((event) => event.event === "requestfailed").requestId = 2;
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("a connection reset is not treated as the observed post-acknowledgement cancellation", () => {
  const proof = evidence();
  proof.mutation.transportError = "Grading request failed: net::ERR_CONNECTION_RESET";
  proof.events.find((event) => event.event === "requestfailed").error = "net::ERR_CONNECTION_RESET";
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("missing acknowledgement timing keeps an aborted response failed", () => {
  const proof = evidence();
  proof.events = proof.events.filter((event) => event.event !== "ui:zero-badge");
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("an HTTP error cannot be overridden by a settled-looking interface", () => {
  const proof = evidence();
  proof.mutation.status = 500;
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});

test("an empty body is not a completed normal response", () => {
  const proof = evidence();
  proof.mutation.streamComplete = true;
  proof.mutation.bodyBytes = 0;
  proof.mutation.transportError = null;
  assert.equal(evaluateGradingTransport(proof).accepted, false);
});
