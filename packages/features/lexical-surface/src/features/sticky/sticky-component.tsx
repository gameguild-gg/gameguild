/**
 * StickyComponent — Renderer for StickyNode with:
 *   - Custom HEX color via DropdownColorPicker
 *   - Classic / Formal / Modern styles
 *   - Wide / Compact size toggle
 *   - Drag-to-reposition (relative offset from natural position)
 *   - readOnly awareness
 */
"use client";

import * as React from "react";
import { useCallback, useEffect, useRef, useState } from "react";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { $getNodeByKey } from "lexical";
import { cn } from "@game-guild/ui/lib/utils";
import {
  Pin,
  Trash2,
  FileText,
  Sparkles,
  Info,
  AlertTriangle,
  AlertCircle,
  CheckCircle2,
  Palette,
  GripVertical,
  Maximize2,
  Minimize2,
  Settings2,
  ChevronDown,
  Check,
} from "lucide-react";
import { StickyStyle, StickySize, $isStickyNode } from "./sticky-node";
import ColorPicker from "../../shared/ui/color-picker";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from "@game-guild/ui/components/dropdown-menu";

interface StickyComponentProps {
  text: string;
  color: string;
  style: StickyStyle;
  size: StickySize;
  xOffset: number;
  yOffset: number;
  nodeKey: string;
}

// ── Helpers ──────────────────────────────────────────────────────────────

function hexToRgb(hex: string) {
  const shorthandRegex = /^#?([a-f\d])([a-f\d])([a-f\d])$/i;
  const fullHex = hex.replace(
    shorthandRegex,
    (_m, r, g, b) => r + r + g + g + b + b,
  );
  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(fullHex);
  return result
    ? {
        r: parseInt(result[1]!, 16),
        g: parseInt(result[2]!, 16),
        b: parseInt(result[3]!, 16),
      }
    : { r: 254, g: 243, b: 199 };
}

function getContrastColor(hex: string) {
  const { r, g, b } = hexToRgb(hex);
  const yiq = (r * 299 + g * 587 + b * 114) / 1000;
  return yiq >= 128 ? "text-gray-950" : "text-gray-50";
}

