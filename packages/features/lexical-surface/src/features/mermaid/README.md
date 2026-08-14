# Mermaid feature

- `lexical/`: persisted node, decorator component, and insertion plugin.
- `editor/`: feature editor, Monaco integration, validation, and template picker.
- `rendering/`: secure Mermaid rendering and document viewer.
- `theme/`: theme contracts, resolution, and dark-theme definitions.
- `templates/`: built-in diagram catalog and preview assets.
- `mermaid-data.ts`: data contract shared by the feature layers.
- `index.ts`: exports required by the Lexical surface schema and capabilities.

Keep Mermaid-specific behavior inside this feature. Add a root-level file only
when it is a cross-layer contract or part of the feature entry point.
