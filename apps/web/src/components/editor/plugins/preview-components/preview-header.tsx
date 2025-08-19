"use client"

import type { SerializedHeaderNode } from "../../nodes/header-node"

export function PreviewHeader({ node }: { node: SerializedHeaderNode }) {
  if (!node?.data) {
    console.error("Invalid header node structure:", node)
    return null
  }

  const { text, level, style = "default" } = node.data

  const getStyleClass = () => {
    switch (style) {
      case "underlined":
        return "border-b-2 border-primary pb-1"
      case "bordered":
        return "border-2 border-primary p-2 rounded-md"
      case "gradient":
        return "bg-gradient-to-r from-primary to-primary/30 text-primary-foreground p-2 rounded-md"
      case "accent":
        return "border-l-4 border-primary pl-2"
      default:
        return ""
    }
  }

  const styleClass = getStyleClass()

  switch (level) {
    case 1:
      return <h1 className={`text-4xl font-bold my-4 ${styleClass}`}>{text}</h1>
    case 2:
      return <h2 className={`text-3xl font-bold my-4 ${styleClass}`}>{text}</h2>
    case 3:
      return <h3 className={`text-2xl font-bold my-4 ${styleClass}`}>{text}</h3>
    case 4:
      return <h4 className={`text-xl font-bold my-4 ${styleClass}`}>{text}</h4>
    case 5:
      return <h5 className={`text-lg font-bold my-4 ${styleClass}`}>{text}</h5>
    case 6:
      return <h6 className={`text-base font-bold my-4 ${styleClass}`}>{text}</h6>
    default:
      return <h2 className={`text-3xl font-bold my-4 ${styleClass}`}>{text}</h2>
  }
}
