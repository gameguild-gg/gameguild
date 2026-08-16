import { describe, expect, it } from "vitest";
import { createAssetUri, parseAssetUri } from "../core/asset-uri";
import { RemoteAssetProviderRegistry } from "../providers/remote-provider-registry";
import { describeAssetRepositoryContract, describeRemoteAssetProviderContract } from "./contract-suites";
import { FakeRemoteAssetProvider } from "./fake-remote-provider";
import { MemoryAssetRepository } from "./memory-asset-repository";

describeAssetRepositoryContract("memory", () => new MemoryAssetRepository());
describeRemoteAssetProviderContract("fake", () => new FakeRemoteAssetProvider());

describe("RemoteAssetProviderRegistry", () => {
  it("routes records by their metadata location", () => {
    const registry = new RemoteAssetProviderRegistry();
    const provider = new FakeRemoteAssetProvider();
    const unregister = registry.register(provider);
    const uri = createAssetUri();
    expect(registry.forRecord({
      id: parseAssetUri(uri)!.id,
      uri,
      name: "one.txt",
      kind: "document",
      mimeType: "text/plain",
      size: 1,
      location: { type: "provider", providerKey: "fake" },
      availability: "remote",
      createdAt: "2026-01-01T00:00:00.000Z",
      updatedAt: "2026-01-01T00:00:00.000Z",
    })).toBe(provider);
    unregister();
    expect(registry.get("fake")).toBeUndefined();
  });
});
