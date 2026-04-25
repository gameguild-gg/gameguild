# @emception/ide

Reactive `<Ide>` React 19 component + `<emception-ide>` custom-element wrapper for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception). Promoted from the legacy `@gameguild/emception-ui` package.

## Status

**Phase 0 alpha — skeleton only.** The reactive rewrite (Phase 8) is not yet started; today this package re-exports the pre-overhaul UI shell so existing demos keep working. Production embeds should consume `@emception/webcomponent` (`<emception-run>`) or `@emception/react` (`<EmceptionRun>` + `useEmception`) directly until Phase 8 ships.

## Roadmap (Phase 8)

The reactive `<Ide>` component will compose smaller building blocks already shipped in `@emception/webcomponent` / `@emception/react`:

| Panel          | Source                                                                  |
| -------------- | ----------------------------------------------------------------------- |
| Editor         | Monaco wrapper (TBD — current code uses CodeMirror)                     |
| Terminal       | `@emception/xterm` + `<emception-run>` slot                             |
| Canvas         | OffscreenCanvas helper from `@emception/browser`                        |
| File explorer  | New — driven by `@emception/core`'s `WorkspaceManager`                  |
| Tabs / docking | Light DOM panels, all toggleable via props                              |

Until the rewrite lands, see:

- `packages/react/examples/basic/Demo.tsx` for the minimal embed pattern.
- `packages/webcomponent/examples/html/index.html` for the framework-free embed.
- `tools/emception/docs/dx-overhaul-plan.md` §8 for the IDE migration plan.
