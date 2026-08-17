import { createAssetUri } from "@game-guild/assets";
import { describe, expect, it } from "vitest";
import { collectQuizAssetUris } from "./assets/collect-quiz-asset-uris";
import { QuizEntryType, createHotspotEntry } from "./questions/question-types";

describe("collectQuizAssetUris", () => {
  it("deduplicates attachments and hotspot image references", () => {
    const uri = createAssetUri("7776453f-1123-4f56-8abc-1234567890ab");
    const entry = createHotspotEntry();
    entry.imageAssetUri = uri;
    entry.attachments = {
      learnerVisible: [{ assetUri: uri, role: "question" }],
      authorOnly: [{ assetUri: uri, role: "source" }],
    };
    expect(entry.type).toBe(QuizEntryType.Hotspot);
    expect(collectQuizAssetUris(entry)).toEqual([uri]);
  });
});
