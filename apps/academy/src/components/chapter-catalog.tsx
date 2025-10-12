'use client';
import React, { createContext, PropsWithChildren, useContext, useMemo, useReducer } from 'react';
import Image from 'next/image';
import type { Chapter } from '@/lib/courses/types';
import { ChevronFirst, ChevronLast, ChevronLeft, ChevronRight, Grid, Play, X } from 'lucide-react';
import { Button } from '@gameguild/ui/components/button';
import { Progress } from '@gameguild/ui/components/progress';

// ---------- types ----------
export interface ChapterCatalogProps {
  chapters: Chapter[];
}

export const ViewMode = {
  HIGHLIGHT: 'HIGHLIGHT',
  GRID: 'GRID',
} as const;

type ViewModeType = (typeof ViewMode)[keyof typeof ViewMode];

interface State {
  chapters: Chapter[];
  activeIndex: number; // index of the highlighted chapter
  viewMode: ViewModeType; // view mode state
}

export const ChapterCatalogActionType = {
  SET_ACTIVE_INDEX: 'SET_ACTIVE_INDEX',
  NEXT: 'NEXT',
  PREVIOUS: 'PREV',
  SET_CHAPTERS: 'SET_CHAPTERS',
  TOGGLE_VIEW_MODE: 'TOGGLE_VIEW_MODE',
} as const;

type ChapterCatalogActionKey = keyof typeof ChapterCatalogActionType;

type ActionMap = {
  [ChapterCatalogActionType.SET_ACTIVE_INDEX]: { index: number };
  [ChapterCatalogActionType.NEXT]: object;
  [ChapterCatalogActionType.PREVIOUS]: object;
  [ChapterCatalogActionType.SET_CHAPTERS]: { chapters: Chapter[] };
  [ChapterCatalogActionType.TOGGLE_VIEW_MODE]: object;
};

type Action = {
  [K in ChapterCatalogActionKey]: { type: (typeof ChapterCatalogActionType)[K] } & ActionMap[(typeof ChapterCatalogActionType)[K]];
}[ChapterCatalogActionKey];

interface ChapterCatalogContextValue {
  chapters: Chapter[];
  activeIndex: number;
  activeChapter?: Chapter;
  viewMode: ViewModeType;
  setActiveIndex: (index: number) => void;
  next: () => void;
  previous: () => void;
  toggleViewMode: () => void;
}

// ---------- reducer ----------
const reducer = (state: State, action: Action): State => {
  switch (action.type) {
    case ChapterCatalogActionType.SET_CHAPTERS: {
      const nextChapters = action.chapters ?? [];
      const clampedIndex = nextChapters.length === 0 ? 0 : Math.min(state.activeIndex, nextChapters.length - 1);
      return { chapters: nextChapters, activeIndex: clampedIndex, viewMode: state.viewMode };
    }
    case ChapterCatalogActionType.SET_ACTIVE_INDEX: {
      const len = state.chapters.length;
      if (len === 0) return state;
      const index = Math.max(0, Math.min(action.index, len - 1));
      return { ...state, activeIndex: index };
    }
    case ChapterCatalogActionType.NEXT: {
      const len = state.chapters.length;
      if (len === 0) return state;
      return { ...state, activeIndex: (state.activeIndex + 1) % len };
    }
    case ChapterCatalogActionType.PREVIOUS: {
      const len = state.chapters.length;
      if (len === 0) return state;
      return { ...state, activeIndex: (state.activeIndex - 1 + len) % len };
    }
    case ChapterCatalogActionType.TOGGLE_VIEW_MODE: {
      const nextViewMode = state.viewMode === ViewMode.GRID ? ViewMode.HIGHLIGHT : ViewMode.GRID;
      return { ...state, viewMode: nextViewMode };
    }
    default:
      return state;
  }
};

// ---------- context ----------
const ChapterCatalogContext = createContext<ChapterCatalogContextValue | undefined>(undefined);

const ChapterCatalogProvider = ({ chapters, children }: { chapters: Chapter[]; children: React.ReactNode }) => {
  const [state, dispatch] = useReducer(reducer, { chapters, activeIndex: 0, viewMode: ViewMode.HIGHLIGHT });

  // If the prop changes, sync into state (keeps reducer as source of truth)
  // Note: relying on useMemo with chapters in deps to avoid extra renders.
  const value = useMemo<ChapterCatalogContextValue>(() => {
    const length = state.chapters.length;
    const activeChapter = length > 0 ? state.chapters[state.activeIndex] : undefined;
    return {
      chapters: state.chapters,
      activeIndex: state.activeIndex,
      activeChapter,
      viewMode: state.viewMode,
      setActiveIndex: (index: number) => dispatch({ type: ChapterCatalogActionType.SET_ACTIVE_INDEX, index }),
      next: () => dispatch({ type: ChapterCatalogActionType.NEXT }),
      previous: () => dispatch({ type: ChapterCatalogActionType.PREVIOUS }),
      toggleViewMode: () => dispatch({ type: ChapterCatalogActionType.TOGGLE_VIEW_MODE }),
    };
  }, [state.chapters, state.activeIndex, state.viewMode]);

  // Keep reducer chapters in sync with incoming prop
  React.useEffect(() => {
    dispatch({ type: ChapterCatalogActionType.SET_CHAPTERS, chapters });
  }, [chapters]);

  return <ChapterCatalogContext.Provider value={value}>{children}</ChapterCatalogContext.Provider>;
};

