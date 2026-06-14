'use client';

import { useMemo, useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { submitActivity } from '@/lib/courses/server-actions';
import { MessageSquare, Send, Star } from 'lucide-react';

interface PeerReviewContentItem {
  id: string;
  title: string;
  type: 'lesson' | 'activity' | 'quiz' | 'assignment' | 'peer-review';
  status: 'locked' | 'available' | 'in-progress' | 'completed';
  duration?: number;
  description?: string;
  order: number;
  isRequired: boolean;
  activityType?: 'text' | 'code' | 'file' | 'quiz' | 'discussion';
  content?: unknown;
}

interface ReviewCriterion {
  name: string;
  description: string;
  weight: number;
}

interface PeerReviewInterfaceProps {
  item: PeerReviewContentItem;
  courseId: string;
  onComplete: (score?: number) => void;
}

const defaultCriteria: ReviewCriterion[] = [
  {
    name: 'clarity',
    description: 'Is the feedback clear, specific, and easy to act on?',
    weight: 0.5,
  },
  {
    name: 'usefulness',
    description: 'Will this feedback help the creator improve the work?',
    weight: 0.5,
  },
];

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function getContentRecord(content: unknown): Record<string, unknown> | null {
  if (isRecord(content)) {
    return content;
  }

  if (typeof content === 'string' && content.trim()) {
    try {
      const parsed = JSON.parse(content) as unknown;
      return isRecord(parsed) ? parsed : null;
    } catch {
      return null;
    }
  }

  return null;
}

function normalizeCriteria(content: unknown): ReviewCriterion[] {
  const record = getContentRecord(content);

  if (!record || !Array.isArray(record.criteria)) {
    return defaultCriteria;
  }

  const criteria = record.criteria
    .filter(isRecord)
    .map((criterion, index) => ({
      name: typeof criterion.name === 'string' && criterion.name.trim() ? criterion.name.trim() : `criterion-${index + 1}`,
      description: typeof criterion.description === 'string' ? criterion.description : 'Rate this aspect of the submission.',
      weight: typeof criterion.weight === 'number' && criterion.weight > 0 ? criterion.weight : 1,
    }));

  if (criteria.length === 0) {
    return defaultCriteria;
  }

  const totalWeight = criteria.reduce((sum, criterion) => sum + criterion.weight, 0);
  return criteria.map((criterion) => ({
    ...criterion,
    weight: criterion.weight / totalWeight,
  }));
}

function getPrompt(item: PeerReviewContentItem): string {
  const record = getContentRecord(item.content);

  if (record) {
    if (typeof record.prompt === 'string' && record.prompt.trim()) {
      return record.prompt.trim();
    }

    if (typeof record.instructions === 'string' && record.instructions.trim()) {
      return record.instructions.trim();
    }
  }

  return item.description || 'Review the assigned peer submission and provide specific, constructive feedback.';
}

function getOverallRating(criteria: ReviewCriterion[], ratings: Record<string, number>): number {
  return criteria.reduce((sum, criterion) => sum + (ratings[criterion.name] ?? 0) * criterion.weight, 0);
}

export function PeerReviewInterface({ item, courseId, onComplete }: PeerReviewInterfaceProps) {
  const [hasStarted, setHasStarted] = useState(false);
  const [ratings, setRatings] = useState<Record<string, number>>({});
  const [feedback, setFeedback] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const criteria = useMemo(() => normalizeCriteria(item.content), [item.content]);
  const prompt = useMemo(() => getPrompt(item), [item]);
  const overallRating = getOverallRating(criteria, ratings);
  const canSubmit = feedback.trim().length > 0 && criteria.every((criterion) => ratings[criterion.name]);

  async function handleSubmit() {
    if (!canSubmit) {
      return;
    }

    setIsSubmitting(true);
    setMessage(null);

    const result = await submitActivity({
      activityId: item.id,
      courseId,
      activityType: item.activityType || 'discussion',
      content: {
        prompt,
        feedback: feedback.trim(),
        criteria: ratings,
        rating: Number(overallRating.toFixed(1)),
      },
      isGraded: true,
      attempt: 1,
      submissionData: {
        kind: 'peer-review',
        contentId: item.id,
      },
    });

    setIsSubmitting(false);

    if (!result.success) {
      setMessage({
        type: 'error',
        text: result.message || 'Peer review could not be submitted. Review the details and try again.',
      });
      return;
    }

    setMessage({ type: 'success', text: 'Peer review submitted.' });
    onComplete(Math.round(overallRating * 20));
  }

  if (!hasStarted) {
    return (
      <div className="py-12 text-center">
        <MessageSquare className="mx-auto mb-4 h-16 w-16 text-blue-400" />
        <h3 className="mb-2 mt-4 text-xl font-semibold text-white">{item.title}</h3>
        <p className="mx-auto mb-6 max-w-2xl text-slate-400">{prompt}</p>
        <div className="mb-6 flex items-center justify-center gap-3">
          {item.isRequired ? (
            <Badge variant="secondary" className="bg-blue-500/15 text-blue-100">
              Required
            </Badge>
          ) : null}
          <Badge variant="outline" className="border-slate-600 text-slate-200">
            {criteria.length} criteria
          </Badge>
        </div>
        <Button onClick={() => setHasStarted(true)} className="bg-blue-600 text-white hover:bg-blue-700">
          Start peer review
        </Button>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      {message ? (
        <div
          role={message.type === 'success' ? 'status' : 'alert'}
          className={`rounded-md border px-4 py-3 text-sm ${
            message.type === 'success'
              ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-100'
              : 'border-red-500/40 bg-red-500/10 text-red-100'
          }`}
        >
          {message.text}
        </div>
      ) : null}

      <Card className="border-slate-700/50 bg-slate-900/50">
        <CardHeader>
          <CardTitle className="text-white">Peer review prompt</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-slate-300">{prompt}</p>
        </CardContent>
      </Card>

      <Card className="border-slate-700/50 bg-slate-900/50">
        <CardHeader>
          <CardTitle className="text-white">Review criteria</CardTitle>
        </CardHeader>
        <CardContent className="space-y-5">
          {criteria.map((criterion) => (
            <div key={criterion.name} className="space-y-2">
              <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="font-medium capitalize text-white">{criterion.name}</p>
                  <p className="text-sm text-slate-400">{criterion.description}</p>
                </div>
                <div className="flex gap-1">
                  {[1, 2, 3, 4, 5].map((rating) => {
                    const selected = rating <= (ratings[criterion.name] ?? 0);
                    return (
                      <Button
                        key={rating}
                        type="button"
                        variant="ghost"
                        size="icon"
                        aria-label={`Rate ${criterion.name} ${rating}`}
                        className="size-8 text-slate-400 hover:text-yellow-300"
                        onClick={() => setRatings((current) => ({ ...current, [criterion.name]: rating }))}
                      >
                        <Star className={selected ? 'size-4 fill-yellow-400 text-yellow-400' : 'size-4'} />
                      </Button>
                    );
                  })}
                </div>
              </div>
            </div>
          ))}
        </CardContent>
      </Card>

      <Card className="border-slate-700/50 bg-slate-900/50">
        <CardHeader>
          <CardTitle className="text-white">Written feedback</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <Label htmlFor="peer-review-feedback">Written feedback</Label>
          <Textarea
            id="peer-review-feedback"
            value={feedback}
            onChange={(event) => setFeedback(event.target.value)}
            placeholder="Write constructive, specific feedback for your peer..."
            rows={5}
            className="border-slate-600 bg-slate-950 text-white"
          />
          <div className="flex items-center justify-between text-sm text-slate-400">
            <span>Calculated rating: {overallRating.toFixed(1)}/5</span>
            <Button onClick={handleSubmit} disabled={!canSubmit || isSubmitting} className="bg-emerald-600 text-white hover:bg-emerald-700">
              <Send className="mr-2 size-4" />
              {isSubmitting ? 'Submitting...' : 'Submit peer review'}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
