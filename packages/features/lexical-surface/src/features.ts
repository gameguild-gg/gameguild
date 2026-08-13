export type LexicalSurfaceFeatures = {
  /** Top toolbar (block format, font, color, alignment, ...). Default: true */
  toolbar?: boolean;
  /** Bubble toolbar over selected text. Default: true */
  floatingTextFormat?: boolean;
  /** Bubble link editor when cursor is on a LinkNode. Default: true */
  floatingLinkEditor?: boolean;
  /** Drag handle on the left margin of every block. Default: true */
  draggable?: boolean;
  /** Native playground slash menu. Default: true */
  picker?: boolean;
  /** Apply page-size/margin/orientation to the editable area. Default: true */
  pageLayout?: boolean;
  shortcuts?: boolean;
  equation?: boolean;
  excalidraw?: boolean;
  emoji?: boolean;
  autoEmbed?: boolean;
  contextMenu?: boolean;
  codeAction?: boolean;
  table?: boolean;
  layout?: boolean;
  collapsible?: boolean;
  sticky?: boolean;
  admonition?: boolean;
  button?: boolean;
  divider?: boolean;
  mermaid?: boolean;
  vegaLite?: boolean;
  media?: boolean;
  history?: boolean;
  list?: boolean;
  link?: boolean;
  checkList?: boolean;
  tabIndentation?: boolean;
};

const DEFAULT_FEATURES: Required<LexicalSurfaceFeatures> = {
  toolbar: true,
  floatingTextFormat: true,
  floatingLinkEditor: true,
  draggable: true,
  picker: true,
  pageLayout: true,
  shortcuts: true,
  equation: true,
  excalidraw: true,
  emoji: true,
  autoEmbed: true,
  contextMenu: true,
  codeAction: true,
  table: true,
  layout: true,
  collapsible: true,
  sticky: true,
  admonition: true,
  button: true,
  divider: true,
  mermaid: true,
  vegaLite: true,
  media: true,
  history: true,
  list: true,
  link: true,
  checkList: true,
  tabIndentation: true,
};

const READ_ONLY_INTERACTIVE_FEATURES: ReadonlyArray<
  keyof LexicalSurfaceFeatures
> = [
  "toolbar",
  "floatingTextFormat",
  "floatingLinkEditor",
  "draggable",
  "picker",
  "shortcuts",
  "equation",
  "excalidraw",
  "emoji",
  "autoEmbed",
  "contextMenu",
  "codeAction",
  "table",
  "layout",
  "collapsible",
  "sticky",
  "admonition",
  "button",
  "divider",
  "mermaid",
  "vegaLite",
  "media",
  "history",
  "list",
  "link",
  "checkList",
  "tabIndentation",
];

export function resolveLexicalSurfaceFeatures(
  features: LexicalSurfaceFeatures | undefined,
  readOnly: boolean,
): Required<LexicalSurfaceFeatures> {
  const resolved = { ...DEFAULT_FEATURES, ...features };

  if (readOnly) {
    for (const feature of READ_ONLY_INTERACTIVE_FEATURES) {
      resolved[feature] = false;
    }
  }

  return resolved;
}
