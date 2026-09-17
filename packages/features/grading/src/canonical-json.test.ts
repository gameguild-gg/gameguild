import { describe, expect, it } from "vitest";
import {
  canonicalizeJson,
  hashAssessmentAuthoringSource,
  hashAssessmentExecutionSnapshot,
} from "./index";

describe("canonical JSON and revision hashes", () => {
  it("is independent of object insertion order", () => {
    expect(canonicalizeJson({ z: 1, a: { y: 2, b: 3 } }))
      .toBe('{"a":{"b":3,"y":2},"z":1}');
  });

  it("keeps authoring identity independent from executable versions", async () => {
    const authoring = { schemaVersion: 1, contentType: "example", content: {}, grading: { schemaVersion: 2, items: {} }, policy: {} };
    const firstSourceHash = await hashAssessmentAuthoringSource(authoring);
    const secondSourceHash = await hashAssessmentAuthoringSource({ ...authoring });
    const firstSnapshotHash = await hashAssessmentExecutionSnapshot({ authoringSource: authoring, manifest: { version: "1" } });
    const secondSnapshotHash = await hashAssessmentExecutionSnapshot({ authoringSource: authoring, manifest: { version: "2" } });
    expect(firstSourceHash).toBe(secondSourceHash);
    expect(firstSnapshotHash).not.toBe(secondSnapshotHash);
  });
});
