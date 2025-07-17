'use client';

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Button } from '@game-guild/ui/components/button';
import { Badge } from '@game-guild/ui/components/badge';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Input } from '@game-guild/ui/components/input';
import { Progress } from '@game-guild/ui/components/progress';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@game-guild/ui/components/dialog';
import { Star, User, Clock, Eye, MessageSquare, ThumbsUp, ThumbsDown, Send, FileText, Download, Upload } from 'lucide-react';

interface PeerReviewSubmission {
  id: string;
  title: string;
  description: string;
  submittedBy: string;
  submittedAt: string;
  activityType: 'assignment' | 'project' | 'discussion';
  fileUrl?: string;
  content?: string;
  reviewsReceived: number;
  averageRating: number;
  status: 'pending' | 'in-review' | 'completed';
}

interface PeerReview {
  id: string;
  submissionId: string;
  reviewerId: string;
  reviewerName: string;
  rating: number;
  feedback: string;
  createdAt: string;
  criteria: {
    [key: string]: number;
  };
}

interface ReviewCriteria {
  name: string;
  description: string;
  weight: number;
}

const mockSubmissions: PeerReviewSubmission[] = [
  {
    id: 'sub-1',
    title: 'Game Design Document - RPG Adventure',
    description: 'A comprehensive design document for a fantasy RPG with character progression and quest systems.',
    submittedBy: 'Alex Thompson',
    submittedAt: '2024-01-20T10:30:00Z',
    activityType: 'assignment',
    fileUrl: '/submissions/game-design-doc-1.pdf',
    reviewsReceived: 2,
    averageRating: 4.5,
    status: 'in-review',
  },
  {
    id: 'sub-2',
    title: 'Unity Prototype - Platformer Game',
    description: 'A working prototype of a 2D platformer with basic mechanics and three levels.',
    submittedBy: 'Maria Garcia',
    submittedAt: '2024-01-19T14:15:00Z',
    activityType: 'project',
    fileUrl: '/submissions/platformer-prototype.zip',
    reviewsReceived: 3,
    averageRating: 4.2,
    status: 'completed',
  },
  {
    id: 'sub-3',
    title: 'Discussion: Best Practices for Game Monetization',
    description: 'Analysis of various monetization strategies in mobile games and their ethical implications.',
    submittedBy: 'James Wilson',
    submittedAt: '2024-01-18T09:45:00Z',
    activityType: 'discussion',
    content: 'In this discussion, I want to explore the balance between profitable monetization and player satisfaction...',
    reviewsReceived: 1,
    averageRating: 3.8,
    status: 'pending',
  },
];

const mockReviews: PeerReview[] = [
  {
    id: 'rev-1',
    submissionId: 'sub-1',
    reviewerId: 'reviewer-1',
    reviewerName: 'Sarah Johnson',
    rating: 4,
    feedback:
      'Excellent documentation structure and clear writing. The character progression system is well thought out. Consider adding more detail about the combat mechanics.',
    createdAt: '2024-01-21T11:00:00Z',
    criteria: {
      clarity: 5,
      completeness: 4,
      creativity: 4,
      technical: 3,
    },
  },
  {
    id: 'rev-2',
    submissionId: 'sub-1',
    reviewerId: 'reviewer-2',
    reviewerName: 'Mike Chen',
    rating: 5,
    feedback: 'Outstanding work! The quest system design is innovative and the document is very comprehensive. This would be ready for development.',
    createdAt: '2024-01-21T15:30:00Z',
    criteria: {
      clarity: 5,
      completeness: 5,
      creativity: 5,
      technical: 4,
    },
  },
];

const reviewCriteria: ReviewCriteria[] = [
  { name: 'clarity', description: 'How clear and understandable is the submission?', weight: 0.3 },
  { name: 'completeness', description: 'Does the submission meet all requirements?', weight: 0.25 },
  { name: 'creativity', description: 'How original and innovative is the work?', weight: 0.25 },
  { name: 'technical', description: 'Technical quality and implementation', weight: 0.2 },
];

