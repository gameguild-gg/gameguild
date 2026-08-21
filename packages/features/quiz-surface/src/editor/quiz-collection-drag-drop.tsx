"use client";

import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type DragEvent,
  type MutableRefObject,
} from "react";
import { ArrowDown } from "lucide-react";

interface IdentifiedItem {
  id: string;
}

interface UseQuizCollectionDragDropOptions<T extends IdentifiedItem> {
  items: T[];
  onChange: (items: T[]) => void;
  onDragStateChange?: (dragging: boolean) => void;
  scrollToIndexRef: MutableRefObject<number | null>;
}

export function QuizCollectionDragPreview({
  onDragOver,
  onDrop,
}: {
  onDragOver: (event: DragEvent) => void;
  onDrop: () => void;
}) {
  return (
    <div
      onDragOver={(event) => {
        event.preventDefault();
        event.dataTransfer.dropEffect = "move";
        onDragOver(event);
      }}
      onDrop={(event) => {
        event.preventDefault();
        onDrop();
      }}
      className="my-2 flex items-center justify-center gap-3 rounded-xl border-3 border-dashed border-blue-500 bg-blue-100/80 py-5 shadow-lg shadow-blue-200/40 transition-all duration-150 dark:border-blue-400 dark:bg-blue-950/50 dark:shadow-blue-900/40"
    >
      <ArrowDown className="h-6 w-6 animate-bounce text-blue-600 dark:text-blue-300" />
      <span className="text-base font-bold tracking-wide text-blue-600 uppercase dark:text-blue-300">
        Move here
      </span>
      <ArrowDown className="h-6 w-6 animate-bounce text-blue-600 dark:text-blue-300" />
    </div>
  );
}

export function useQuizCollectionDragDrop<T extends IdentifiedItem>({
  items,
  onChange,
  onDragStateChange,
  scrollToIndexRef,
}: UseQuizCollectionDragDropOptions<T>) {
  const [isDragging, setIsDragging] = useState(false);
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const [dropTargetIndex, setDropTargetIndex] = useState<number | null>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const autoScrollFrame = useRef<number | null>(null);
  const lastDragY = useRef(0);

  const handleDragStart = useCallback(
    (index: number) => {
      setDragIndex(index);
      requestAnimationFrame(() => {
        setIsDragging(true);
        onDragStateChange?.(true);
      });
    },
    [onDragStateChange],
  );

  const handleDragEnd = useCallback(() => {
    if (dragIndex !== null && dropTargetIndex !== null) {
      const fromIndex = dragIndex;
      let toIndex = dropTargetIndex;

      if (toIndex !== fromIndex && toIndex !== fromIndex + 1) {
        if (toIndex > fromIndex) toIndex -= 1;
        onChange(moveItem(items, fromIndex, toIndex));
        scrollToIndexRef.current = toIndex;
      }
    }

    setIsDragging(false);
    onDragStateChange?.(false);
    setDragIndex(null);
    setDropTargetIndex(null);
    if (autoScrollFrame.current !== null) {
      cancelAnimationFrame(autoScrollFrame.current);
    }
  }, [
    dragIndex,
    dropTargetIndex,
    items,
    onChange,
    onDragStateChange,
    scrollToIndexRef,
  ]);

  const handleContainerDragOver = useCallback((event: DragEvent) => {
    event.preventDefault();
    lastDragY.current = event.clientY;
  }, []);

  const handleContainerDragLeave = useCallback((event: DragEvent) => {
    if (!event.currentTarget.contains(event.relatedTarget as Node)) {
      setDropTargetIndex(null);
    }
  }, []);

  useEffect(() => {
    if (!isDragging) return;

    const scrollStep = () => {
      const pointerY = lastDragY.current;
      if (pointerY === 0) {
        autoScrollFrame.current = requestAnimationFrame(scrollStep);
        return;
      }

      const scrollZone = 150;
      const maxSpeed = 40;
      let scrollParent: HTMLElement | null =
        containerRef.current?.parentElement ?? null;

      while (scrollParent && scrollParent !== document.documentElement) {
        const { overflowY } = getComputedStyle(scrollParent);
        if (/(auto|scroll)/.test(overflowY)) break;
        scrollParent = scrollParent.parentElement;
      }
      if (!scrollParent) scrollParent = document.documentElement;

      const rect =
        scrollParent === document.documentElement
          ? { top: 0, bottom: window.innerHeight }
          : scrollParent.getBoundingClientRect();
      const distanceFromTop = pointerY - rect.top;
      const distanceFromBottom = rect.bottom - pointerY;

      if (distanceFromTop < scrollZone) {
        scrollParent.scrollTop -= Math.round(
          maxSpeed * (1 - distanceFromTop / scrollZone),
        );
      } else if (distanceFromBottom < scrollZone) {
        scrollParent.scrollTop += Math.round(
          maxSpeed * (1 - distanceFromBottom / scrollZone),
        );
      }

      autoScrollFrame.current = requestAnimationFrame(scrollStep);
    };

    autoScrollFrame.current = requestAnimationFrame(scrollStep);
    return () => {
      if (autoScrollFrame.current !== null) {
        cancelAnimationFrame(autoScrollFrame.current);
      }
    };
  }, [isDragging]);

  return {
    containerRef,
    dragIndex,
    dropTargetIndex,
    handleContainerDragLeave,
    handleContainerDragOver,
    handleDragEnd,
    handleDragStart,
    isDragging,
    setDropTargetIndex,
  };
}

function moveItem<T>(items: T[], fromIndex: number, toIndex: number): T[] {
  const next = [...items];
  const [moved] = next.splice(fromIndex, 1);
  if (moved === undefined) return items;
  next.splice(toIndex, 0, moved);
  return next;
}
