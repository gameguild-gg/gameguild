'use client';

import { useState, useEffect } from 'react';
import { Button } from '@game-guild/ui/components';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Textarea } from '@game-guild/ui/components';
import { Badge } from '@game-guild/ui/components';
import { Slider } from '@game-guild/ui/components';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@game-guild/ui/components';
import { Users, Star, AlertTriangle, CheckCircle, Clock, Flag, ThumbsUp, ThumbsDown, Loader2 } from 'lucide-react';
import { useToast } from '@/hooks/use-toast';
import { ReportButton } from '@/components/common/ReportButton';

interface PeerReviewProps {
  readonly submissionId: string;
  readonly submissionTitle: string;
  readonly submissionContent: string;
  readonly rubric?: ReviewCriteria[];
  readonly onReviewComplete?: (review: PeerReviewSubmission) => void;
}

interface ReviewCriteria {
  name: string;
  description: string;
  maxScore: number;
}

interface PeerReviewSubmission {
  score: number;
  feedback: string;
  criteriaScores: Record<string, number>;
  qualityRating: number;
}

interface ReceivedReview {
  id: string;
  reviewerId: string;
  reviewerName: string;
  score: number;
  feedback: string;
  submittedAt: string;
  isAccepted?: boolean;
}

export function PeerReview({ submissionId, submissionTitle, submissionContent, rubric, onReviewComplete }: PeerReviewProps) {
  const [score, setScore] = useState<number[]>([75]);
  const [feedback, setFeedback] = useState('');
  const [criteriaScores, setCriteriaScores] = useState<Record<string, number>>({});
  const [qualityRating, setQualityRating] = useState<number[]>([4]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const { toast } = useToast();

  useEffect(() => {
    // Initialize criteria scores
    if (rubric) {
      const initialScores: Record<string, number> = {};
      rubric.forEach(criteria => {
        initialScores[criteria.name] = Math.floor(criteria.maxScore * 0.75); // Default to 75%
      });
      setCriteriaScores(initialScores);
    }
  }, [rubric]);

  const handleCriteriaScoreChange = (criteriaName: string, value: number[]) => {
    setCriteriaScores(prev => ({
      ...prev,
      [criteriaName]: value[0]
    }));
  };

  const calculateOverallScore = () => {
    if (rubric && Object.keys(criteriaScores).length > 0) {
      const totalMaxScore = rubric.reduce((sum, criteria) => sum + criteria.maxScore, 0);
      const totalActualScore = Object.values(criteriaScores).reduce((sum, score) => sum + score, 0);
      return Math.round((totalActualScore / totalMaxScore) * 100);
    }
    return score[0];
  };

  const handleSubmitReview = async () => {
    const reviewData: PeerReviewSubmission = {
      score: calculateOverallScore(),
      feedback,
      criteriaScores,
      qualityRating: qualityRating[0],
    };

    setIsSubmitting(true);

    try {
      const response = await fetch('/api/peer-reviews', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          submissionId,
          ...reviewData,
        }),
      });

      if (response.ok) {
        toast({
          title: 'Review submitted',
          description: 'Your peer review has been submitted successfully.',
        });
        onReviewComplete?.(reviewData);
        setShowConfirmDialog(false);
      } else {
        throw new Error('Failed to submit review');
      }
    } catch (error) {
      console.error('Error submitting peer review:', error);
      toast({
        title: 'Submission failed',
        description: 'Failed to submit review. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const isValidReview = feedback.trim().length >= 10;

  return (
    <>
      <Card className="w-full">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2">
              <Users className="h-5 w-5" />
              Peer Review: {submissionTitle}
            </CardTitle>
            <Badge variant="outline" className="border-blue-500 text-blue-600">
              <Clock className="h-3 w-3 mr-1" />
              Review Assignment
            </Badge>
          </div>
        </CardHeader>
        <CardContent className="space-y-6">
          {/* Submission Content */}
          <div className="space-y-3">
            <h4 className="font-medium">Submission to Review:</h4>
            <div className="p-4 bg-gray-50 rounded-lg border">
              <p className="text-sm leading-relaxed">{submissionContent}</p>
            </div>
          </div>

          {/* Rubric-based Scoring */}
          {rubric && (
            <div className="space-y-4">
              <h4 className="font-medium">Evaluation Criteria:</h4>
              {rubric.map((criteria) => (
                <div key={criteria.name} className="space-y-2 p-3 border rounded-lg">
                  <div className="flex justify-between items-center">
                    <div>
                      <label className="text-sm font-medium">{criteria.name}</label>
                      <p className="text-xs text-gray-600">{criteria.description}</p>
                    </div>
                    <span className="text-sm font-mono">
                      {criteriaScores[criteria.name] || 0} / {criteria.maxScore}
                    </span>
                  </div>
                  <Slider
                    value={[criteriaScores[criteria.name] || 0]}
                    onValueChange={(value) => handleCriteriaScoreChange(criteria.name, value)}
                    max={criteria.maxScore}
                    step={1}
                    className="w-full"
                  />
                </div>
              ))}
            </div>
          )}

          {/* Overall Score (if no rubric) */}
          {!rubric && (
            <div className="space-y-3">
              <label className="text-sm font-medium">Overall Score (0-100)</label>
              <div className="space-y-2">
                <Slider
                  value={score}
                  onValueChange={setScore}
                  max={100}
                  step={1}
                  className="w-full"
                />
                <div className="text-center">
                  <span className="text-lg font-mono">{score[0]}/100</span>
                </div>
              </div>
            </div>
          )}

          {/* Calculated Score Display */}
          {rubric && (
            <div className="p-3 bg-blue-50 border border-blue-200 rounded-lg">
              <div className="flex justify-between items-center">
                <span className="font-medium">Calculated Overall Score:</span>
                <span className="text-lg font-mono text-blue-700">{calculateOverallScore()}/100</span>
              </div>
            </div>
          )}

          {/* Feedback */}
          <div className="space-y-3">
            <label className="text-sm font-medium">Detailed Feedback *</label>
            <Textarea
              placeholder="Provide constructive feedback about the submission. Be specific about strengths and areas for improvement..."
              value={feedback}
              onChange={(e) => setFeedback(e.target.value)}
              rows={6}
              className="resize-y"
            />
            <p className="text-xs text-gray-500">
              Minimum 10 characters required. Current: {feedback.length}
            </p>
          </div>

          {/* Review Quality Self-Assessment */}
          <div className="space-y-3">
            <label className="text-sm font-medium">Rate the quality of your review (1-5 stars)</label>
            <div className="flex items-center gap-4">
              <Slider
                value={qualityRating}
                onValueChange={setQualityRating}
                max={5}
                min={1}
                step={1}
                className="flex-1"
              />
              <div className="flex items-center gap-1">
                {Array.from({ length: 5 }, (_, i) => (
                  <Star
                    key={i}
                    className={`h-4 w-4 ${
                      i < qualityRating[0] ? 'text-yellow-500 fill-current' : 'text-gray-300'
                    }`}
                  />
                ))}
                <span className="ml-2 text-sm font-mono">{qualityRating[0]}/5</span>
              </div>
            </div>
          </div>

          {/* Submit Button */}
          <div className="flex items-center justify-between pt-4 border-t">
            <p className="text-xs text-gray-500">
              Your review will be sent to the student and may be used for grading.
            </p>
            <Button
              onClick={() => setShowConfirmDialog(true)}
              disabled={!isValidReview || isSubmitting}
              className="px-8"
            >
              Submit Review
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Confirmation Dialog */}
      <Dialog open={showConfirmDialog} onOpenChange={setShowConfirmDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Confirm Peer Review Submission</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="p-3 bg-gray-50 rounded-lg space-y-2">
              <div className="flex justify-between">
                <span>Overall Score:</span>
                <span className="font-mono">{calculateOverallScore()}/100</span>
              </div>
              <div className="flex justify-between">
                <span>Review Quality:</span>
                <div className="flex items-center gap-1">
                  {Array.from({ length: qualityRating[0] }, (_, i) => (
                    <Star key={i} className="h-3 w-3 text-yellow-500 fill-current" />
                  ))}
                </div>
              </div>
            </div>
            <p className="text-sm text-gray-600">
              Are you sure you want to submit this peer review? Once submitted, it cannot be edited.
            </p>
            <div className="flex gap-2 justify-end">
              <Button
                variant="outline"
                onClick={() => setShowConfirmDialog(false)}
                disabled={isSubmitting}
              >
                Cancel
              </Button>
              <Button onClick={handleSubmitReview} disabled={isSubmitting}>
                {isSubmitting && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                Submit Review
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}

/* Component for displaying received peer reviews */
export function ReceivedPeerReviews({ submissionId }: { submissionId: string }) {
  const [reviews, setReviews] = useState<ReceivedReview[]>([]);
  const [loading, setLoading] = useState(true);
  const { toast } = useToast();

  useEffect(() => {
    loadReviews();
  }, [submissionId]);

  const loadReviews = async () => {
    try {
      const response = await fetch(`/api/peer-reviews/${submissionId}`);
      if (response.ok) {
        const data = await response.json();
        setReviews(data.reviews || []);
      }
    } catch (error) {
      console.error('Error loading reviews:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleAcceptReview = async (reviewId: string) => {
    try {
      const response = await fetch(`/api/peer-reviews/${reviewId}/accept`, {
        method: 'POST',
      });

      if (response.ok) {
        setReviews(prev => 
          prev.map(review => 
            review.id === reviewId ? { ...review, isAccepted: true } : review
          )
        );
        toast({
          title: 'Review accepted',
          description: 'You have accepted this peer review.',
        });
      }
    } catch (error) {
      console.error('Error accepting review:', error);
      toast({
        title: 'Error',
        description: 'Failed to accept review.',
        variant: 'destructive',
      });
    }
  };

  const handleRejectReview = async (reviewId: string) => {
    try {
      const response = await fetch(`/api/peer-reviews/${reviewId}/reject`, {
        method: 'POST',
      });

      if (response.ok) {
        setReviews(prev => 
          prev.map(review => 
            review.id === reviewId ? { ...review, isAccepted: false } : review
          )
        );
        toast({
          title: 'Review rejected',
          description: 'You have rejected this review. A new reviewer will be assigned.',
        });
      }
    } catch (error) {
      console.error('Error rejecting review:', error);
      toast({
        title: 'Error',
        description: 'Failed to reject review.',
        variant: 'destructive',
      });
    }
  };

  if (loading) {
    return (
      <Card>
        <CardContent className="p-6 text-center">
          <Loader2 className="h-6 w-6 animate-spin mx-auto mb-2" />
          <p>Loading peer reviews...</p>
        </CardContent>
      </Card>
    );
  }

  if (reviews.length === 0) {
    return (
      <Card>
        <CardContent className="p-6 text-center text-gray-500">
          <Users className="h-8 w-8 mx-auto mb-2" />
          <p>No peer reviews available yet.</p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      <h3 className="text-lg font-semibold">Peer Reviews Received</h3>
      {reviews.map((review) => (
        <Card key={review.id}>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <span className="font-medium">Review by {review.reviewerName}</span>
                <Badge variant="outline">Score: {review.score}/100</Badge>
              </div>
              <div className="flex items-center gap-2">
                {review.isAccepted === true && (
                  <Badge className="bg-green-500">
                    <CheckCircle className="h-3 w-3 mr-1" />
                    Accepted
                  </Badge>
                )}
                {review.isAccepted === false && (
                  <Badge variant="destructive">
                    <AlertTriangle className="h-3 w-3 mr-1" />
                    Rejected
                  </Badge>
                )}
                <ReportButton
                  reportType="review"
                  targetId={review.id}
                  targetTitle={`Review by ${review.reviewerName}`}
                  variant="ghost"
                  size="sm"
                />
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="p-3 bg-gray-50 rounded-lg">
              <p className="text-sm leading-relaxed">{review.feedback}</p>
            </div>
            
            {review.isAccepted === undefined && (
              <div className="flex gap-2 justify-end">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => handleRejectReview(review.id)}
                  className="text-red-600 border-red-300 hover:bg-red-50"
                >
                  <ThumbsDown className="h-4 w-4 mr-1" />
                  Reject Review
                </Button>
                <Button
                  size="sm"
                  onClick={() => handleAcceptReview(review.id)}
                  className="bg-green-600 hover:bg-green-700"
                >
                  <ThumbsUp className="h-4 w-4 mr-1" />
                  Accept Review
                </Button>
              </div>
            )}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
