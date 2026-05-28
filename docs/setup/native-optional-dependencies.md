# Native Optional Dependencies

This note documents a cross-platform npm issue where native optional dependencies are missing from `node_modules` or `package-lock.json` after dependency updates.

## Symptom

`npm install` may complete successfully, but `npm run dev` or package builds can fail later with errors like:

```text
Cannot find native binding
Cannot find module '@rolldown/binding-linux-x64-gnu'
Cannot find module '@parcel/watcher-linux-x64-glibc'
Cannot find module '@swc/core-linux-x64-gnu'
Cannot find module '../lightningcss.linux-x64-gnu.node'
```

## Cause

The repository uses npm workspaces and several packages that install native binaries through `optionalDependencies`. These binaries are platform-specific.

A `package-lock.json` generated on macOS can include macOS native packages while missing Linux or Windows entries. A lockfile generated on Linux can similarly miss Windows entries. In this state, npm may consider the install complete even though the current platform cannot load the native binary it needs at runtime.

Known packages affected by this pattern include:

- `@rolldown/binding-*`, used by Vite 8/Rolldown.
- `@parcel/watcher-*`, used by Next.js file watching.
- `@swc/core-*`, used by `next-intl` and SWC transforms.
- `lightningcss-*`, used by CSS/font processing.
- `@tailwindcss/oxide-*`, used by Tailwind CSS 4.

## Windows Risk

When validating on Windows, expect equivalent package names such as:

- `@rolldown/binding-win32-x64-msvc`
- `@parcel/watcher-win32-x64`
- `@swc/core-win32-x64-msvc`
- `lightningcss-win32-x64-msvc`
- `@tailwindcss/oxide-win32-x64-msvc`

Use the `arm64` variants on ARM Windows.

## Prevention

1. Do not install with `--no-optional` or `--omit=optional`.
2. Run installs from the repository root with `npm install --include=optional`.
3. Commit the updated `package-lock.json` after adding or updating packages that introduce native optional dependencies.
4. Validate on every target platform before merging dependency changes.

Useful validation commands:

```bash
npm install --include=optional
npm run build --workspace=packages/dotnet-wasm
npm run dev --workspace=apps/web
```

## Recovery

If a clone is already broken, remove only local install artifacts and reinstall from the committed lockfile. Do not delete `package-lock.json` unless intentionally regenerating it for a dependency update.

Linux/macOS:

```bash
rm -rf node_modules apps/web/node_modules packages/dotnet-wasm/node_modules
npm install --include=optional
```

PowerShell:

```powershell
Remove-Item -Recurse -Force node_modules, apps/web/node_modules, packages/dotnet-wasm/node_modules -ErrorAction SilentlyContinue
npm install --include=optional
```

If the same error persists after reinstalling, inspect the missing package name in the stack trace and verify that the corresponding platform-specific optional dependency exists in `package-lock.json`.