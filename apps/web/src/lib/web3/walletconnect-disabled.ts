import type { WalletConnectEthereumProviderLike } from "@game-guild/web3";

// Selected by the host's bundler configuration only when WalletConnect is off.
// No SDK import belongs here: ordinary sign-in must work without that package.
const disabledProvider = {
  init(): Promise<WalletConnectEthereumProviderLike> {
    return Promise.reject(new Error("WalletConnect is disabled for this deployment."));
  },
};

export default disabledProvider;
