import { parseAssetUri, type AssetUri } from "../core/asset-uri";
import type { RemoteAssetProvider } from "./remote-asset-provider";

export class RemoteAssetProviderRegistry {
  private readonly providers = new Map<string, RemoteAssetProvider>();

  register(provider: RemoteAssetProvider): () => void {
    if (this.providers.has(provider.key)) {
      throw new Error(`Remote asset provider is already registered: ${provider.key}`);
    }
    this.providers.set(provider.key, provider);
    return () => {
      if (this.providers.get(provider.key) === provider) this.providers.delete(provider.key);
    };
  }

  get(key: string): RemoteAssetProvider | undefined {
    return this.providers.get(key);
  }

  list(): RemoteAssetProvider[] {
    return Array.from(this.providers.values());
  }

  forUri(uri: AssetUri): RemoteAssetProvider | undefined {
    const parsed = parseAssetUri(uri);
    return parsed?.source === "remote" ? this.providers.get(parsed.providerKey) : undefined;
  }
}