function getRgba(hex: string, alpha: number) {
  const { r, g, b } = hexToRgb(hex);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

function getFormalIcon(hex: string) {
  const { r, g, b } = hexToRgb(hex);
  if (g > r && g > b) return CheckCircle2;
  if (b > r && b > g) return Info;
  if (r > g && r > b && g > 150) return AlertTriangle;
  return AlertCircle;
}

// ── Component ────────────────────────────────────────────────────────────

export function StickyComponent({
  text,
  color,
  style,
  size,
  xOffset,
  yOffset,
  nodeKey,
}: StickyComponentProps) {
  const [editor] = useLexicalComposerContext();
  const [value, setValue] = useState(text);
  const [isFocused, setIsFocused] = useState(false);
  const [isEditable, setIsEditable] = useState(() => editor.isEditable());
  const [isDragging, setIsDragging] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const wrapperRef = useRef<HTMLDivElement>(null);

  // ── Clamp position to keep the sticky within the editor area ──
  // Similar to how the table plugin uses getMaxTableWidth to keep
  // columns within the content-editable's boundaries.
  const clampToEditor = useCallback((): { x: number; y: number } | null => {
    const wrapper = wrapperRef.current;
    const rootElement = editor.getRootElement();
    if (!wrapper || !rootElement) return null;

    const rootRect = rootElement.getBoundingClientRect();
    const wrapperRect = wrapper.getBoundingClientRect();

    // Compute the natural position (where the wrapper would be at offset 0,0)
    const naturalLeft = wrapperRect.left - xOffset;
    const naturalRight = wrapperRect.right - xOffset;
    const naturalTop = wrapperRect.top - yOffset;
    const naturalBottom = wrapperRect.bottom - yOffset;

    // Min/max offsets that keep the entire sticky within the root element
    const minX = rootRect.left - naturalLeft;
    const maxX = rootRect.right - naturalRight;
    const minY = rootRect.top - naturalTop;
    const maxY = rootRect.bottom - naturalBottom;

    // If the sticky is wider/taller than the editor, clamp to left/top
    const safeMinX = Math.min(minX, maxX);
    const safeMaxX = Math.max(minX, maxX);
    const safeMinY = Math.min(minY, maxY);
    const safeMaxY = Math.max(minY, maxY);

    const clampedX = Math.max(safeMinX, Math.min(xOffset, safeMaxX));
    const clampedY = Math.max(safeMinY, Math.min(yOffset, safeMaxY));

    if (clampedX !== xOffset || clampedY !== yOffset) {
      return { x: clampedX, y: clampedY };
    }
    return null;
  }, [editor, xOffset, yOffset]);

  // Re-clamp whenever offset, size, or layout changes
  useEffect(() => {
    // Wait for the DOM to settle after size/offset changes
    const raf = requestAnimationFrame(() => {
      const clamped = clampToEditor();
      if (clamped) {
        editor.update(() => {
          const node = $getNodeByKey(nodeKey);
          if ($isStickyNode(node)) node.setPosition(clamped.x, clamped.y);
        });
      }
    });
    return () => cancelAnimationFrame(raf);
  }, [editor, nodeKey, clampToEditor, size]);

  // ── Autogrow ──
  const adjustHeight = useCallback(() => {
    const textarea = textareaRef.current;
    if (textarea) {
      textarea.style.height = "auto";
      textarea.style.height = `${textarea.scrollHeight}px`;
    }
  }, []);

  useEffect(() => {
    setValue(text);
  }, [text]);
  useEffect(() => {
    adjustHeight();
  }, [value, adjustHeight]);

  useEffect(() => {
    return editor.registerEditableListener((editable) =>
      setIsEditable(editable),
    );
  }, [editor]);

  // Auto-focus on newly created (empty) notes
  useEffect(() => {
    if (text === "" && isEditable && textareaRef.current) {
      const timer = setTimeout(() => {
        editor.getRootElement()?.blur();
        textareaRef.current?.focus();
      }, 50);
      return () => clearTimeout(timer);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ── Event helpers ──
  const stopLexicalPropagation = useCallback((e: React.SyntheticEvent) => {
    e.stopPropagation();
  }, []);

  const handleWrapperMouseDown = useCallback(
    (e: React.MouseEvent) => {
      if (!isEditable) return;
      const target = e.target as HTMLElement;
      if (target.tagName === "INPUT" || target.tagName === "TEXTAREA") return;
      if (
        target.closest("button") ||
        target.closest("[data-radix-popper-content-wrapper]")
      )
        return;
      // Don't steal focus when drag handle is being used
      if (target.closest("[data-sticky-drag]")) return;
      editor.getRootElement()?.blur();
      requestAnimationFrame(() => {
        textareaRef.current?.focus();
      });
    },
    [editor, isEditable],
  );

  // ── Handlers ──
  const handleTextChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    e.stopPropagation();
    const val = e.target.value;
    setValue(val);
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if ($isStickyNode(node)) node.setText(val);
    });
  };

  const handleColorChange = (newColor: string) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if ($isStickyNode(node)) node.setColor(newColor);
    });
    requestAnimationFrame(() => {
      textareaRef.current?.focus();
    });
  };

  const handleStyleChange = (newStyle: StickyStyle) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if ($isStickyNode(node)) node.setStyle(newStyle);
    });
    requestAnimationFrame(() => {
      textareaRef.current?.focus();
    });
  };

  const handleSizeToggle = () => {
    const newSize: StickySize = size === "wide" ? "compact" : "wide";
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if ($isStickyNode(node)) node.setSize(newSize);
    });
    requestAnimationFrame(() => {
      textareaRef.current?.focus();
    });
  };

  const handleDelete = () => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node) node.remove();
    });
  };

  // ── Drag-to-reposition (GPU-accelerated, no transitions, bounded) ──
  const handleDragStart = useCallback(
    (e: React.MouseEvent) => {
      if (!isEditable) return;
      e.preventDefault();
      e.stopPropagation();
      setIsDragging(true);

      const startX = e.clientX;
      const startY = e.clientY;
      const startOffsetX = xOffset;
      const startOffsetY = yOffset;
      const wrapper = wrapperRef.current;
      const rootElement = editor.getRootElement();

      // Compute bounds once at drag start for consistent real-time clamping
      let minOX = -Infinity;
      let maxOX = Infinity;
      let minOY = -Infinity;
      let maxOY = Infinity;

      if (wrapper && rootElement) {
        const rootRect = rootElement.getBoundingClientRect();
        const wrapperRect = wrapper.getBoundingClientRect();

        // Natural position = where the wrapper sits at offset (0, 0)
        const naturalLeft = wrapperRect.left - startOffsetX;
        const naturalRight = wrapperRect.right - startOffsetX;
        const naturalTop = wrapperRect.top - startOffsetY;
        const naturalBottom = wrapperRect.bottom - startOffsetY;

        minOX = rootRect.left - naturalLeft;
        maxOX = rootRect.right - naturalRight;
        minOY = rootRect.top - naturalTop;
        maxOY = rootRect.bottom - naturalBottom;

        if (maxOX < minOX) maxOX = minOX;
        if (maxOY < minOY) maxOY = minOY;
      }

      // GPU-accelerate and kill transitions for 60 fps drag
      if (wrapper) {
        wrapper.style.willChange = "transform";
        wrapper.style.transition = "none";
      }

      const onMouseMove = (ev: MouseEvent) => {
        if (!wrapper) return;
        const rawX = startOffsetX + (ev.clientX - startX);
        const rawY = startOffsetY + (ev.clientY - startY);
        const cx = Math.max(minOX, Math.min(rawX, maxOX));
        const cy = Math.max(minOY, Math.min(rawY, maxOY));
        wrapper.style.transform = `translate(${cx}px, ${cy}px)`;
      };

      const onMouseUp = (ev: MouseEvent) => {
        const rawX = startOffsetX + (ev.clientX - startX);
        const rawY = startOffsetY + (ev.clientY - startY);
        const finalX = Math.max(minOX, Math.min(rawX, maxOX));
        const finalY = Math.max(minOY, Math.min(rawY, maxOY));

        document.removeEventListener("mousemove", onMouseMove);
        document.removeEventListener("mouseup", onMouseUp);

        if (wrapper) {
          wrapper.style.willChange = "";
          wrapper.style.transition = "";
        }
        setIsDragging(false);
        editor.update(() => {
          const node = $getNodeByKey(nodeKey);
          if ($isStickyNode(node)) node.setPosition(finalX, finalY);
        });
      };

      document.addEventListener("mousemove", onMouseMove);
      document.addEventListener("mouseup", onMouseUp);
    },
    [editor, isEditable, nodeKey, xOffset, yOffset],
  );

  // Reset position on double-click the handle
  const handleResetPosition = useCallback(() => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if ($isStickyNode(node)) node.setPosition(0, 0);
    });
  }, [editor, nodeKey]);

  // ── Visuals ──
  const getRotationClass = () => {
    if (style !== "classic") return "";
    const num = parseInt(nodeKey, 10);
    if (isNaN(num)) return "rotate-1";
    return num % 2 === 0 ? "rotate-1" : "-rotate-1";
  };

  const getStyleProps = (): React.CSSProperties => {
    const base: React.CSSProperties = {
      transform: `translate(${xOffset}px, ${yOffset}px)`,
      // Re-enable pointer events on the card itself (wrapper is none)
      pointerEvents: "auto",
      position: "relative",
      zIndex: 10,
    };
    switch (style) {
      case "formal":
        return {
          ...base,
          backgroundColor: getRgba(color, 0.08),
          borderLeftColor: color,
          borderLeftWidth: "4px",
        };
      case "modern":
        return {
          ...base,
          borderTopColor: color,
          borderTopWidth: "4px",
        };
      case "classic":
      default:
        return {
          ...base,
          backgroundColor: color,
          borderColor: getRgba(color, 0.4),
        };
    }
  };

  const STYLES_LIST: {
    id: StickyStyle;
    label: string;
    Icon: React.ComponentType<{ className?: string }>;
  }[] = [
    { id: "classic", label: "Classic", Icon: Pin },
    { id: "formal", label: "Formal", Icon: FileText },
    { id: "modern", label: "Modern", Icon: Sparkles },
  ];

  const FormalIcon = getFormalIcon(color);

  return (
    <div
      ref={wrapperRef}
      className={cn(
        "group p-5 border",
        // Only animate when NOT dragging — transitions kill drag fluidity
        !isDragging && "transition-shadow duration-150",
        size === "wide" ? "w-full max-w-lg mx-auto" : "w-full max-w-[16rem]",
        style === "formal" &&
          "border-y-gray-200 border-r-gray-200 dark:border-y-gray-800 dark:border-r-gray-800 rounded-r-md shadow-sm",
        style === "modern" &&
          "bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-800 shadow-sm rounded-xl",
        style === "classic" && "rounded-lg shadow-md",
        getRotationClass(),
        isDragging && "opacity-80 shadow-2xl cursor-grabbing select-none",
        isEditable &&
          !isDragging &&
          (isFocused ? "shadow-lg" : "hover:shadow-lg"),
      )}
      style={getStyleProps()}
      onFocus={() => isEditable && setIsFocused(true)}
      onBlur={(e) => {
        if (!e.currentTarget.contains(e.relatedTarget)) setIsFocused(false);
      }}
      onMouseDown={handleWrapperMouseDown}
      onKeyDown={stopLexicalPropagation}
      onKeyUp={stopLexicalPropagation}
      onInput={stopLexicalPropagation}
      onBeforeInput={stopLexicalPropagation}
      onCopy={stopLexicalPropagation}
      onCut={stopLexicalPropagation}
      onPaste={stopLexicalPropagation}
      tabIndex={isEditable ? 0 : -1}
    >
      {/* ── Drag Handle (top-left) ── */}
      {isEditable && (
        <div
          data-sticky-drag
          onMouseDown={handleDragStart}
          onDoubleClick={handleResetPosition}
          className={cn(
            "absolute top-1.5 left-1.5 cursor-grab active:cursor-grabbing p-0.5 rounded opacity-0 group-hover:opacity-60 group-focus-within:opacity-60 hover:!opacity-100 transition-opacity",
            "text-black/40 dark:text-white/40 hover:text-black/70 dark:hover:text-white/70",
          )}
          title="Drag to reposition · Double-click to reset"
        >
          <GripVertical className="w-4 h-4" />
        </div>
      )}

      {/* ── Action Menu (top-right floating) ── */}
      {isEditable && (
        <div className="absolute top-2 right-2 z-10 opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 transition-opacity">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                aria-label="Sticky note settings"
                className={cn(
                  "inline-flex h-6 items-center justify-center gap-1 rounded px-1.5",
                  "border border-gray-300 dark:border-gray-700",
                  "bg-white/90 dark:bg-gray-800/90 text-gray-700 dark:text-gray-200",
                  "shadow-sm hover:bg-gray-100 dark:hover:bg-gray-700",
                  isDragging && "hidden", // Hide while dragging
                )}
              >
                <Settings2 className="h-3.5 w-3.5" />
                <ChevronDown className="h-3.5 w-3.5" />
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              align="start"
              className="w-48"
              onCloseAutoFocus={(e) => e.preventDefault()}
            >
              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  <Palette className="w-4 h-4 mr-2 text-gray-500" /> Style
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent>
                  {STYLES_LIST.map(({ id, label, Icon: StyleIcon }) => (
                    <DropdownMenuItem
                      key={id}
                      onSelect={(e) => {
                        e.preventDefault();
                        handleStyleChange(id);
                      }}
                    >
                      <StyleIcon className="w-4 h-4 mr-2" />
                      {label}
                      {style === id && <Check className="ml-auto w-4 h-4" />}
                    </DropdownMenuItem>
                  ))}
                </DropdownMenuSubContent>
              </DropdownMenuSub>

              <DropdownMenuItem
                onSelect={(e) => {
                  e.preventDefault();
                  handleSizeToggle();
                }}
              >
                {size === "wide" ? (
                  <Minimize2 className="w-4 h-4 mr-2 text-gray-500" />
                ) : (
                  <Maximize2 className="w-4 h-4 mr-2 text-gray-500" />
                )}
                {size === "wide" ? "Compact Size" : "Wide Size"}
              </DropdownMenuItem>

              <DropdownMenuSub>
                <DropdownMenuSubTrigger>
                  <span
                    className="w-4 h-4 mr-2 rounded-full border border-gray-300 dark:border-gray-600"
                    style={{ backgroundColor: color }}
                  />
                  Color
                </DropdownMenuSubTrigger>
                <DropdownMenuSubContent
                  className="p-3"
                  onFocusOutside={(e) => {
                    const t = (e as any).detail?.originalEvent?.target;
                    if (
                      t instanceof Element &&
                      t.closest('[contenteditable="true"]')
                    )
                      e.preventDefault();
                  }}
                >
                  <ColorPicker
                    color={color}
                    onChange={(nextColor) => {
                      if (typeof nextColor === "string")
                        handleColorChange(nextColor);
                    }}
                  />
                </DropdownMenuSubContent>
              </DropdownMenuSub>

              <DropdownMenuSeparator />
              <DropdownMenuItem
                onSelect={handleDelete}
                className="text-red-600 focus:text-red-600 focus:bg-red-50 dark:focus:bg-red-950/30"
              >
                <Trash2 className="mr-2 h-4 w-4" />
                Delete note
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      )}

      {/* Pin Icon (Classic Style Only) */}
      {style === "classic" && (
        <div className="absolute top-2 left-1/2 -translate-x-1/2 opacity-80 text-rose-500 pointer-events-none">
          <Pin
            className="w-5 h-5 fill-rose-500 drop-shadow-sm"
            style={{ color }}
          />
        </div>
      )}

      {/* Main content body */}
      <div
        className={cn("flex items-start gap-3", style === "classic" && "mt-3")}
      >
        {style === "formal" && (
          <div className="mt-0.5 shrink-0" style={{ color }}>
            <FormalIcon className="w-5 h-5" />
          </div>
        )}

        <textarea
          ref={textareaRef}
          value={value}
          onChange={handleTextChange}
          disabled={!isEditable}
          readOnly={!isEditable}
          placeholder={isEditable ? "Type something here..." : ""}
          className={cn(
            "w-full resize-none bg-transparent outline-none border-none p-0 font-sans text-base leading-relaxed placeholder-black/35 dark:placeholder-white/35",
            style === "classic"
              ? getContrastColor(color)
              : "text-gray-800 dark:text-gray-200",
            !isEditable && "cursor-default",
          )}
          rows={2}
        />
      </div>
    </div>
  );
}
