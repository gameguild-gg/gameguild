'use client';

import { useState } from 'react';
import { useSession } from 'next-auth/react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { Progress } from '@/components/ui/progress';
import { toast } from 'sonner';
import { Calendar, Star, MessageSquare, User, ThumbsUp, ThumbsDown, TestTube } from 'lucide-react';
import Link from 'next/link';
import type { TestingFeedback } from '@/lib/api/generated/types.gen';

interface TestingFeedbackDetailsProps {
  data: TestingFeedback;
}

export function TestingFeedbackDetails({ data: feedback }: TestingFeedbackDetailsProps) {
  const { data: session } = useSession();
  const router = useRouter();
  const [loading, setLoading] = useState(false);

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const getRatingStars = (rating: number) => {
    return Array.from({ length: 5 }, (_, i) => <Star key={i} className={`w-4 h-4 ${i < rating ? 'fill-yellow-400 text-yellow-400' : 'text-gray-300'}`} />);
  };

  const getQualityBadge = (quality?: number) => {
    if (!quality) return <Badge variant="outline">Not Rated</Badge>;

    switch (quality) {
      case 1:
        return <Badge variant="destructive">Poor</Badge>;
      case 2:
        return <Badge variant="secondary">Fair</Badge>;
      case 3:
        return <Badge variant="outline">Good</Badge>;
      case 4:
        return <Badge variant="default">Very Good</Badge>;
      case 5:
        return (
          <Badge variant="secondary" className="bg-green-100 text-green-800">
            Excellent
          </Badge>
        );
      default:
        return <Badge variant="outline">Not Rated</Badge>;
    }
  };

  const handleReportFeedback = async () => {
    if (!session?.user?.id) {
      toast.error('Please log in to report feedback');
      return;
    }

    setLoading(true);
    try {
      // Implement report feedback logic here
      toast.success('Feedback reported successfully');
    } catch (error) {
      toast.error('Failed to report feedback. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container mx-auto py-8 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Feedback Details</h1>
          <p className="text-muted-foreground mt-2">Testing feedback submitted by user</p>
        </div>
        <div className="flex items-center gap-2">
          {getQualityBadge(feedback.qualityRating as number)}
          <Badge variant="outline">
            <TestTube className="w-4 h-4 mr-1" />
            Testing Feedback
          </Badge>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Feedback Overview */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <MessageSquare className="w-5 h-5" />
                Feedback Information
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-4">
                <div>
                  <Label className="text-sm font-medium">Rating</Label>
                  <div className="flex items-center gap-2 mt-1">
                    <div className="flex items-center gap-1">{getRatingStars(feedback.overallRating || 0)}</div>
                    <span className="text-sm text-muted-foreground">({feedback.overallRating || 0}/5)</span>
                  </div>
                </div>

                {feedback.additionalNotes && (
                  <div>
                    <Label className="text-sm font-medium">Additional Notes</Label>
                    <div className="mt-1 p-3 bg-muted rounded-md">
                      <p className="text-sm whitespace-pre-wrap">{feedback.additionalNotes}</p>
                    </div>
                  </div>
                )}

                <div>
                  <Label className="text-sm font-medium">Would Recommend</Label>
                  <div className="flex items-center gap-2 mt-1">
                    {feedback.wouldRecommend ? (
                      <div className="flex items-center gap-1 text-green-600">
                        <ThumbsUp className="w-4 h-4" />
                        <span className="text-sm">Yes</span>
                      </div>
                    ) : (
                      <div className="flex items-center gap-1 text-red-600">
                        <ThumbsDown className="w-4 h-4" />
                        <span className="text-sm">No</span>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Feedback Form (if available) */}
          {feedback.feedbackForm && (
            <Card>
              <CardHeader>
                <CardTitle>Custom Feedback Form</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="p-3 bg-muted rounded-md">
                    <p className="text-sm">Form data: {feedback.feedbackData}</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          {/* Related Information */}
          <Card>
            <CardHeader>
              <CardTitle>Related Information</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {feedback.testingRequestId && (
                <div className="flex items-center justify-between p-3 border rounded-md">
                  <div>
                    <p className="font-medium">Testing Request</p>
                    <p className="text-sm text-muted-foreground">View the original testing request</p>
                  </div>
                  <Link href={`/dashboard/testing-lab/requests/${feedback.testingRequestId}`}>
                    <Button variant="outline" size="sm">
                      View Request
                    </Button>
                  </Link>
                </div>
              )}

              {feedback.sessionId && (
                <div className="flex items-center justify-between p-3 border rounded-md">
                  <div>
                    <p className="font-medium">Testing Session</p>
                    <p className="text-sm text-muted-foreground">View the testing session details</p>
                  </div>
                  <Link href={`/dashboard/testing-lab/sessions/${feedback.sessionId}`}>
                    <Button variant="outline" size="sm">
                      View Session
                    </Button>
                  </Link>
                </div>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Quick Actions */}
          <Card>
            <CardHeader>
              <CardTitle>Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Button onClick={handleReportFeedback} disabled={loading} variant="outline" className="w-full">
                {loading ? 'Reporting...' : 'Report Feedback'}
              </Button>

              <Button variant="outline" className="w-full" asChild>
                <Link href="/dashboard/testing-lab/feedback">Back to Feedback</Link>
              </Button>
            </CardContent>
          </Card>

          {/* Feedback Metadata */}
          <Card>
            <CardHeader>
              <CardTitle>Feedback Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Feedback ID:</span>
                  <span className="font-mono">{feedback.id}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">User ID:</span>
                  <span className="font-mono">{feedback.userId}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Submitted:</span>
                  <span>{feedback.createdAt ? formatDate(feedback.createdAt) : 'N/A'}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Updated:</span>
                  <span>{feedback.updatedAt ? formatDate(feedback.updatedAt) : 'N/A'}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Quality Score:</span>
                  <span>{feedback.qualityRating || 'Not rated'}</span>
                </div>
              </div>

              <Separator />

              <div className="space-y-2">
                <h4 className="text-sm font-medium">Feedback Details</h4>
                <div className="space-y-1 text-xs text-muted-foreground">
                  <div className="flex justify-between">
                    <span>Context:</span>
                    <span>{feedback.testingContext || 'N/A'}</span>
                  </div>
                  {feedback.isReported && (
                    <div className="flex justify-between">
                      <span>Status:</span>
                      <span className="text-red-600">Reported</span>
                    </div>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
