# Vega-Lite feature

- `lexical/`: persisted node, decorator component, and insertion plugin.
- `editor/`: feature editor, Monaco integration, validation, export, datasets,
  and template picker UI.
- `rendering/`: Vega view lifecycle and document viewers.
- `data/`: CSV and inline dataset resolution.
- `theme/`: theme contracts, overrides, and customization notes.
- `templates/`: built-in visualization catalog and preview assets.
- `vega-lite-data.ts`: data contract shared by the feature layers.
- `index.ts`: exports required by the Lexical surface schema and capabilities.

Keep Vega-Lite-specific behavior inside this feature. Add a root-level file
only when it is a cross-layer contract or part of the feature entry point.
