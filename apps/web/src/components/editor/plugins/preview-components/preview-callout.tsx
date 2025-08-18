import type { SerializedCalloutNode } from "../../nodes/callout-node"
import { Callout as UICallout } from "../../extras/callout"

export function PreviewCallout({ node }: { node: SerializedCalloutNode }) {
  if (!node?.data) {
    console.error("Invalid callout node structure:", node)
    return null
  }

  const { calloutTitle, content, type } = node.data

  return <UICallout calloutTitle={calloutTitle} content={content} type={type} />
}
