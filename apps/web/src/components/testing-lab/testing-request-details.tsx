'use client';

import { useState } from 'react';
import { useSession } from 'next-auth/react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Progress } from '@/components/ui/progress';
import { toast } from 'sonner';
import { Calendar, Clock, Download, FileText, Send, Users, Star, MessageSquare, BarChart3, CheckCircle, XCircle, AlertTriangle, TestTube } from 'lucide-react';
import Link from 'next/link';
import type { TestingRequest } from '@/lib/api/generated/types.gen';
import { joinTestingRequest, leaveTestingRequest, checkTestingRequestParticipation, submitTestingRequestFeedback } from '@/lib/testing-lab/testing-lab.actions';

interface TestingRequestDetailsProps {
  request: TestingRequest;
  participants: any[];
  feedback: any[];
  statistics: any;
}

interface FeedbackForm {
  rating: number;
  comments: string;
  wouldRecommend: boolean;
}

export function TestingRequestDetails({ request, participants, feedback, statistics }: TestingRequestDetailsProps) {
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

  const getInstructionsIcon = (type: string) => {
    switch (type) {
      case 'file':
        return <FileText className="h-4 w-4" />;
      case 'url':
        return <FileText className="h-4 w-4" />;
      default:
        return <FileText className="h-4 w-4" />;
    }
  };

  const handleJoinRequest = async () => {
    if (!session?.user?.id) return;

    setLoading(true);
    try {
      await joinTestingRequest(request.id!, session.user.id);
      toast.success('Successfully joined testing request');
      setHasJoined(true);
      router.refresh();
    } catch (error) {
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
    } catch (error) {
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
    } catch (error) {
      toast.error('Failed to submit feedback');
    } finally {
      setLoading(false);
    }
  };

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
                    <RadioGroup
                      value={feedbackForm.rating.toString()}
                      onValueChange={(value) => setFeedbackForm({ ...feedbackForm, rating: parseInt(value) })}
                      className="flex gap-4 mt-2"
                    >
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
                    <Textarea
                      id="comments"
                      placeholder="Share your thoughts about the game..."
                      value={feedbackForm.comments}
                      onChange={(e) => setFeedbackForm({ ...feedbackForm, comments: e.target.value })}
                      className="mt-2"
                      rows={4}
                    />
                  </div>
                  <div>
                    <Label>Would you recommend this game?</Label>
                    <RadioGroup
                      value={feedbackForm.wouldRecommend.toString()}
                      onValueChange={(value) => setFeedbackForm({ ...feedbackForm, wouldRecommend: value === 'true' })}
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
                {getInstructionsIcon(request.instructionsType)}
                Testing Instructions
              </CardTitle>
            </CardHeader>
            <CardContent>
              {request.instructionsType === 'inline' && request.instructionsContent && (
                <div className="prose dark:prose-invert max-w-none">
                  <p>{request.instructionsContent}</p>
                </div>
              )}
              {request.instructionsType === 'url' && request.instructionsUrl && (
                <Button variant="outline" asChild>
                  <Link href={request.instructionsUrl} target="_blank">
                    <FileText className="h-4 w-4 mr-2" />
                    View Instructions
                  </Link>
                </Button>
              )}
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
          {statistics && (
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
                      <span className="text-lg font-semibold">{statistics.averageRating || 'N/A'}</span>
                    </div>
                  </div>
                  <div>
                    <Label className="text-sm font-medium">Total Feedback</Label>
                    <p className="text-lg font-semibold">{statistics.totalFeedback || 0}</p>
                  </div>
                  <div>
                    <Label className="text-sm font-medium">Completion Rate</Label>
                    <div className="mt-1">
                      <Progress value={statistics.completionRate || 0} className="h-2" />
                      <p className="text-sm text-gray-600 mt-1">{statistics.completionRate || 0}%</p>
                    </div>
                  </div>
                  <div>
                    <Label className="text-sm font-medium">Would Recommend</Label>
                    <p className="text-lg font-semibold">{statistics.recommendationRate || 0}%</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
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
                  {participants.slice(0, 5).map((participant: any) => (
                    <div key={participant.id} className="flex items-center gap-2">
                      <div className="w-8 h-8 rounded-full bg-blue-500 flex items-center justify-center text-white text-sm font-medium">
                        {participant.name?.charAt(0) || 'U'}
                      </div>
                      <div>
                        <p className="text-sm font-medium">{participant.name}</p>
                        <p className="text-xs text-gray-500">{participant.email}</p>
                      </div>
                    </div>
                  ))}
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
                  {feedback.slice(0, 3).map((item: any) => (
                    <div key={item.id} className="border-l-2 border-blue-500 pl-3">
                      <div className="flex items-center gap-1 mb-1">
                        {Array.from({ length: 5 }).map((_, i) => (
                          <Star key={i} className={`h-3 w-3 ${i < (item.rating || 0) ? 'text-yellow-500 fill-current' : 'text-gray-300'}`} />
                        ))}
                      </div>
                      <p className="text-sm text-gray-600 line-clamp-2">{item.comments}</p>
                      <p className="text-xs text-gray-500 mt-1">by {item.user?.name || 'Anonymous'}</p>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
}
