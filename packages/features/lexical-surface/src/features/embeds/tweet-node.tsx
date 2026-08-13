/**
 * TweetNode — `DecoratorBlockNode` rendering an embedded X/Tweet via
 * Twitter widgets.js. Ported from `lexical-playground/src/nodes/TweetNode.tsx`.
 */
"use client";

import * as React from "react";
import { useCallback, useEffect, useRef, useState } from "react";
import { BlockWithAlignableContents } from "@lexical/react/LexicalBlockWithAlignableContents";
import {
  DecoratorBlockNode,
  type SerializedDecoratorBlockNode,
} from "@lexical/react/LexicalDecoratorBlockNode";
import type {
  DOMConversionMap,
  DOMConversionOutput,
  DOMExportOutput,
  EditorConfig,
  ElementFormatType,
  LexicalNode,
  NodeKey,
  Spread,
} from "lexical";

const WIDGET_SCRIPT_URL = "https://platform.twitter.com/widgets.js";

type TweetComponentProps = Readonly<{
  className: Readonly<{ base: string; focus: string }>;
  format: ElementFormatType | null;
  loadingComponent?: React.JSX.Element | string;
  nodeKey: NodeKey;
  onError?: (error: string) => void;
  onLoad?: () => void;
  tweetID: string;
}>;

let isTwitterScriptLoading = true;

function TweetComponent({
  className,
  format,
  loadingComponent,
  nodeKey,
  onError,
  onLoad,
  tweetID,
}: TweetComponentProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const previousTweetIDRef = useRef<string>("");
  const [isTweetLoading, setIsTweetLoading] = useState(false);

  const createTweet = useCallback(async () => {
    try {
      const twttr = (
        window as unknown as {
          twttr?: {
            widgets: {
              createTweet: (
                id: string,
                el: HTMLElement | null,
              ) => Promise<unknown>;
            };
          };
        }
      ).twttr;
      await twttr?.widgets.createTweet(tweetID, containerRef.current);
      setIsTweetLoading(false);
      isTwitterScriptLoading = false;
      onLoad?.();
    } catch (error) {
      onError?.(String(error));
    }
  }, [onError, onLoad, tweetID]);

  useEffect(() => {
    if (tweetID !== previousTweetIDRef.current) {
      setIsTweetLoading(true);
      if (isTwitterScriptLoading) {
        const script = document.createElement("script");
        script.src = WIDGET_SCRIPT_URL;
        script.async = true;
        document.body?.appendChild(script);
        script.onload = createTweet;
        if (onError) script.onerror = onError as OnErrorEventHandler;
      } else {
        createTweet().catch((err) => console.error(err));
      }
      previousTweetIDRef.current = tweetID;
    }
  }, [createTweet, onError, tweetID]);

  return (
    <BlockWithAlignableContents
      className={className}
      format={format}
      nodeKey={nodeKey}
    >
      {isTweetLoading ? loadingComponent : null}
      <div
        style={{ display: "inline-block", width: "550px" }}
        ref={containerRef}
      />
    </BlockWithAlignableContents>
  );
}

export type SerializedTweetNode = Spread<
  { id: string },
  SerializedDecoratorBlockNode
>;

function $convertTweetElement(
  domNode: HTMLDivElement,
): DOMConversionOutput | null {
  const id = domNode.getAttribute("data-lexical-tweet-id");
  if (id) return { node: $createTweetNode(id) };
  return null;
}

export class TweetNode extends DecoratorBlockNode {
  __id: string;

  static getType(): string {
    return "tweet";
  }

  static clone(node: TweetNode): TweetNode {
    return new TweetNode(node.__id, node.__format, node.__key);
  }

  static importJSON(serializedNode: SerializedTweetNode): TweetNode {
    return $createTweetNode(serializedNode.id).updateFromJSON(serializedNode);
  }

  exportJSON(): SerializedTweetNode {
    return { ...super.exportJSON(), id: this.getId() };
  }

  static importDOM(): DOMConversionMap<HTMLDivElement> | null {
    return {
      div: (domNode: HTMLDivElement) => {
        if (!domNode.hasAttribute("data-lexical-tweet-id")) return null;
        return { conversion: $convertTweetElement, priority: 2 };
      },
    };
  }

  exportDOM(): DOMExportOutput {
    const element = document.createElement("div");
    element.setAttribute("data-lexical-tweet-id", this.__id);
    element.append(document.createTextNode(this.getTextContent()));
    return { element };
  }

  constructor(id: string, format?: ElementFormatType, key?: NodeKey) {
    super(format, key);
    this.__id = id;
  }

  getId(): string {
    return this.getLatest().__id;
  }

  getTextContent(): string {
    return `https://x.com/i/web/status/${this.__id}`;
  }

  decorate(_editor: unknown, config: EditorConfig): React.JSX.Element {
    const embedBlockTheme =
      (config.theme as { embedBlock?: { base?: string; focus?: string } })
        .embedBlock ?? {};
    const className = {
      base: embedBlockTheme.base ?? "",
      focus: embedBlockTheme.focus ?? "",
    };
    return (
      <TweetComponent
        className={className}
        format={this.__format}
        loadingComponent="Loading..."
        nodeKey={this.getKey()}
        tweetID={this.__id}
      />
    );
  }
}

export function $createTweetNode(tweetID: string): TweetNode {
  return new TweetNode(tweetID);
}

export function $isTweetNode(
  node: TweetNode | LexicalNode | null | undefined,
): node is TweetNode {
  return node instanceof TweetNode;
}