export const useChapterCatalog = (): ChapterCatalogContextValue => {
  const context = useContext(ChapterCatalogContext);

  if (!context) throw new Error('useChapterCatalog must be used within a ChapterCatalogProvider inside a client component');

  return context;
};

export const useActiveChapter = (): Chapter | undefined => {
  try {
    const { activeChapter } = useChapterCatalog();

    return activeChapter;
  } catch {
    throw new Error('useActiveChapter must be used within a ChapterCatalogProvider inside a client component');
  }
};

export const ChapterCatalog = ({ chapters }: Readonly<ChapterCatalogProps>): React.JSX.Element => {
  return (
    <ChapterCatalogProvider chapters={chapters}>
      <ChapterCatalogInternal />
    </ChapterCatalogProvider>
  );
};

const HighlightedChapterBackground = (): React.JSX.Element => {
  const activeChapter = useActiveChapter();
  return (
    <div className="absolute inset-0 select-none">
      <Image
        className="object-cover"
        src={activeChapter?.coverImage || '/placeholder.svg?height=1280&width=1280'}
        alt={activeChapter?.title || 'Chapter'}
        fill
        draggable={false}
        priority
      />
      <div className="absolute inset-0 bg-gradient-to-r from-black/70 to-black/20" />
      {/* TODO: Add highlighted chapter overlay here */}
    </div>
  );
};

