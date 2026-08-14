"use client";

import { useEffect, useState, useRef } from "react";
import { VegaLiteViewer } from "./vega-lite-viewer";
import type { VegaDataAttachment } from "../vega-lite-data";

const EMPTY_ATTACHMENTS: Record<string, VegaDataAttachment> = {};

interface ControlledVegaLiteViewerProps {
  spec: string;
  layout?: "square" | "rectangular";
  themeLight?: string;
  themeDark?: string;
  title?: string;
  caption?: string;
  size?: number;
  showControls?: boolean;
  allowFullscreen?: boolean;
  className?: string;
  updateTrigger: string | number; // When this changes, update the chart
  attachments?: Record<string, VegaDataAttachment>;
}

export function ControlledVegaLiteViewer({
  spec,
  layout = "rectangular",
  themeLight = "default",
  themeDark = "dark",
  title,
  caption,
  size = 100,
  showControls = true,
  allowFullscreen = true,
  className = "",
  updateTrigger,
  attachments = EMPTY_ATTACHMENTS,
}: ControlledVegaLiteViewerProps) {
  const [currentSpec, setCurrentSpec] = useState(spec);
  const [currentLayout, setCurrentLayout] = useState(layout);
  const [currentThemeLight, setCurrentThemeLight] = useState(themeLight);
  const [currentThemeDark, setCurrentThemeDark] = useState(themeDark);
  const [currentTitle, setCurrentTitle] = useState(title);
  const [currentCaption, setCurrentCaption] = useState(caption);
  const [currentAttachments, setCurrentAttachments] = useState(attachments);
  const [containerHeight, setContainerHeight] = useState<number | null>(null);
  const previousUpdateTrigger = useRef(updateTrigger);
  const containerRef = useRef<HTMLDivElement>(null);

  // Capture initial container height
  useEffect(() => {
    const timer = setTimeout(() => {
      if (containerRef.current && containerHeight === null) {
        const height = containerRef.current.offsetHeight;
        if (height > 0) {
          setContainerHeight(height);
        }
      }
    }, 500);

    return () => clearTimeout(timer);
  }, [containerHeight]);

  // Update internal state only when updateTrigger changes
  useEffect(() => {
    if (updateTrigger !== previousUpdateTrigger.current) {
      // Preserve current container height during update
      if (containerRef.current && containerHeight === null) {
        setContainerHeight(containerRef.current.offsetHeight);
      }

      // Instant update - no delays or transitions
      setCurrentSpec(spec);
      setCurrentLayout(layout);
      setCurrentThemeLight(themeLight);
      setCurrentThemeDark(themeDark);
      setCurrentTitle(title);
      setCurrentCaption(caption);
      setCurrentAttachments(attachments);
      previousUpdateTrigger.current = updateTrigger;
    }
  }, [
    updateTrigger,
    spec,
    layout,
    themeLight,
    themeDark,
    title,
    caption,
    attachments,
    containerHeight,
  ]);

  return (
    <div
      ref={containerRef}
      className="relative"
      style={{
        height: containerHeight ? `${containerHeight}px` : "auto",
        minHeight: layout === "square" ? "500px" : "400px",
      }}
    >
      <VegaLiteViewer
        spec={currentSpec}
        layout={currentLayout}
        themeLight={currentThemeLight}
        themeDark={currentThemeDark}
        title={currentTitle}
        caption={currentCaption}
        size={size}
        showControls={showControls}
        allowFullscreen={allowFullscreen}
        className={className}
        attachments={currentAttachments}
      />
    </div>
  );
}
