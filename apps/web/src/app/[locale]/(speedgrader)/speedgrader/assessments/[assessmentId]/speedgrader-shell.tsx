'use client';

import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import { Link, usePathname, useRouter } from '@/i18n/navigation';
import { useState, type ReactNode } from 'react';
import type { LearningAssessmentsGradingQueueItem } from '@game-guild/client';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import {
  ResizableHandle,
  ResizablePanel,
  ResizablePanelGroup,
} from '@game-guild/ui/components/resizable';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@game-guild/ui/components/select';

// ponytail: viewer/grading slots are render-props over the CURRENT item so
// todo 12 can slot real viewers/panels in without touching navigation logic.

export function itemLabel(
  item: LearningAssessmentsGradingQueueItem,
): string {
  const attempt = item.attemptCount
    ? `attempt ${item.attemptNumber ?? 1} of ${item.attemptCount}`
    : `attempt ${item.attemptNumber ?? 1}`;
  if (item.isGroup) {
    const members = item.memberNames?.length
      ? ` (${item.memberNames.join(', ')})`
      : '';
    return `Group: ${item.groupName ?? 'Unnamed group'}${members} — ${attempt} — ${item.status ?? 'InProgress'}`;
  }
  return `${item.displayName ?? 'Unknown student'} — ${attempt} — ${item.status ?? 'InProgress'}`;
}

export interface SpeedgraderShellProps {
  assessmentTitle: string;
  assessmentId: string;
  courseSlug: string;
  items: LearningAssessmentsGradingQueueItem[];
  needsGrading: number;
  initialIndex?: number;
  /** Todo 12: render the submission viewer for the current item. */
  renderViewer?: (item: LearningAssessmentsGradingQueueItem) => ReactNode;
  /** Todo 12: render the grading panel for the current item. */
  renderGrading?: (item: LearningAssessmentsGradingQueueItem) => ReactNode;
}

export function SpeedgraderShell({
  assessmentTitle,
  assessmentId,
  courseSlug,
  items,
  needsGrading,
  initialIndex = 0,
  renderViewer,
  renderGrading,
}: SpeedgraderShellProps): React.JSX.Element {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const total = items.length;
  const maxIndex = Math.max(total - 1, 0);
  const [rawIndex, setRawIndex] = useState(initialIndex);
  // Clamp at render: covers ?nav= out of range AND item-count changes after refresh.
  const index = Math.min(
    Number.isFinite(rawIndex) ? Math.max(rawIndex, 0) : 0,
    maxIndex,
  );

  const goTo = (next: number) => {
    setRawIndex(next);
    const params = new URLSearchParams(searchParams?.toString());
    params.set('nav', String(next));
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  };

  const backHref = `/dashboard/learning/courses/${courseSlug}/assessments/${assessmentId}/submissions`;

  if (total === 0) {
    return (
      <div data-testid="speedgrader-empty" className="grid flex-1 place-items-center p-6">
        <div className="max-w-md text-center">
          <h1 className="text-lg font-semibold text-foreground">{assessmentTitle}</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            No submissions to grade yet. Check back once students start submitting.
          </p>
          <Link
            href={backHref}
            className="mt-4 inline-block text-sm text-primary underline-offset-4 hover:underline"
          >
            Back to submissions
          </Link>
        </div>
      </div>
    );
  }

  const current = items[index];

  return (
    <div className="flex min-h-0 flex-1 flex-col">
      <header
        data-testid="speedgrader-header"
        className="flex flex-wrap items-center gap-2 border-b px-3 py-2"
      >
        <Button variant="ghost" size="sm" asChild>
          <Link href={backHref} aria-label="Back to submissions">
            <ChevronLeft className="size-4" />
            Back
          </Link>
        </Button>
        <h1 className="min-w-0 truncate text-sm font-semibold">{assessmentTitle}</h1>
        <Badge data-testid="needs-grading-badge" variant="secondary">
          {needsGrading} to grade
        </Badge>
        <div className="ml-auto flex items-center gap-2">
          <Button
            variant="outline"
            size="icon"
            aria-label="Previous submission"
            onClick={() => goTo((index - 1 + total) % total)}
          >
            <ChevronLeft className="size-4" />
          </Button>
          <span data-testid="item-counter" className="text-sm tabular-nums">
            {index + 1} of {total}
          </span>
          <Button
            variant="outline"
            size="icon"
            aria-label="Next submission"
            onClick={() => goTo((index + 1) % total)}
          >
            <ChevronRight className="size-4" />
          </Button>
          <Select
            data-testid="item-picker"
            value={String(index)}
            onValueChange={(value) => goTo(Number.parseInt(value, 10))}
          >
            <SelectTrigger className="w-64" aria-label="Select submission">
              <SelectValue placeholder="Select submission" />
            </SelectTrigger>
            <SelectContent>
              {items.map((item, i) => (
                <SelectItem key={item.submissionId ?? String(i)} value={String(i)}>
                  {itemLabel(item)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </header>

      <ResizablePanelGroup orientation="horizontal" className="min-h-0 flex-1">
        <ResizablePanel defaultSize="60" minSize="20">
          {renderViewer ? (
            renderViewer(current)
          ) : (
            <div data-testid="viewer-slot" className="h-full overflow-auto p-4 text-sm">
              {itemLabel(current)}
            </div>
          )}
        </ResizablePanel>
        <ResizableHandle />
        <ResizablePanel defaultSize="40" minSize="20">
          {renderGrading ? (
            renderGrading(current)
          ) : (
            <div data-testid="grading-slot" className="h-full overflow-auto p-4 text-sm text-muted-foreground">
              Grading panel
            </div>
          )}
        </ResizablePanel>
      </ResizablePanelGroup>
    </div>
  );
}
