import { describe, expect, it } from "vitest";
import {
  createAssetUri,
  isAssetId,
  isAssetUri,
  parseAssetUri,
} from "./asset-uri";

describe("asset URI", () => {
  it("roundtrips source-neutral identities", () => {
    const id = "7776453f-1123-4f56-8abc-1234567890ab";
    const uri = createAssetUri(id);
    expect(uri).toBe(`asset://${id}`);
    expect(parseAssetUri(uri)).toEqual({ id });
    expect(isAssetId(id)).toBe(true);
  });

  it("generates a UUID when no identity is supplied", () => {
    expect(isAssetUri(createAssetUri())).toBe(true);
  });

  it("rejects prototype SHA-1 references", () => {
    expect(isAssetUri("asset://ab12cd34")).toBe(false);
    expect(isAssetUri("asset://local/7776453f-1123-4f56-8abc-1234567890ab")).toBe(false);
  });
});
