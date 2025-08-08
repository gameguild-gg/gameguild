"use client"

import { useState } from "react"
import type { SerializedPresentationNode } from "../../nodes/presentation-node"
import { SlidePlayer } from "../../ui/slide-player"

export function PreviewPresentation({ node }: { node: SerializedPresentationNode }) {
  const [currentSlideIndex, setCurrentSlideIndex] = useState(0)

  if (!node?.data) {
    console.error("Invalid presentation node structure:", node)
    return null
  }

  const { slides, title, theme, transitionEffect, autoAdvance, autoAdvanceDelay, autoAdvanceLoop, showControls } =
    node.data

  return (
    <div className="my-4">
      <SlidePlayer
        slides={slides}
        title={title}
        currentSlideIndex={currentSlideIndex}
        onSlideChange={setCurrentSlideIndex}
        autoAdvance={autoAdvance}
        autoAdvanceDelay={autoAdvanceDelay}
        autoAdvanceLoop={autoAdvanceLoop}
        transitionEffect={transitionEffect}
        showControls={showControls}
        showThumbnails={true}
        showHeader={true}
        showFullscreenButton={false}
        theme={theme}
        customThemeColor={node.data.customThemeColor}
        size="lg"
        isPresenting={false}
        isEditing={false}
      />
    </div>
  )
}
