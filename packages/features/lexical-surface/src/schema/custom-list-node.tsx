"use client";

import {
  ListNode,
  type ListType,
  type SerializedListNode,
} from "@lexical/list";
import type {
  EditorConfig,
  LexicalNode,
  NodeKey,
  SerializedLexicalNode,
} from "lexical";

const DEFAULT_MARKER_COLOR = "#3b82f6";
const CUSTOM_LIST_STYLE_ID = "lexical-surface-custom-list-styles";
const LIST_STYLE_TYPES = new Set([
  "decimal",
  "upper-alpha",
  "lower-alpha",
  "upper-roman",
  "lower-roman",
  "decimal-leading-zero",
  "disc",
  "circle",
  "square",
  "greek-upper",
  "circled",
  "arrow",
  "star",
]);

export interface SerializedCustomListNode extends SerializedListNode {
  listStyleType?: string;
  markerColor?: string;
}

export function normalizeListStyleType(
  value: unknown,
  listType: ListType,
): string {
  return typeof value === "string" && LIST_STYLE_TYPES.has(value)
    ? value
    : listType === "bullet"
      ? "disc"
      : "decimal";
}

export function normalizeMarkerColor(value: unknown): string {
  if (typeof value !== "string") return DEFAULT_MARKER_COLOR;
  const color = value.trim();
  return /^#(?:[\da-f]{3}|[\da-f]{4}|[\da-f]{6}|[\da-f]{8})$/i.test(color)
    ? color
    : DEFAULT_MARKER_COLOR;
}

function ensureCustomListStyles(): void {
  if (
    typeof document === "undefined" ||
    document.getElementById(CUSTOM_LIST_STYLE_ID)
  )
    return;

  const style = document.createElement("style");
  style.id = CUSTOM_LIST_STYLE_ID;
  style.textContent = `
    .lexical-editor [data-lexical-custom-list="true"] > li::marker {
      color: var(--lexical-list-marker-color);
    }
    .lexical-editor [data-list-style-type="greek-upper"],
    .lexical-editor [data-list-style-type="circled"],
    .lexical-editor [data-list-style-type="arrow"],
    .lexical-editor [data-list-style-type="star"] {
      list-style: none;
    }
    .lexical-editor [data-list-style-type="greek-upper"] { counter-reset: lexical-greek; }
    .lexical-editor [data-list-style-type="greek-upper"] > li::before {
      counter-increment: lexical-greek;
      content: counter(lexical-greek, upper-greek) ".";
    }
    .lexical-editor [data-list-style-type="circled"] { counter-reset: lexical-circled; }
    .lexical-editor [data-list-style-type="circled"] > li::before {
      counter-increment: lexical-circled;
      content: "(" counter(lexical-circled) ")";
    }
    .lexical-editor [data-list-style-type="arrow"] > li::before { content: "\u25b6"; }
    .lexical-editor [data-list-style-type="star"] > li::before { content: "\u2605"; }
    .lexical-editor [data-list-style-type="greek-upper"] > li::before,
    .lexical-editor [data-list-style-type="circled"] > li::before,
    .lexical-editor [data-list-style-type="arrow"] > li::before,
    .lexical-editor [data-list-style-type="star"] > li::before {
      color: var(--lexical-list-marker-color);
      display: inline-block;
      font-weight: bold;
      margin-right: 0.5rem;
    }
  `;
  document.head.appendChild(style);
}

export class CustomListNode extends ListNode {
  __listStyleType: string;
  __markerColor: string;

  static getType(): string {
    return "custom-list";
  }

  static clone(node: CustomListNode): CustomListNode {
    return new CustomListNode(
      node.getListType(),
      node.getStart(),
      node.__listStyleType,
      node.__markerColor,
      node.__key,
    );
  }

  constructor(
    listType: ListType,
    start: number,
    listStyleType?: string,
    markerColor?: string,
    key?: NodeKey,
  ) {
    super(listType, start, key);
    this.__listStyleType = normalizeListStyleType(listStyleType, listType);
    this.__markerColor = normalizeMarkerColor(markerColor);
  }

  getListStyleType(): string {
    return this.getLatest().__listStyleType;
  }

  setListStyleType(listStyleType: string): void {
    this.getWritable().__listStyleType = normalizeListStyleType(
      listStyleType,
      this.getListType(),
    );
  }

  getMarkerColor(): string {
    return this.getLatest().__markerColor;
  }

  setMarkerColor(markerColor: string): void {
    this.getWritable().__markerColor = normalizeMarkerColor(markerColor);
  }

  private applyStyles(element: HTMLElement): void {
    ensureCustomListStyles();
    const listStyleType = normalizeListStyleType(
      this.__listStyleType,
      this.getListType(),
    );
    element.dataset.lexicalCustomList = "true";
    element.dataset.listStyleType = listStyleType;
    element.style.setProperty(
      "--lexical-list-marker-color",
      normalizeMarkerColor(this.__markerColor),
    );
    element.style.listStylePosition = "inside";
    element.style.listStyleType = [
      "greek-upper",
      "circled",
      "arrow",
      "star",
    ].includes(listStyleType)
      ? "none"
      : listStyleType;
    element.style.paddingLeft = "1rem";
    element.style.marginBlock = "1rem";
  }

  createDOM(config: EditorConfig): HTMLElement {
    const element = super.createDOM(config);
    this.applyStyles(element);
    return element;
  }

  updateDOM(
    prevNode: CustomListNode,
    dom: HTMLElement,
    config: EditorConfig,
  ): boolean {
    const result = super.updateDOM(prevNode as this, dom, config);
    this.applyStyles(dom);
    return result;
  }

  static importJSON(
    serializedNode: SerializedLexicalNode & Record<string, unknown>,
  ): CustomListNode {
    const payload = serializedNode as Partial<SerializedCustomListNode>;
    const listType = (payload.listType ?? "number") as ListType;
    return new CustomListNode(
      listType,
      payload.start ?? 1,
      payload.listStyleType,
      payload.markerColor,
    );
  }

  exportJSON(): SerializedCustomListNode {
    return {
      ...super.exportJSON(),
      listStyleType: this.__listStyleType,
      markerColor: this.__markerColor,
      type: "custom-list",
    };
  }
}

export function $createCustomListNode(
  listType: ListType,
  start = 1,
  listStyleType = "decimal",
  markerColor = DEFAULT_MARKER_COLOR,
): CustomListNode {
  return new CustomListNode(listType, start, listStyleType, markerColor);
}

export function $isCustomListNode(
  node: LexicalNode | null | undefined,
): node is CustomListNode {
  return node instanceof CustomListNode;
}
