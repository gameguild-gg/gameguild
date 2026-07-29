import { TestingLabActionForm } from '@/components/testing-lab/testing-lab-action-form';
import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues, TestingLabEmptyState } from '@/components/testing-lab/testing-lab-state';
import { rateTestingFeedback, reportTestingFeedback } from '@/lib/testing-lab/actions';
import { getTestingLabDashboard, getTestingRequestDetail } from '@/lib/testing-lab';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { MessageSquareText, Search, Star } from 'lucide-react';

export default async function TestingLabFeedbackPage({ searchParams }: { searchParams: Promise<{ q?: string }> }) {
  const params = await searchParams;
  const q = params.q?.trim().toLowerCase() ?? '';
  const directory = await getTestingLabDashboard();
  const requestDetails = await Promise.all(directory.requests.slice(0, 50).map((request) => getTestingRequestDetail(request.id)));
  const issues = [...directory.accessIssues, ...requestDetails.flatMap((detail) => detail.accessIssues)];
  const feedback = requestDetails
    .flatMap((detail) =>
      detail.feedback.map((entry) => ({
        entry,
        requestTitle: detail.request?.title ?? 'Testing request',
      })),
    )
    .filter(({ entry, requestTitle }) => {
      if (!q) return true;
      return `${requestTitle} ${entry.user?.name ?? ''} ${entry.user?.email ?? ''} ${entry.additionalNotes ?? ''}`.toLowerCase().includes(q);
    });

  return (
    <main className="space-y-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={MessageSquareText}
        title="Testing feedback"
        description="Review every feedback submission used by active Testing Lab requests, assign quality ratings, and report unsafe or low-integrity content."
      />
      <TestingLabAccessIssues issues={[...new Set(issues)]} />
      <form method="get" className="relative max-w-xl">
        <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
        <Input name="q" defaultValue={params.q} className="pl-9" placeholder="Search request, member, or feedback" />
      </form>
      {feedback.length === 0 ? (
        <TestingLabEmptyState
          title={q ? 'No feedback matches this search' : 'No feedback submitted'}
          description="Participant feedback appears after a member completes a testing report."
        />
      ) : (
        <div className="space-y-3">
          {feedback.map(({ entry, requestTitle }) => (
            <article key={entry.id} className="rounded-md border p-4">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="font-semibold">{requestTitle}</h2>
                    {entry.isReported ? <Badge variant="destructive">Reported</Badge> : null}
                    <Badge variant="outline">{entry.testingContext}</Badge>
                  </div>
                  <p className="mt-1 text-sm text-muted-foreground">{entry.user?.name ?? entry.user?.email ?? entry.userId}</p>
                </div>
                <div className="flex items-center gap-1 text-sm">
                  <Star className="size-4" />
                  {entry.overallRating ?? '-'} / 5
                </div>
              </div>
              <p className="mt-4 whitespace-pre-wrap text-sm">{entry.additionalNotes ?? entry.feedbackData}</p>
              <div className="mt-4 flex flex-col gap-3 border-t pt-3 lg:flex-row lg:items-end lg:justify-between">
                <TestingLabActionForm action={rateTestingFeedback} submitLabel="Save quality" pendingLabel="Saving..." className="flex flex-wrap items-end gap-2" actionsClassName="">
                  <input type="hidden" name="feedbackId" value={entry.id} />
                  <label className="text-xs font-medium">
                    Quality
                    <select
                      name="quality"
                      defaultValue={entry.qualityRating ?? 'Medium'}
                      className="mt-1 block h-8 rounded-md border bg-background px-2 text-sm"
                    >
                      <option>Low</option>
                      <option>Medium</option>
                      <option>High</option>
                    </select>
                  </label>
                </TestingLabActionForm>
                <TestingLabActionForm action={reportTestingFeedback} submitLabel="Report" pendingLabel="Reporting..." resetOnSuccess className="flex min-w-0 flex-1 items-end gap-2 lg:max-w-lg" actionsClassName="">
                  <input type="hidden" name="feedbackId" value={entry.id} />
                  <label className="min-w-0 flex-1 text-xs font-medium">
                    Moderation report
                    <Input name="reason" required className="mt-1 h-8" placeholder="Reason for moderator review" />
                  </label>
                </TestingLabActionForm>
              </div>
            </article>
          ))}
        </div>
      )}
    </main>
  );
}