export function PeerReviewInterface() {
  const [submissions] = useState<PeerReviewSubmission[]>(mockSubmissions);
  const [reviews] = useState<PeerReview[]>(mockReviews);
  const [selectedSubmission, setSelectedSubmission] = useState<PeerReviewSubmission | null>(null);
  const [showReviewDialog, setShowReviewDialog] = useState(false);
  const [currentReview, setCurrentReview] = useState({
    rating: 0,
    feedback: '',
    criteria: {} as { [key: string]: number },
  });

  const handleSubmitReview = () => {
    if (!selectedSubmission) return;

    // Calculate weighted average rating
    const weightedRating = reviewCriteria.reduce((sum, criterion) => {
      return sum + (currentReview.criteria[criterion.name] || 0) * criterion.weight;
    }, 0);

    console.log('Submitting review:', {
      submissionId: selectedSubmission.id,
      rating: Math.round(weightedRating),
      feedback: currentReview.feedback,
      criteria: currentReview.criteria,
    });

    // Reset form
    setCurrentReview({
      rating: 0,
      feedback: '',
      criteria: {},
    });
    setShowReviewDialog(false);
    setSelectedSubmission(null);
  };

  const handleCriteriaRating = (criteriaName: string, rating: number) => {
    setCurrentReview((prev) => ({
      ...prev,
      criteria: {
        ...prev.criteria,
        [criteriaName]: rating,
      },
    }));
  };

  const getSubmissionReviews = (submissionId: string) => {
    return reviews.filter((review) => review.submissionId === submissionId);
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case 'completed':
        return <Badge className="bg-green-500 text-white">Completed</Badge>;
      case 'in-review':
        return <Badge className="bg-blue-500 text-white">In Review</Badge>;
      case 'pending':
        return <Badge variant="outline">Pending</Badge>;
      default:
        return null;
    }
  };

  const renderStars = (rating: number, onRate?: (rating: number) => void) => {
    return (
      <div className="flex gap-1">
        {[1, 2, 3, 4, 5].map((star) => (
          <Star
            key={star}
            className={`h-4 w-4 cursor-pointer ${star <= rating ? 'fill-yellow-400 text-yellow-400' : 'text-gray-400'}`}
            onClick={() => onRate && onRate(star)}
          />
        ))}
      </div>
    );
  };

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      <div className="container mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-3xl font-bold mb-2">Peer Review System</h1>
          <p className="text-gray-400">Review and provide feedback on peer submissions</p>
        </div>

        {/* Statistics */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
          <Card className="bg-gray-900 border-gray-800">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <FileText className="h-8 w-8 text-blue-400" />
                <div>
                  <p className="text-2xl font-bold">{submissions.length}</p>
                  <p className="text-sm text-gray-400">Total Submissions</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-gray-900 border-gray-800">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <Clock className="h-8 w-8 text-yellow-400" />
                <div>
                  <p className="text-2xl font-bold">{submissions.filter((s) => s.status === 'pending').length}</p>
                  <p className="text-sm text-gray-400">Pending Reviews</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-gray-900 border-gray-800">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <MessageSquare className="h-8 w-8 text-green-400" />
                <div>
                  <p className="text-2xl font-bold">{reviews.length}</p>
                  <p className="text-sm text-gray-400">Reviews Given</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card className="bg-gray-900 border-gray-800">
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <Star className="h-8 w-8 text-purple-400" />
                <div>
                  <p className="text-2xl font-bold">{submissions.reduce((sum, s) => sum + s.averageRating, 0) / submissions.length || 0}</p>
                  <p className="text-sm text-gray-400">Avg Rating</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Submissions List */}
        <div className="space-y-6">
          {submissions.map((submission) => {
            const submissionReviews = getSubmissionReviews(submission.id);
            return (
              <Card key={submission.id} className="bg-gray-900 border-gray-800">
                <CardHeader>
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <CardTitle className="text-lg mb-2">{submission.title}</CardTitle>
                      <div className="flex items-center gap-4 text-sm text-gray-400 mb-2">
                        <span className="flex items-center gap-1">
                          <User className="h-4 w-4" />
                          {submission.submittedBy}
                        </span>
                        <span className="flex items-center gap-1">
                          <Clock className="h-4 w-4" />
                          {new Date(submission.submittedAt).toLocaleDateString()}
                        </span>
                        <span className="capitalize">{submission.activityType}</span>
                      </div>
                      <p className="text-gray-300 mb-4">{submission.description}</p>
                    </div>
                    <div className="flex flex-col items-end gap-2">
                      {getStatusBadge(submission.status)}
                      <div className="flex items-center gap-2">
                        {renderStars(Math.round(submission.averageRating))}
                        <span className="text-sm text-gray-400">({submission.reviewsReceived})</span>
                      </div>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-4">
                      {submission.fileUrl && (
                        <Button variant="outline" size="sm">
                          <Download className="h-4 w-4 mr-2" />
                          Download
                        </Button>
                      )}
                      <Button variant="outline" size="sm">
                        <Eye className="h-4 w-4 mr-2" />
                        View Details
                      </Button>
                    </div>
                    <div className="flex gap-2">
                      <Dialog>
                        <DialogTrigger asChild>
                          <Button variant="outline" size="sm">
                            <MessageSquare className="h-4 w-4 mr-2" />
                            View Reviews ({submissionReviews.length})
                          </Button>
                        </DialogTrigger>
                        <DialogContent className="max-w-2xl">
                          <DialogHeader>
                            <DialogTitle>Reviews for "{submission.title}"</DialogTitle>
                          </DialogHeader>
                          <div className="space-y-4 max-h-96 overflow-y-auto">
                            {submissionReviews.map((review) => (
                              <Card key={review.id} className="bg-gray-800">
                                <CardContent className="p-4">
                                  <div className="flex items-start justify-between mb-3">
                                    <div>
                                      <p className="font-medium">{review.reviewerName}</p>
                                      <p className="text-sm text-gray-400">{new Date(review.createdAt).toLocaleDateString()}</p>
                                    </div>
                                    <div className="flex items-center gap-2">
                                      {renderStars(review.rating)}
                                      <span className="text-sm">({review.rating}/5)</span>
                                    </div>
                                  </div>
                                  <p className="text-gray-300 mb-3">{review.feedback}</p>
                                  <div className="grid grid-cols-2 gap-2 text-sm">
                                    {Object.entries(review.criteria).map(([criterion, rating]) => (
                                      <div key={criterion} className="flex justify-between">
                                        <span className="capitalize text-gray-400">{criterion}:</span>
                                        <span>{rating}/5</span>
                                      </div>
                                    ))}
                                  </div>
                                </CardContent>
                              </Card>
                            ))}
                            {submissionReviews.length === 0 && <p className="text-center text-gray-400 py-4">No reviews yet</p>}
                          </div>
                        </DialogContent>
                      </Dialog>
                      <Button
                        onClick={() => {
                          setSelectedSubmission(submission);
                          setShowReviewDialog(true);
                        }}
                        disabled={submission.status === 'completed'}
                      >
                        <MessageSquare className="h-4 w-4 mr-2" />
                        Add Review
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>

        {/* Review Dialog */}
        <Dialog open={showReviewDialog} onOpenChange={setShowReviewDialog}>
          <DialogContent className="max-w-2xl">
            <DialogHeader>
              <DialogTitle>Review Submission</DialogTitle>
            </DialogHeader>
            {selectedSubmission && (
              <div className="space-y-6">
                <div>
                  <h3 className="font-semibold mb-2">{selectedSubmission.title}</h3>
                  <p className="text-sm text-gray-400 mb-4">{selectedSubmission.description}</p>
                </div>

                {/* Review Criteria */}
                <div className="space-y-4">
                  <h4 className="font-medium">Review Criteria</h4>
                  {reviewCriteria.map((criterion) => (
                    <div key={criterion.name} className="space-y-2">
                      <div className="flex justify-between items-center">
                        <label className="text-sm font-medium capitalize">{criterion.name}</label>
                        <div className="flex items-center gap-2">
                          {renderStars(currentReview.criteria[criterion.name] || 0, (rating) => handleCriteriaRating(criterion.name, rating))}
                          <span className="text-sm text-gray-400">({currentReview.criteria[criterion.name] || 0}/5)</span>
                        </div>
                      </div>
                      <p className="text-xs text-gray-500">{criterion.description}</p>
                    </div>
                  ))}
                </div>

                {/* Overall Rating Display */}
                <div className="bg-gray-800 p-3 rounded-lg">
                  <div className="flex justify-between items-center">
                    <span className="font-medium">Calculated Overall Rating:</span>
                    <span className="text-lg font-bold">
                      {reviewCriteria
                        .reduce((sum, criterion) => {
                          return sum + (currentReview.criteria[criterion.name] || 0) * criterion.weight;
                        }, 0)
                        .toFixed(1)}
                      /5
                    </span>
                  </div>
                </div>

                {/* Feedback */}
                <div className="space-y-2">
                  <label className="text-sm font-medium">Written Feedback</label>
                  <Textarea
                    placeholder="Provide constructive feedback about this submission..."
                    value={currentReview.feedback}
                    onChange={(e) =>
                      setCurrentReview((prev) => ({
                        ...prev,
                        feedback: e.target.value,
                      }))
                    }
                    rows={4}
                    className="bg-gray-800 border-gray-700"
                  />
                </div>

                {/* Submit Button */}
                <div className="flex justify-end gap-2">
                  <Button variant="outline" onClick={() => setShowReviewDialog(false)}>
                    Cancel
                  </Button>
                  <Button
                    onClick={handleSubmitReview}
                    disabled={!currentReview.feedback.trim() || reviewCriteria.some((criterion) => !currentReview.criteria[criterion.name])}
                  >
                    <Send className="h-4 w-4 mr-2" />
                    Submit Review
                  </Button>
                </div>
              </div>
            )}
          </DialogContent>
        </Dialog>
      </div>
    </div>
  );
}
