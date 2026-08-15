import { describe, expect, it } from "vitest";
import { createRemoteAssetUri } from "../core/asset-uri";
import { RemoteAssetProviderRegistry } from "../providers/remote-provider-registry";
import { describeAssetRepositoryContract, describeRemoteAssetProviderContract } from "./contract-suites";
import { FakeRemoteAssetProvider } from "./fake-remote-provider";
import { MemoryAssetRepository } from "./memory-asset-repository";

describeAssetRepositoryContract("memory", () => new MemoryAssetRepository());
describeRemoteAssetProviderContract("fake", () => new FakeRemoteAssetProvider());

describe("RemoteAssetProviderRegistry", () => {
  it("routes opaque remote URIs by provider key", () => {
    const registry = new RemoteAssetProviderRegistry();
    const provider = new FakeRemoteAssetProvider();
    const unregister = registry.register(provider);
    expect(registry.forUri(createRemoteAssetUri("fake", "one"))).toBe(provider);
    unregister();
    expect(registry.get("fake")).toBeUndefined();
  });
});
