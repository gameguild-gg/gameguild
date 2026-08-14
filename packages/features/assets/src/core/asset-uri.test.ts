import { describe, expect, it } from "vitest";
import {
  createLocalAssetUri,
  createRemoteAssetUri,
  isAssetUri,
  parseAssetUri,
} from "./asset-uri";

describe("asset URI", () => {
  it("roundtrips local identities", () => {
    const uri = createLocalAssetUri("7776453f-1123-4f56-8abc-1234567890ab");
    expect(uri).toBe("asset://local/7776453f-1123-4f56-8abc-1234567890ab");
    expect(parseAssetUri(uri)).toEqual({
      source: "local",
      id: "7776453f-1123-4f56-8abc-1234567890ab",
    });
  });

  it("keeps remote ids opaque", () => {
    const uri = createRemoteAssetUri("dashboard", "folder/file 1");
    expect(parseAssetUri(uri)).toEqual({
      source: "remote",
      providerKey: "dashboard",
      id: "folder/file 1",
    });
  });

  it("rejects prototype SHA-1 references", () => {
    expect(isAssetUri("asset://ab12cd34")).toBe(false);
  });
});
