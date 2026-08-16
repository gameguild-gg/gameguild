'use client';

import { addCourseSupportTicketMessage, resolveCourseSupportTicket } from '@/lib/learning/actions';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@game-guild/ui/components/dialog';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { CheckCircle2, Loader2, Reply } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState, useTransition } from 'react';

export function CourseTicketActionPanel({
  courseId,
  ticketId,
  resolved,
}: {
  courseId: string;
  ticketId: string;
  resolved: boolean;
}) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [reply, setReply] = useState('');
  const [resolutionOpen, setResolutionOpen] = useState(false);
  const [resolutionSummary, setResolutionSummary] = useState('');
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const sendReply = () => {
    setMessage(null);
    startTransition(async () => {
      const result = await addCourseSupportTicketMessage({ courseId, ticketId, message: reply.trim() });
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      setReply('');
      setMessage({ type: 'success', text: 'Reply sent.' });
      router.refresh();
    });
  };

  const resolveTicket = () => {
    setMessage(null);
    startTransition(async () => {
      const result = await resolveCourseSupportTicket({
        courseId,
        ticketId,
        summary: resolutionSummary.trim(),
      });
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      setResolutionOpen(false);
      setResolutionSummary('');
      setMessage({ type: 'success', text: 'Ticket resolved.' });
      router.refresh();
    });
  };

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Instructor actions</CardTitle>
          <CardDescription>Respond to the learner or close the operational support loop.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="space-y-2">
            <Label htmlFor="ticket-reply">Reply</Label>
            <Textarea
              id="ticket-reply"
              rows={6}
              value={reply}
              onChange={(event) => setReply(event.target.value)}
              disabled={isPending || resolved}
              placeholder="Explain the fix, request details, or point the learner to the right lesson."
            />
            <Button type="button" className="w-full" disabled={isPending || resolved || reply.trim().length < 2} onClick={sendReply}>
              {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Reply className="mr-2 size-4" />}
              Send reply
            </Button>
          </div>

          <Button type="button" variant="outline" className="w-full" disabled={isPending || resolved} onClick={() => setResolutionOpen(true)}>
            <CheckCircle2 className="mr-2 size-4" />
            Resolve ticket
          </Button>

          {message && (
            <p role={message.type === 'success' ? 'status' : 'alert'} className={message.type === 'success' ? 'text-sm text-emerald-600' : 'text-sm text-destructive'}>
              {message.text}
            </p>
          )}
        </CardContent>
      </Card>

      <Dialog open={resolutionOpen} onOpenChange={setResolutionOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Resolve ticket</DialogTitle>
            <DialogDescription>Record what fixed the learner issue before closing the active support queue item.</DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="ticket-resolution-summary">Resolution summary</Label>
            <Textarea id="ticket-resolution-summary" value={resolutionSummary} onChange={(event) => setResolutionSummary(event.target.value)} rows={5} />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setResolutionOpen(false)}>Cancel</Button>
            <Button type="button" onClick={resolveTicket} disabled={isPending || resolutionSummary.trim().length < 3}>
              {isPending && <Loader2 className="mr-2 size-4 animate-spin" />}
              Confirm resolution
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
