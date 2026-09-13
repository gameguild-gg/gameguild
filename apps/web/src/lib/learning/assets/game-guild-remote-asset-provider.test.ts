import { createAssetUri } from "@game-guild/assets";
import { afterEach, describe, expect, it, vi } from "vitest";
import { GameGuildRemoteAssetProvider } from "./game-guild-remote-asset-provider";

const scope = {
  type: "ProgramContent",
  id: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
};
const assetId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
const hash = "a".repeat(64);

function apiAsset() {
  return {
    id: assetId,
    displayName: "diagram.png",
    parentResourceType: scope.type,
    parentResourceId: scope.id,
    createdAt: "2026-09-12T12:00:00Z",
    updatedAt: "2026-09-12T12:01:00Z",
    content: {
      contentHash: hash,
      mimeType: "image/png",
      sizeBytes: 7,
      virusScanStatus: "Clean",
      moderationStatus: "Approved",
    },
  };
}

describe("GameGuildRemoteAssetProvider", () => {
  afterEach(() => vi.unstubAllGlobals());

  it("uploads into the learning-content scope while preserving the portable asset URI", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(null, { status: 404 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ assetReferenceId: assetId }), { status: 201 }))
      .mockResolvedValueOnce(new Response(JSON.stringify(apiAsset()), { status: 200 }));
    vi.stubGlobal("fetch", fetchMock);
    const provider = new GameGuildRemoteAssetProvider();
    const uri = createAssetUri(assetId);

    const [record] = await provider.upload(
      [{ id: assetId, uri, blob: new Blob(["diagram"], { type: "image/png" }), name: "diagram.png", mimeType: "image/png" }],
      { scope },
    );

    expect(record).toMatchObject({
      id: assetId,
      uri,
      scope,
      availability: "remote",
      contentHash: `sha256:${hash}`,
      location: { type: "provider", providerKey: "gameguild", providerAssetId: assetId },
    });
    const uploadUrl = new URL(String(fetchMock.mock.calls[1]?.[0]), "http://localhost");
    expect(uploadUrl.pathname).toBe("/api/assets");
    expect(uploadUrl.searchParams.get("referenceId")).toBe(assetId);
    expect(uploadUrl.searchParams.get("parentResourceType")).toBe(scope.type);
    expect(uploadUrl.searchParams.get("parentResourceId")).toBe(scope.id);
    expect(uploadUrl.searchParams.get("accessPolicy")).toBe("Private");
    expect(fetchMock.mock.calls[1]?.[1]).toMatchObject({ method: "POST" });
  });

  it("lists scoped assets and resolves them through the authenticated content proxy", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ items: [apiAsset()] }), { status: 200 }),
    ));
    const provider = new GameGuildRemoteAssetProvider();

    const page = await provider.list({ scope }, { scope });
    const resolved = await provider.resolveUrl(page.items[0]!, { scope });

    expect(page.items).toHaveLength(1);
    expect(new URL(resolved.url).pathname).toBe(`/api/assets/${assetId}/content`);
  });
});
