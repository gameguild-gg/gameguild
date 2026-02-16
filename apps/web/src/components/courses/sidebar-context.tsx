'use client';

import { ReactNode, createContext, useCallback, useContext, useEffect, useRef, useState } from 'react';

// Custom hook to detect mobile screen size
function useIsMobile() {
  const [isMobile, setIsMobile] = useState(false);
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    setMounted(true);
    const checkIsMobile = () => {
      setIsMobile(window.innerWidth < 1024);
    };

    checkIsMobile();
    window.addEventListener('resize', checkIsMobile);

    return () => window.removeEventListener('resize', checkIsMobile);
  }, []);

  return { isMobile, mounted };
}

// --- sessionStorage helpers ---

function getStorageKey(courseSlug: string, suffix: string): string {
  return `course-sidebar:${courseSlug}:${suffix}`;
}

function loadExpandedIds(courseSlug: string): Set<string> {
  if (typeof window === 'undefined') return new Set();
  try {
    const raw = sessionStorage.getItem(getStorageKey(courseSlug, 'expanded'));
    if (raw) return new Set(JSON.parse(raw) as string[]);
  } catch { /* ignore */ }
  return new Set();
}

function saveExpandedIds(courseSlug: string, ids: Set<string>): void {
  if (typeof window === 'undefined') return;
  try {
    sessionStorage.setItem(getStorageKey(courseSlug, 'expanded'), JSON.stringify([...ids]));
  } catch { /* ignore */ }
}

function loadScrollTop(courseSlug: string): number {
  if (typeof window === 'undefined') return 0;
  try {
    const raw = sessionStorage.getItem(getStorageKey(courseSlug, 'scrollTop'));
    if (raw) return Number(raw);
  } catch { /* ignore */ }
  return 0;
}

function saveScrollTop(courseSlug: string, scrollTop: number): void {
  if (typeof window === 'undefined') return;
  try {
    sessionStorage.setItem(getStorageKey(courseSlug, 'scrollTop'), String(scrollTop));
  } catch { /* ignore */ }
}

interface SidebarContextType {
  isSidebarOpen: boolean;
  toggleSidebar: () => void;
  closeSidebar: () => void;
  openSidebar: () => void;
  isMobile: boolean;
  mounted: boolean;
  // Expanded node management
  expandedIds: Set<string>;
  toggleExpanded: (id: string) => void;
  expandIds: (ids: string[]) => void;
  // Scroll position management
  scrollRef: React.RefObject<HTMLDivElement | null>;
  restoreScroll: () => void;
}

const SidebarContext = createContext<SidebarContextType | undefined>(undefined);

interface SidebarProviderProps {
  children: ReactNode;
  courseSlug?: string;
}

export function SidebarProvider({ children, courseSlug = '' }: SidebarProviderProps) {
  const { isMobile, mounted } = useIsMobile();

  // Initialize sidebar state based on screen size to prevent layout shift
  // On desktop (>=1024px), sidebar is open by default
  // On mobile (<1024px), sidebar is closed by default
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);

  const toggleSidebar = () => setIsSidebarOpen(!isSidebarOpen);
  const closeSidebar = () => setIsSidebarOpen(false);
  const openSidebar = () => setIsSidebarOpen(true);

  // --- Expanded state, persisted in sessionStorage ---
  const [expandedIds, setExpandedIds] = useState<Set<string>>(() => loadExpandedIds(courseSlug));

  // Sync to storage whenever it changes
  useEffect(() => {
    saveExpandedIds(courseSlug, expandedIds);
  }, [expandedIds, courseSlug]);

  const toggleExpanded = useCallback((id: string) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  }, []);

  const expandIds = useCallback((ids: string[]) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      let changed = false;
      for (const id of ids) {
        if (!next.has(id)) {
          next.add(id);
          changed = true;
        }
      }
      return changed ? next : prev;
    });
  }, []);

  // --- Scroll position, persisted in sessionStorage ---
  const scrollRef = useRef<HTMLDivElement | null>(null);
  const scrollSaveTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Save scroll position on scroll (debounced)
  useEffect(() => {
    const el = scrollRef.current;
    if (!el || !courseSlug) return;

    const handleScroll = () => {
      if (scrollSaveTimer.current) clearTimeout(scrollSaveTimer.current);
      scrollSaveTimer.current = setTimeout(() => {
        saveScrollTop(courseSlug, el.scrollTop);
      }, 150);
    };

    el.addEventListener('scroll', handleScroll, { passive: true });
    return () => {
      el.removeEventListener('scroll', handleScroll);
      if (scrollSaveTimer.current) clearTimeout(scrollSaveTimer.current);
    };
  }, [courseSlug, mounted]);

  const restoreScroll = useCallback(() => {
    if (!courseSlug) return;
    const saved = loadScrollTop(courseSlug);
    if (saved && scrollRef.current) {
      // Use rAF to ensure DOM is ready
      requestAnimationFrame(() => {
        scrollRef.current?.scrollTo({ top: saved });
      });
    }
  }, [courseSlug]);

  return (
    <SidebarContext.Provider
      value={{
        isSidebarOpen, toggleSidebar, closeSidebar, openSidebar,
        isMobile, mounted,
        expandedIds, toggleExpanded, expandIds,
        scrollRef, restoreScroll,
      }}
    >
      {children}
    </SidebarContext.Provider>
  );
}

export function useSidebar() {
  const context = useContext(SidebarContext);
  if (context === undefined) {
    throw new Error('useSidebar must be used within a SidebarProvider');
  }
  return context;
}
