# Sign-In with Ethereum

The Web3 challenge endpoint creates an EIP-4361 SIWE message. It binds the message to the configured application origin, the requested EIP-155 chain, a cryptographically random nonce, and a five-minute validity window. Verification parses the complete message, checks those values against the server-side challenge, recovers the signer using EIP-191, and consumes a valid nonce once.

## Configuration

Set `Authentication:Web3:Siwe:Origin` to the public web application's origin, including its scheme and optional port. The API reads this from configuration, never from a client-supplied host header. Production origins must use HTTPS. HTTP is accepted only for loopback development origins.

Set `Authentication:Web3:Siwe:AllowedChainIds` to the EIP-155 chain IDs that users may select. The default is Ethereum mainnet (`1`). For example:

```json
{
  "Authentication": {
    "Web3": {
      "Siwe": {
        "Origin": "https://gameguild.gg",
        "AllowedChainIds": [ "1", "11155111" ]
      }
    }
  }
}
```

The local Compose file uses `WEB_PUBLIC_URL` and defaults to `http://localhost:3000`. Coolify passes its required `WEB_PUBLIC_URL` to the API as the SIWE origin. Other deployments should provide the equivalent `Authentication__Web3__Siwe__Origin` environment variable and may override the chain list with indexed configuration keys such as `Authentication__Web3__Siwe__AllowedChainIds__0`.

## Verification boundary

This implementation supports externally owned Ethereum accounts using EIP-191 signatures. Smart-contract wallet verification through EIP-1271, persistent wallet-to-user linking, wallet-provider adapters, and distributed challenge storage remain separate Web3 authentication work tracked by the broader #291 issue.
