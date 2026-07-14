# BusyBox (Optional)

BusyBox provides a POSIX shell (`ash`) and coreutils for the browser terminal.
This is optional and is not required for the MVP.

## Strategy

**Option A (recommended): Custom minimal shell** — already implemented in
`orchestrator/shell.ts` (MiniShell with TypeScript built-ins).

**Option B: BusyBox with heavy patching** — requires:

- Replacing `fork`/`exec` with `system()`-like calls to the orchestrator
- Disabling job control, signal handling, `termios`
- Disabling all networking applets
- Patching `ash` to use single-process execution (`CONFIG_FEATURE_SH_NOFORK`)
- Estimated 4-8 weeks of patch development and maintenance

## Build (when Option B is pursued)

```bash
BUSYBOX_VERSION=1.37.0
curl -fSL "https://busybox.net/downloads/busybox-$BUSYBOX_VERSION.tar.bz2" | tar xj
cd busybox-$BUSYBOX_VERSION
for patch in ../patches/*.patch; do [ -f "$patch" ] && patch -p1 < "$patch"; done
# Apply browser-specific defconfig
cp ../defconfig .config
emcmake make -j$(nproc)
```

## Patches

Place `.patch` files in the `patches/` directory. They are applied in
alphabetical order during the build.