const HighlightedChapterInfo = (): React.JSX.Element => {
  const chapter = useActiveChapter();
  if (!chapter) return <></>;

  return (
    <div className="flex flex-col flex-1 align-start justify-center max-w-lg">
      <div className="flex flex-col flex-0">
        <h1 className="text-4xl font-bold text-white mb-4">{chapter.title}</h1>
        <p className="text-lg text-white/80 mb-6">{chapter.description}</p>
        <div className="flex items-center gap-6 text-white/60">
          <div>{chapter.duration}</div>
          {chapter.progress !== undefined && (
            <div className="flex items-center gap-2">
              <div className="w-32 bg-white/20 rounded-full overflow-hidden">
                <Progress value={chapter.progress} />
              </div>
              <span>{chapter.progress}%</span>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

const ChapterCatalogHighlightView = (): React.JSX.Element => {
  const { chapters, activeIndex, setActiveIndex } = useChapterCatalog();
  const scrollContainerRef = React.useRef<HTMLDivElement>(null);

  if (!chapters?.length) return <></>;

  const handleCardClick = (index: number) => {
    setActiveIndex(index);
    if (scrollContainerRef.current) {
      const card = scrollContainerRef.current.children[index] as HTMLElement | undefined;
      if (card) card.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
    }
  };

  return (
    <div className="flex flex-col flex-1">
      <div className="flex flex-col flex-1">
        {/* Chapter Info: hide in grid view */}
        <HighlightedChapterInfo />
      </div>
      <div className="flex flex-col flex-0 relative">
        <div className="relative h-48">
          <div
            ref={scrollContainerRef}
            className="ml-auto w-[70dvw] flex gap-4 overflow-x-auto scrollbar-hide pr-12 pl-24"
            style={{
              maskImage: 'linear-gradient(to right, transparent, black 15%, black 85%, transparent)',
              WebkitMaskImage: 'linear-gradient(to right, transparent, black 15%, black 85%, transparent)',
            }}
          >
            {chapters.map((chapter, index) => (
              <div
                key={chapter.id}
                className={`flex-shrink-0 relative transition-all duration-300 ${
                  index === activeIndex ? 'scale-110 z-10' : 'rounded-lg scale-90 opacity-60 hover:opacity-80'
                }`}
              >
                <button onClick={() => handleCardClick(index)} className="relative w-36 h-48 rounded-lg overflow-hidden">
                  <Image src={chapter.image || '/placeholder.svg'} alt={chapter.title} fill className="object-cover" draggable={false} />
                  {chapter.progress !== undefined && (
                    <div className="absolute bottom-0 left-0 right-0 h-1 bg-white/20">
                      <div className="h-full bg-blue-500" style={{ width: `${chapter.progress}%` }} />
                    </div>
                  )}
                  {index === activeIndex && <div className="absolute inset-0 ring-2 ring-white" />}
                </button>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};

const ChapterCatalogContent = ({ children }: PropsWithChildren): React.JSX.Element => {
  return (
    <div className="flex flex-col flex-1 relative overflow-hidden gap-4 m-24 mr-0 select-none">
      {/* TODO: */}
      {children}
    </div>
  );
};

const ChapterCatalogGridView = (): React.JSX.Element => {
  const { chapters, setActiveIndex } = useChapterCatalog();

  if (!chapters?.length) return <></>;

  const handleCardClick = (index: number) => {
    setActiveIndex(index);
  };

  return (
    <div className="flex flex-col flex-1">
      <div className="relative h-full">
        <div
          className="grid gap-4 p-8 grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-7 2xl:grid-cols-8"
          style={{
            maskImage: 'radial-gradient(circle, black 70%, transparent 100%)',
            WebkitMaskImage: 'radial-gradient(circle, black 70%, transparent 100%)',
          }}
        >
          {chapters.map((chapter, index) => (
            <div key={chapter.id} className="relative transition-all duration-300 rounded-lg scale-90 opacity-60 hover:opacity-80">
              <button onClick={() => handleCardClick(index)} className="relative w-full aspect-[3/4] rounded-lg overflow-hidden">
                <Image src={chapter.image || '/placeholder.svg'} alt={chapter.title} fill className="object-cover" draggable={false} />
                {chapter.progress !== undefined && (
                  <div className="absolute bottom-0 left-0 right-0 h-1 bg-white/20">
                    <div className="h-full bg-blue-500" style={{ width: `${chapter.progress}%` }} />
                  </div>
                )}
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};

const ChapterCatalogInternal = (): React.JSX.Element => {
  const { viewMode } = useChapterCatalog();

  return (
    <div className="flex flex-col flex-1 relative overflow-hidden select-none">
      <HighlightedChapterBackground />
      {/* Catalog Content */}
      <ChapterCatalogContent>
        {viewMode === ViewMode.HIGHLIGHT && <ChapterCatalogHighlightView />}
        {viewMode === ViewMode.GRID && <ChapterCatalogGridView />}
        {/* Navigation */}
        <CourseCatalogNavigation />
      </ChapterCatalogContent>
    </div>
  );
};

// navigation now imported from separate component
interface ChapterCardProps {
  title: string;
  image: string;
  progress?: number;
  isSelected?: boolean;
}

export const ChapterCard = ({ title, image, progress, isSelected }: ChapterCardProps) => (
  <div className={`relative group ${isSelected ? 'scale-110 z-10' : ''} select-none`}>
    <div className="relative w-48 h-64 rounded-lg overflow-hidden">
      <Image src={image} alt={title} fill className="object-cover" draggable={false} />
      {progress !== undefined && (
        <div className="absolute bottom-0 left-0 right-0 h-1 bg-white/20">
          <div className="h-full bg-blue-500" style={{ width: `${progress}%` }} />
        </div>
      )}
      <div className="absolute inset-0 bg-black/20 group-hover:bg-black/40 transition-colors" />

      <Button size="icon" className="absolute bottom-4 right-4 opacity-0 group-hover:opacity-100 transition-opacity">
        <Play className="w-4 h-4" />
      </Button>
    </div>
  </div>
);

export const CourseCatalogNavigation = (): React.JSX.Element => {
  const { chapters, setActiveIndex, viewMode, toggleViewMode, next, previous } = useChapterCatalog();

  const handleFirst = () => setActiveIndex(0);
  const handleLast = () => setActiveIndex(chapters.length - 1);
  const handlePrevious = () => previous();
  const handleNext = () => next();

  return (
    <div className="flex flex-col flex-0">
      <div className="flex flex-row flex-1 gap-8 text-gray-100">
        <Button variant="ghost" size="icon" onClick={toggleViewMode}>
          {viewMode !== ViewMode.GRID ? <Grid className="size-8" /> : <X className="size-8" />}
        </Button>
        {viewMode !== ViewMode.GRID && (
          <div className="flex flex-row flex-1 gap-4">
            <Button variant="ghost" size="icon" onClick={handleFirst}>
              <ChevronFirst className="size-8" />
            </Button>
            <Button variant="ghost" size="icon" onClick={handlePrevious}>
              <ChevronLeft className="size-8" />
            </Button>
            <Button variant="ghost" size="icon" onClick={handleNext}>
              <ChevronRight className="size-8" />
            </Button>
            <Button variant="ghost" size="icon" onClick={handleLast}>
              <ChevronLast className="size-8" />
            </Button>
          </div>
        )}
      </div>
    </div>
  );
};
