import { createAssetUri } from "@game-guild/assets";
import { describe, expect, it, vi } from "vitest";
import { prepareAuthoringAssets } from "./prepare-authoring-assets";

const scope = { type: "ProgramContent", id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa" };

describe("prepareAuthoringAssets", () => {
  it("promotes local references, verifies portability, and reconciles the manifest", async () => {
    const uri = createAssetUri("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    const repository = {
      get: vi.fn().mockResolvedValue({ uri, location: { type: "local" } }),
      uploadLocalAsset: vi.fn().mockResolvedValue({ uri, location: { type: "provider" } }),
      checkPortability: vi.fn().mockResolvedValue({ portable: true, localOnly: [], unavailable: [] }),
      reconcileUsage: vi.fn().mockResolvedValue(undefined),
    };

    await prepareAuthoringAssets({ jsonBody: { root: { children: [{ src: uri }] } } }, repository, scope);

    expect(repository.uploadLocalAsset).toHaveBeenCalledWith(uri, "gameguild", { scope });
    expect(repository.reconcileUsage).toHaveBeenCalledWith(scope, [
      { uri, consumerId: "lesson-document", role: "embedded" },
    ]);
  });

  it("refuses to save a document with local-only or unavailable assets", async () => {
    const uri = createAssetUri("cccccccc-cccc-4ccc-8ccc-cccccccccccc");
    const repository = {
      get: vi.fn().mockResolvedValue({ uri, location: { type: "provider" } }),
      uploadLocalAsset: vi.fn(),
      checkPortability: vi.fn().mockResolvedValue({ portable: false, localOnly: [uri], unavailable: [] }),
      reconcileUsage: vi.fn(),
    };

    await expect(prepareAuthoringAssets({ body: uri }, repository, scope)).rejects.toThrow(
      "Upload every lesson asset before saving",
    );
    expect(repository.reconcileUsage).not.toHaveBeenCalled();
  });
});
