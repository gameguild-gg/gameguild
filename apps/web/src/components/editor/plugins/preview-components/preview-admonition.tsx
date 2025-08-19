import type { SerializedAdmonitionNode } from "../../nodes/admonition-node"
import { Admonition as UIAdmonition } from "../../extras/admonition"

export function PreviewAdmonition({ node }: { node: SerializedAdmonitionNode }) {
  if (!node?.data) {
    console.error("Invalid Admonition node structure:", node)
    return null
  }

  const { title, content, type } = node.data

  return <UIAdmonition title={title} content={content} type={type} />
}
