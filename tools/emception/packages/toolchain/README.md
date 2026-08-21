# @gameguild/emception-toolchain

Canonical, versioned browser toolchain artifacts for Emception.

This package owns:

- the schema-v2 manifest and build fingerprint;
- Brotli-compressed sysroot bundles and decompressor;
- generated Emscripten `.mjs` glue and matching `.wasm` modules;
- runtime ABI, patch-set version, tool versions, and per-pair import/export
  profiles.

Application code normally installs `@gameguild/emception-browser`, which pins
and validates this package. Direct installation is useful for self-hosting:

```bash
npm install @gameguild/emception-toolchain@4.2.0
```

Copy `node_modules/@gameguild/emception-toolchain/cdn/` to a static origin and
pass its `manifest.json` URL to `createEmception()`.

Artifacts are staged only from the canonical `build/cdn` release. The
`emception/cdn` export is a temporary compatibility copy produced from that
same release, not an independent artifact source.
