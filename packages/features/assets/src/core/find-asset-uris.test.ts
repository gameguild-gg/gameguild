import { describe, expect, it } from "vitest";
import { createAssetUri } from "./asset-uri";
import { findAssetUris } from "./find-asset-uris";

describe("findAssetUris", () => {
  it("finds unique structured references without scanning ordinary text", () => {
    const local = createAssetUri("7776453f-1123-4f56-8abc-1234567890ab");
    const remote = createAssetUri("8776453f-1123-4f56-8abc-1234567890ab");
    const cyclic: Record<string, unknown> = {
      local,
      nested: [local, { remote }],
      prose: `do not extract ${remote} from a larger string`,
    };
    cyclic.self = cyclic;

    expect(findAssetUris(cyclic)).toEqual([local, remote]);
  });
});
