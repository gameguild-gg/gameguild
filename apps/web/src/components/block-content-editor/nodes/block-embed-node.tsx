/**
 * Lexical decorator node that hosts an embedded `Block` envelope.
 *
 * One node class handles every embeddable block type. The actual rendering
 * is delegated to the surface-agnostic `<BlockEmbedView>` (see `../embed/`).
 *
 * Surface bridge: this file is the only place that knows how to translate
 * the user's actions (Editar / Remover from the view) into Lexical
 * `editor.update(...)` calls. Everything else stays in `embed/`.
 */

import { DecoratorNode, type LexicalNode, type NodeKey, type SerializedLexicalNode, type Spread } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $getNodeByKey } from "lexical"
import type { JSX } from "react"

import { BlockEmbedView } from "../embed/block-embed-view"
import type { EmbeddableBlock, EmbeddableBlockData } from "../embed/types"

// ---------------------------------------------------------------------------
// Serialized shape
// ---------------------------------------------------------------------------

export type SerializedBlockEmbedNode = Spread<
  {
    type: "block-embed"
    version: 1
    block: EmbeddableBlock
  },
  SerializedLexicalNode
>

// ---------------------------------------------------------------------------
// Node class
// ---------------------------------------------------------------------------

export class BlockEmbedNode extends DecoratorNode<JSX.Element> {
  __block: EmbeddableBlock

  static getType(): string {
    return "block-embed"
  }

  static clone(node: BlockEmbedNode): BlockEmbedNode {
    return new BlockEmbedNode(node.__block, node.__key)
  }

  constructor(block: EmbeddableBlock, key?: NodeKey) {
    super(key)
    this.__block = block
  }

  // -------------------------------------------------------------------------
  // DOM — `display: contents` keeps the decorator from breaking parent
  // paragraph layout (technique borrowed from the legacy RichTextNode).
  // -------------------------------------------------------------------------

  createDOM(): HTMLElement {
    const div = document.createElement("div")
    div.style.display = "contents"
    return div
  }

  updateDOM(): false {
    return false
  }

  // -------------------------------------------------------------------------
  // Serialization
  // -------------------------------------------------------------------------

  static importJSON(serialized: SerializedBlockEmbedNode): BlockEmbedNode {
    return new BlockEmbedNode(serialized.block)
  }

  exportJSON(): SerializedBlockEmbedNode {
    return {
      type: "block-embed",
      version: 1,
      block: this.__block,
    }
  }

  // -------------------------------------------------------------------------
  // Mutators
  // -------------------------------------------------------------------------

  setBlockData(data: EmbeddableBlockData): void {
    const self = this.getWritable()
    self.__block = { ...self.__block, data } as EmbeddableBlock
  }

  getBlock(): EmbeddableBlock {
    return this.__block
  }

  // -------------------------------------------------------------------------
  // Lexical structure flags
  // -------------------------------------------------------------------------

  isInline(): false {
    return false
  }

  isKeyboardSelectable(): true {
    return true
  }

  // -------------------------------------------------------------------------
  // React rendering
  // -------------------------------------------------------------------------

  decorate(): JSX.Element {
    return <BlockEmbedLexicalShell nodeKey={this.getKey()} />
  }
}

// ---------------------------------------------------------------------------
// React shell — bridges Lexical mutations to the surface-agnostic view.
// ---------------------------------------------------------------------------

function BlockEmbedLexicalShell({ nodeKey }: { nodeKey: NodeKey }) {
  const [editor] = useLexicalComposerContext()

  // Read the live block from the editor state on every render. `decorate()`
  // is called inside Lexical's reconciliation, so this stays in sync.
  let block: EmbeddableBlock | null = null
  editor.getEditorState().read(() => {
    const node = $getNodeByKey(nodeKey)
    if ($isBlockEmbedNode(node)) block = node.getBlock()
  })

  if (!block) return null

  const editable = editor.isEditable()

  const onChange = (data: EmbeddableBlockData) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isBlockEmbedNode(node)) node.setBlockData(data)
    })
  }

  const onRemove = () => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isBlockEmbedNode(node)) node.remove()
    })
  }

  return (
    <BlockEmbedView
      block={block}
      editable={editable}
      onChange={onChange}
      onRemove={onRemove}
    />
  )
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

export function $createBlockEmbedNode(block: EmbeddableBlock): BlockEmbedNode {
  return new BlockEmbedNode(block)
}

export function $isBlockEmbedNode(
  node: LexicalNode | null | undefined,
): node is BlockEmbedNode {
  return node instanceof BlockEmbedNode
}
