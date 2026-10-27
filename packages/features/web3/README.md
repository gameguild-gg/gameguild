# @game-guild/web3

Wallet connection context, EIP-1193 adapters and the shared Sign-In with
Ethereum (SIWE) journey for GameGuild web clients.

Issues: #38 (React context), #39 (MetaMask), #17 (Trust Wallet), #18
(WalletConnect), #16 (Torus), under the umbrella of #291 (Web3 Wallet
Authentication System). The backend challenge/verify endpoints are owned by
#292 (`POST /v1/auth/web3/challenge`, `POST /v1/auth/web3:verify`).

## Supported provider scope

| Wallet       | Transport                                                   | Availability                                                            |
| ------------ | ----------------------------------------------------------- | ----------------------------------------------------------------------- |
| MetaMask     | Injected EIP-1193 provider (EIP-6963 discovery, legacy `window.ethereum` fallback incl. multi-wallet `providers[]`) | Button rendered whenever the extension is detected. No SDK dependency.  |
| Trust Wallet | Injected EIP-1193 provider (EIP-6963 rdns `com.trustwallet.app`, `window.trustwallet`, flagged `window.ethereum` incl. `providers[]`); mobile external browsers fall back to the universal deep link `https://link.trustwallet.com/open_url?coin_id=60&url=…` which reopens the page in the Trust in-app browser | Button rendered when the extension / in-app provider is detected, or on mobile browsers where the deep link can hand the page to the Trust app. No SDK dependency. |
| WalletConnect | WalletConnect v2 protocol through `@walletconnect/ethereum-provider` loaded via a lazy dynamic `import()` (pairing modal / `display_uri`) | Config-gated (`enabled: true` + `projectId`) and fails closed if the SDK is missing or the deployment has not opted in. Never part of the main bundle. |
| Torus        | `@toruslabs/torus-embed` loaded through a lazy dynamic `import()` | Config-gated (`enabled: true`) and fails closed if the SDK is missing. Never part of the main bundle. |

All adapters speak plain EIP-1193 (`eth_requestAccounts`, `eth_accounts`,
`eth_chainId`, `personal_sign`, `wallet_switchEthereumChain`,
`wallet_addEthereumChain`) — no ethers/web3.js dependency. Compose them with
`createWalletAdapters` (the wallet-adapter registry): MetaMask and Trust
Wallet register by default (detection-driven), WalletConnect and Torus only
when the deployment passes their config. Any future EIP-1193 wallet plugs in
by implementing the same `WalletAdapter` contract and registering it on the
provider; duplicate adapter ids never shadow an earlier registration.

### Trust Wallet environments (#17)

1. Desktop browser with the Trust extension — EIP-6963/injected detection,
   identical flow to MetaMask.
2. Trust in-app browser (mobile) — the same injected provider, so pages
   opened inside Trust behave like environment 1.
3. Mobile external browser — no provider can be injected; `connect()`
   navigates to Trust's universal deep link (`connection_failed` error with a
   hand-off message) and the user reconnects inside the app where
   environment 2 applies.

Connection failures (`wallet_not_found` without a provider, `user_rejected`
on prompt dismissal, `connection_failed` when no account is returned) and
signing failures (raw provider errors, surfaced by the context as
`sign_failed`/`user_rejected`) are covered by the adapter suite.

### WalletConnect runtime behavior and limits (#18)

- The SDK is an optional peer of `@game-guild/web3`. The default loader uses a
  static dynamic import that the host bundles normally; `loadProvider` remains
  available for hosts supplying their own integration.
- In the GameGuild Next application, leave
  `NEXT_PUBLIC_WALLETCONNECT_PROJECT_ID` unset to disable WalletConnect. Both
  Turbopack and Webpack then resolve the optional provider to a local disabled
  module, so `/sign-in` compiles even if the SDK is physically absent. The SDK is
  not required for password, MetaMask or Trust Wallet sign-in.
- To enable WalletConnect in an installation without the optional peer, run
  `pnpm --filter @game-guild/web3 add --save-optional @walletconnect/ethereum-provider@2.26.0`,
  set a real `NEXT_PUBLIC_WALLETCONNECT_PROJECT_ID` in `apps/web/.env.local`, and
  restart `pnpm run dev:fast` (or rebuild for production). With that setting,
  both bundlers use the real package; a missing SDK is a build error rather
  than a nonfunctional enabled button. Upstream SDK license terms apply.
- Hosted sign-in tests cover both bundlers with the SDK removed and disabled,
  and with the locked SDK installed and enabled. The enabled test clicks the
  real button and verifies SDK initialization reaches its provider transport;
  external HTTP/WebSocket requests are blocked. This proves module delivery,
  not a real relay pairing or signed wallet authentication; those remain
  separate acceptance work in #18.
- `isAvailable()` only checks the config gate (`enabled` + `projectId`, a
  public WalletConnect Cloud identifier — not a secret). The SDK import and
  the relay session happen exclusively inside `connect()`.
- Session lifecycle: pairing runs through `provider.connect()` (built-in QR
  modal, or the `display_uri` pairing URI via `getPairingUri()` for hosts
  rendering their own QR); `accountsChanged`/`chainChanged` forward account
  and network changes; `disconnect`, `session_delete` (wallet/user deleted
  the session) and `session_expire` (TTL elapsed) all reset the adapter so
  the next connect pairs a fresh session; `disconnect()` deletes the relay
  session and notifies the wallet.

Torus runtime behavior and limits:

- The embed SDK is an **optional** runtime dependency of the consuming app.
  When it is not installed (or the chunk fails to load) the adapter reports
  unavailable and the Torus button is hidden — it never breaks the bundle.
- The SDK is LGPL-3.0; embedding it in the (proprietary) GameGuild web app is
  acceptable for unmodified dynamic use, but any fork/patch of the SDK would
  need license review. Bundle impact is isolated to a lazy chunk
  (~1 MB minified across the embed + its iframe assets).
- Signature rejection (`login()`/`personal_sign` dismissed), disconnection
  (`disconnect` event or `accountsChanged: []`) and account/network changes
  (`accountsChanged`/`chainChanged`) are forwarded through the shared state
  machine for every wallet.

## Usage

```tsx
import { Web3Provider, createWalletAdapters } from '@game-guild/web3';

<Web3Provider
  adapters={createWalletAdapters({
    walletConnect: {
      enabled: Boolean(process.env.NEXT_PUBLIC_WALLETCONNECT_PROJECT_ID),
      projectId: process.env.NEXT_PUBLIC_WALLETCONNECT_PROJECT_ID,
    },
    torus: {
      enabled: process.env.NEXT_PUBLIC_WALLET_TORUS_ENABLED === 'true',
    },
  })}
>
  <SignInButtons />
</Web3Provider>
```

The SIWE journey runs `challenge → personal_sign → verify` through an
injected `SiweTransport`, so the package stays framework-agnostic:

```ts
const { state, connect, authenticate } = useWeb3();
await connect('metamask');
await authenticate({
  getChallenge: async (request) => (await fetch('/api/auth/web3/challenge', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })).json(),
  verify: async (request) => { /* POST /v1/auth/web3:verify */ },
});
```

State machine (`Web3State.status`): `disconnected → connecting → connected →
signing → authenticated`, with `error` reachable from any user-facing step
and full cleanup on disconnect (wallet logout, listener teardown, state
reset). `accountsChanged` downgrades `authenticated` back to `connected`
because the session identity changed.
