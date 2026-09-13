import { getDefaultBrowserAssetRepository } from "@game-guild/assets/browser";
import { ComposedAssetRepository, RemoteAssetProviderRegistry } from "@game-guild/assets/providers";
import { GameGuildRemoteAssetProvider } from "./game-guild-remote-asset-provider";

let repository: ComposedAssetRepository | undefined;

/** Shared browser cache plus the authenticated GameGuild asset backend. */
export function getLearningAssetRepository(): ComposedAssetRepository {
  if (repository) return repository;
  const providers = new RemoteAssetProviderRegistry();
  providers.register(new GameGuildRemoteAssetProvider());
  repository = new ComposedAssetRepository(getDefaultBrowserAssetRepository(), providers);
  return repository;
}
