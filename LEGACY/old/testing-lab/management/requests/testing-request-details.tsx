'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Progress } from '@/components/ui/progress';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Textarea } from '@/components/ui/textarea';
import { approveTestingRequest, joinTestingRequest, leaveTestingRequest, rejectTestingRequest, submitTestingRequestFeedback } from '@/lib/admin';
import type { TestingRequest } from '@/lib/api/generated/types.gen';
import { BarChart3, Calendar, Download, FileText, MessageSquare, Send, Star, TestTube, Users } from 'lucide-react';
import { useSession } from 'next-auth/react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import React, { useState } from 'react';
import { toast } from 'sonner';

interface TestingRequestDetailsProps {
  data: TestingRequest;
  participants?: unknown[];
  feedback?: unknown[];
  statistics?: TestingRequestStatistics;
  sessions?: { id?: string; sessionName?: string; status?: number }[];
}

interface TestingRequestStatistics {
  averageRating?: number;
  totalFeedback?: number;
  completionRate?: number;
  recommendationRate?: number;
}

interface FeedbackForm {
  rating: number;
  comments: string;
  wouldRecommend: boolean;
}

export function TestingRequestDetails({ data: request, participants = [], feedback = [], statistics = {}, sessions = [] }: TestingRequestDetailsProps): React.ReactElement {
  const { data: session } = useSession();
  const router = useRouter();
  const [showFeedbackDialog, setShowFeedbackDialog] = useState(false);
  const [feedbackForm, setFeedbackForm] = useState<FeedbackForm>({
    rating: 5,
    comments: '',
    wouldRecommend: true,
  });
  const [loading, setLoading] = useState(false);
  const [hasJoined, setHasJoined] = useState(false);
  const [approving, setApproving] = useState(false);
  const [rejecting, setRejecting] = useState(false);

  const getStatusBadge = (status: number) => {
    switch (status) {
      case 0:
        return <Badge variant="outline">Draft</Badge>;
      case 1:
        return <Badge className="bg-green-500">Open</Badge>;
      case 2:
        return <Badge className="bg-blue-500">In Progress</Badge>;
      case 3:
        return <Badge className="bg-gray-500">Completed</Badge>;
      case 4:
        return <Badge variant="destructive">Cancelled</Badge>;
      default:
        return <Badge variant="outline">Unknown</Badge>;
    }
  };

  // Simplified: static icon (per-type icon removed due to transient typing issue)

  const handleJoinRequest = async () => {
    if (!session?.user?.id) return;

    setLoading(true);
    try {
      await joinTestingRequest(request.id!, session.user.id);
      toast.success('Successfully joined testing request');
      setHasJoined(true);
      router.refresh();
    } catch {
      toast.error('Failed to join testing request');
    } finally {
      setLoading(false);
    }
  };

  const handleLeaveRequest = async () => {
    if (!session?.user?.id) return;

    setLoading(true);
    try {
      await leaveTestingRequest(request.id!, session.user.id);
      toast.success('Successfully left testing request');
      setHasJoined(false);
      router.refresh();
    } catch {
      toast.error('Failed to leave testing request');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmitFeedback = async () => {
    if (!request.id) return;

    setLoading(true);
    try {
      await submitTestingRequestFeedback(request.id, feedbackForm);
      toast.success('Feedback submitted successfully');
      setShowFeedbackDialog(false);
      router.refresh();
    } catch {
      toast.error('Failed to submit feedback');
    } finally {
      setLoading(false);
    }
  };

  const canModerate = session?.user && (session.user as any).isAdmin; // simplistic; replace with proper permission check
  const isDraft = request.status === 0;
  const hasLinkedSession = sessions.length > 0;

  const handleApprove = async () => {
    setApproving(true);
    const res = await approveTestingRequest(request.id!);
    if (res.success) {
      toast.success('Request approved');
      router.refresh();
    } else {
      toast.error(res.error || 'Approval failed');
    }
    setApproving(false);
  };

  const handleReject = async () => {
    setRejecting(true);
    const res = await rejectTestingRequest(request.id!);
    if (res.success) {
      toast.success('Request rejected');
      router.refresh();
    } else {
      toast.error(res.error || 'Rejection failed');
    }
    setRejecting(false);
  };

  // const instructionsIcon: React.ReactNode = getInstructionsIcon(request.instructionsType as number | undefined);

  return (
    <div className="container mx-auto p-6 space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white">{request.title}</h1>
            {getStatusBadge(request.status)}
          </div>
          <p className="text-gray-600 dark:text-gray-400">{request.description}</p>
        </div>
        <div className="flex gap-2">
          {canModerate && isDraft && (
            <>
              <Button variant="outline" onClick={handleReject} disabled={rejecting || approving}>
                {rejecting ? 'Rejecting...' : 'Reject'}
              </Button>
              <Button onClick={handleApprove} disabled={approving || rejecting || !hasLinkedSession}>
                {approving ? 'Approving...' : hasLinkedSession ? 'Approve' : 'Link a Session First'}
              </Button>
            </>
          )}
          {request.status === 1 && session?.user && !hasJoined && (
            <Button onClick={handleJoinRequest} disabled={loading}>
              <TestTube className="h-4 w-4 mr-2" />
              Join Testing
            </Button>
          )}
          {hasJoined && (
            <Button variant="outline" onClick={handleLeaveRequest} disabled={loading}>
              Leave Testing
            </Button>
          )}
          {request.downloadUrl && (
            <Button variant="outline" asChild>
              <Link href={request.downloadUrl} target="_blank">
                <Download className="h-4 w-4 mr-2" />
                Download Game
              </Link>
            </Button>
          )}
          {request.status === 3 && hasJoined && (
            <Dialog open={showFeedbackDialog} onOpenChange={setShowFeedbackDialog}>
              <DialogTrigger asChild>
                <Button>
                  <MessageSquare className="h-4 w-4 mr-2" />
                  Submit Feedback
                </Button>
              </DialogTrigger>
              <DialogContent className="max-w-2xl">
                <DialogHeader>
                  <DialogTitle>Submit Testing Feedback</DialogTitle>
                  <DialogDescription>Share your experience testing {request.title}</DialogDescription>
                </DialogHeader>
                <div className="space-y-4">
                  <div>
                    <Label htmlFor="rating">Overall Rating</Label>
                    <RadioGroup value={feedbackForm.rating.toString()} onValueChange={(value) => setFeedbackForm({ ...feedbackForm, rating: parseInt(value) })} className="flex gap-4 mt-2">
                      {[1, 2, 3, 4, 5].map((rating) => (
                        <div key={rating} className="flex items-center space-x-2">
                          <RadioGroupItem value={rating.toString()} id={`rating-${rating}`} />
                          <Label htmlFor={`rating-${rating}`} className="flex items-center gap-1">
                            {rating} <Star className="h-4 w-4" />
                          </Label>
                        </div>
                      ))}
                    </RadioGroup>
                  </div>
                  <div>
                    <Label htmlFor="comments">Comments</Label>
                    <Textarea id="comments" placeholder="Share your thoughts about the game..." value={feedbackForm.comments} onChange={(e) => setFeedbackForm({ ...feedbackForm, comments: e.target.value })} className="mt-2" rows={4} />
                  </div>
                  <div>
                    <Label>Would you recommend this game?</Label>
                    <RadioGroup
                      value={feedbackForm.wouldRecommend.toString()}
                      onValueChange={(value) =>
                        setFeedbackForm({
                          ...feedbackForm,
                          wouldRecommend: value === 'true',
                        })
                      }
                      className="flex gap-4 mt-2"
                    >
                      <div className="flex items-center space-x-2">
                        <RadioGroupItem value="true" id="recommend-yes" />
                        <Label htmlFor="recommend-yes">Yes</Label>
                      </div>
                      <div className="flex items-center space-x-2">
                        <RadioGroupItem value="false" id="recommend-no" />
                        <Label htmlFor="recommend-no">No</Label>
                      </div>
                    </RadioGroup>
                  </div>
                  <div className="flex justify-end gap-2">
                    <Button variant="outline" onClick={() => setShowFeedbackDialog(false)}>
                      Cancel
                    </Button>
                    <Button onClick={handleSubmitFeedback} disabled={loading}>
                      <Send className="h-4 w-4 mr-2" />
                      Submit Feedback
                    </Button>
                  </div>
                </div>
              </DialogContent>
            </Dialog>
          )}
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Project Information */}
          {request.projectVersion && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <TestTube className="h-5 w-5" />
                  Project Information
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <Label className="text-sm font-medium">Project</Label>
                  <p className="text-lg">{request.projectVersion.project.title}</p>
                </div>
                <div>
                  <Label className="text-sm font-medium">Version</Label>
                  <p className="text-lg">{request.projectVersion.versionNumber}</p>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Instructions */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <FileText className="h-4 w-4" />
                Testing Instructions
              </CardTitle>
            </CardHeader>
            <CardContent>
              {isDraft && canModerate && !hasLinkedSession && (
                <div className="mb-4 border border-amber-400/30 bg-amber-500/10 rounded-md p-3 text-sm text-amber-300">
                  Esta request ainda não possui uma Testing Session vinculada. Crie ou vincule uma sessão antes de aprovar.
                </div>
              )}
              {(() => {
                const t = (request as any).instructionsType;
                const isText = t === 0 || t === 'inline' || t === 'text';
                const isUrl = t === 1 || t === 'url';
                if (isText && request.instructionsContent) {
                  return (
                    <div className="prose dark:prose-invert max-w-none">
                      <p>{request.instructionsContent}</p>
                    </div>
                  );
                }
                if (isUrl && request.instructionsUrl) {
                  return (
                    <Button variant="outline" asChild>
                      <Link href={request.instructionsUrl} target="_blank">
                        <FileText className="h-4 w-4 mr-2" />
                        View Instructions
                      </Link>
                    </Button>
                  );
                }
                return null;
              })()}
            </CardContent>
          </Card>

          {/* Feedback Form */}
          {request.feedbackFormContent && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <MessageSquare className="h-5 w-5" />
                  Feedback Form
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="prose dark:prose-invert max-w-none">
                  <p>{request.feedbackFormContent}</p>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Statistics */}
          {!!statistics && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <BarChart3 className="h-5 w-5" />
                  Testing Statistics
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid gap-4 md:grid-cols-2">
                  <div>
                    <Label className="text-sm font-medium">Average Rating</Label>
                    <div className="flex items-center gap-2 mt-1">
                      <Star className="h-4 w-4 text-yellow-500" />
                      <span className="text-lg font-semibold">{statistics.averageRating ?? 'N/A'}</span>
                    </div>
                  </div>
                  <div>
                    <Label className="text-sm font-medium">Total Feedback</Label>
                    <p className="text-lg font-semibold">{statistics.totalFeedback ?? 0}</p>
                  </div>
                  <div>
                    <Label className="text-sm font-medium">Completion Rate</Label>
                    <div className="mt-1">
                      <Progress value={statistics.completionRate ?? 0} className="h-2" />
                      <p className="text-sm text-gray-600 mt-1">{statistics.completionRate ?? 0}%</p>
                    </div>
                  </div>
                  <div>
                    <Label className="text-sm font-medium">Would Recommend</Label>
                    <p className="text-lg font-semibold">{statistics.recommendationRate ?? 0}%</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Linked Sessions */}
          <Card>
            <CardHeader>
              <CardTitle>Linked Sessions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {sessions.length === 0 && (
                <div className="text-sm text-gray-400">
                  Nenhuma sessão vinculada.
                  {canModerate && (
                    <div className="mt-2">
                      <Button size="sm" asChild>
                        <Link href={`/dashboard/testing-lab/sessions/create?testingRequestId=${request.id}`}>Criar sessão</Link>
                      </Button>
                    </div>
                  )}
                </div>
              )}
              {sessions.length > 0 && (
                <ul className="space-y-2">
                  {sessions.map(s => (
                    <li key={s.id} className="rounded border border-slate-700 p-2 text-sm flex items-center justify-between">
                      <span className="truncate max-w-[150px]" title={s.sessionName || s.id}>{s.sessionName || s.id}</span>
                      <Link href={`/dashboard/testing-lab/sessions/${s.id}`} className="text-blue-400 hover:underline">ver</Link>
                    </li>
                  ))}
                </ul>
              )}
            </CardContent>
          </Card>
          {/* Quick Info */}
          <Card>
            <CardHeader>
              <CardTitle>Quick Info</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-2 text-sm">
                <Calendar className="h-4 w-4 text-gray-500" />
                <span>Ends: {new Date(request.endDate).toLocaleDateString()}</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <Users className="h-4 w-4 text-gray-500" />
                <span>
                  {request.currentTesterCount || 0}
                  {request.maxTesters ? `/${request.maxTesters}` : ''} testers
                </span>
              </div>
              {request.createdBy && (
                <div>
                  <Label className="text-sm font-medium">Created by</Label>
                  <p className="text-sm">{request.createdBy.name}</p>
                  <p className="text-xs text-gray-500">{request.createdBy.email}</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Participants */}
          {participants.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Participants ({participants.length})</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  {participants.slice(0, 5).map((participant: unknown) => {
                    const p = participant as { id: string; name?: string; email?: string };
                    return (
                      <div key={p.id} className="flex items-center gap-2">
                        <div className="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center text-white text-sm font-medium">{p.name?.charAt(0) || 'U'}</div>
                        <div>
                          <p className="text-sm font-medium">{p.name}</p>
                          <p className="text-xs text-gray-500">{p.email}</p>
                        </div>
                      </div>
                    );
                  })}
                  {participants.length > 5 && <p className="text-sm text-gray-500">+{participants.length - 5} more</p>}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Recent Feedback */}
          {feedback.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Recent Feedback</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {feedback.slice(0, 3).map((item: unknown) => {
                    const f = item as { id: string; rating?: number; comments?: string; user?: { name?: string } };
                    return (
                      <div key={f.id} className="border-l-2 border-blue-500 pl-3">
                        <div className="flex items-center gap-1 mb-1">
                          {Array.from({ length: 5 }).map((_, i) => (
                            <Star key={i} className={`h-3 w-3 ${i < (f.rating || 0) ? 'text-yellow-500 fill-current' : 'text-gray-300'}`} />
                          ))}
                        </div>
                        <p className="text-sm text-gray-600 line-clamp-2">{f.comments}</p>
                        <p className="text-xs text-gray-500 mt-1">by {f.user?.name || 'Anonymous'}</p>
                      </div>
                    );
                  })}
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
