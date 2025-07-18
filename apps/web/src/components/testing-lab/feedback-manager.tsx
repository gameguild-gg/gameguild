'use client';

import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { CheckCircle, Eye, Filter, Flag, MessageSquare, Star, ThumbsDown, ThumbsUp, XCircle } from 'lucide-react';

interface TestingFeedback {
  id: string;
  testingRequest: {
    id: string;
    title: string;
    projectVersion: {
      versionNumber: string;
      project: {
        title: string;
      };
    };
  };
  user: {
    id: string;
    name: string;
    email: string;
  };
  feedbackData: {
    rating: number;
    responses: Record<string, string>;
    generalComments?: string;
  };
  testingContext: 'session' | 'individual';
  submittedAt: string;
  reviewStatus: 'pending' | 'approved' | 'flagged' | 'rejected';
  qualityRating?: 'positive' | 'negative' | 'neutral';
  developerResponse?: string;
  session?: {
    id: string;
    sessionName: string;
    sessionDate: string;
  };
}

interface FeedbackStats {
  total: number;
  pending: number;
  approved: number;
  flagged: number;
  rejected: number;
  averageRating: number;
}

export function FeedbackManager() {
  const [feedback, setFeedback] = useState<TestingFeedback[]>([]);
  const [stats, setStats] = useState<FeedbackStats>({
    total: 0,
    pending: 0,
    approved: 0,
    flagged: 0,
    rejected: 0,
    averageRating: 0,
  });
  const [selectedFeedback, setSelectedFeedback] = useState<TestingFeedback | null>(null);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<'all' | 'pending' | 'approved' | 'flagged' | 'rejected'>('all');
  const [sortBy, setSortBy] = useState<'date' | 'rating' | 'project'>('date');

  useEffect(() => {
    fetchFeedback();
  }, [filter, sortBy]);

  const fetchFeedback = async () => {
    try {
      setLoading(true);
      // Mock data - replace with actual API call
      const mockFeedback: TestingFeedback[] = [
        {
          id: 'fb1',
          testingRequest: {
            id: 'req1',
            title: 'Space Adventure v1.2 Testing',
            projectVersion: {
              versionNumber: '1.2.0',
              project: { title: 'Space Adventure' },
            },
          },
          user: {
            id: 'user1',
            name: 'John Doe',
            email: 'john.doe@mymail.champlain.edu',
          },
          feedbackData: {
            rating: 4,
            responses: {
              'How was the difficulty level?': 'The difficulty was well-balanced. Not too easy, not too hard.',
              'Were the controls intuitive?': 'Yes, the controls felt natural after a few minutes of play.',
              'What did you like most about the game?': 'The art style and the puzzle mechanics are really engaging.',
              'What could be improved?': 'Some sound effects are missing and the tutorial could be clearer.',
            },
            generalComments:
              "Overall, this is a solid game with great potential. The core mechanics work well and it's fun to play. Looking forward to the next version!",
          },
          testingContext: 'session',
          submittedAt: '2024-12-15T14:30:00Z',
          reviewStatus: 'pending',
          session: {
            id: 'session1',
            sessionName: 'Block 3 Testing Session',
            sessionDate: '2024-12-15',
          },
        },
        {
          id: 'fb2',
          testingRequest: {
            id: 'req2',
            title: 'Racing Game v2.1 Testing',
            projectVersion: {
              versionNumber: '2.1.0',
              project: { title: 'Racing Game' },
            },
          },
          user: {
            id: 'user2',
            name: 'Jane Smith',
            email: 'jane.smith@mymail.champlain.edu',
          },
          feedbackData: {
            rating: 2,
            responses: {
              'How was the performance?': 'The game lagged significantly during races.',
              'How were the graphics?': 'Graphics look good but the frame rate drops hurt the experience.',
              'What needs improvement?': 'Optimize performance and fix collision detection bugs.',
            },
          },
          testingContext: 'individual',
          submittedAt: '2024-12-14T16:45:00Z',
          reviewStatus: 'approved',
          qualityRating: 'positive',
        },
      ];

      setFeedback(mockFeedback);

      // Calculate stats
      const total = mockFeedback.length;
      const pending = mockFeedback.filter((f) => f.reviewStatus === 'pending').length;
      const approved = mockFeedback.filter((f) => f.reviewStatus === 'approved').length;
      const flagged = mockFeedback.filter((f) => f.reviewStatus === 'flagged').length;
      const rejected = mockFeedback.filter((f) => f.reviewStatus === 'rejected').length;
      const averageRating = mockFeedback.reduce((sum, f) => sum + f.feedbackData.rating, 0) / total;

      setStats({ total, pending, approved, flagged, rejected, averageRating });
    } catch (error) {
      console.error('Error fetching feedback:', error);
    } finally {
      setLoading(false);
    }
  };

  const updateFeedbackStatus = async (feedbackId: string, status: 'approved' | 'flagged' | 'rejected', qualityRating?: 'positive' | 'negative') => {
    try {
      // API call would go here
      setFeedback((prev) => prev.map((f) => (f.id === feedbackId ? { ...f, reviewStatus: status, qualityRating } : f)));
    } catch (error) {
      console.error('Error updating feedback status:', error);
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'pending':
        return 'bg-yellow-600/20 text-yellow-400';
      case 'approved':
        return 'bg-green-600/20 text-green-400';
      case 'flagged':
        return 'bg-orange-600/20 text-orange-400';
      case 'rejected':
        return 'bg-red-600/20 text-red-400';
      default:
        return 'bg-gray-600/20 text-gray-400';
    }
  };

  const getQualityColor = (quality?: string) => {
    switch (quality) {
      case 'positive':
        return 'text-green-400';
      case 'negative':
        return 'text-red-400';
      case 'neutral':
        return 'text-gray-400';
      default:
        return 'text-gray-500';
    }
  };

  const renderStars = (rating: number) => {
    return Array.from({ length: 5 }).map((_, i) => <Star key={i} className={`h-4 w-4 ${i < rating ? 'text-yellow-400 fill-yellow-400' : 'text-gray-400'}`} />);
  };

  const filteredFeedback = feedback.filter((f) => filter === 'all' || f.reviewStatus === filter);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-3xl font-bold text-white">Feedback Management</h1>
          <p className="text-slate-400 mt-1">Review and manage testing feedback from students</p>
        </div>
        <div className="flex gap-3">
          <Select value={filter} onValueChange={(value: any) => setFilter(value)}>
            <SelectTrigger className="w-40 bg-slate-700 border-slate-600 text-white">
              <Filter className="h-4 w-4 mr-2" />
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Feedback</SelectItem>
              <SelectItem value="pending">Pending Review</SelectItem>
              <SelectItem value="approved">Approved</SelectItem>
              <SelectItem value="flagged">Flagged</SelectItem>
              <SelectItem value="rejected">Rejected</SelectItem>
            </SelectContent>
          </Select>
          <Select value={sortBy} onValueChange={(value: any) => setSortBy(value)}>
            <SelectTrigger className="w-40 bg-slate-700 border-slate-600 text-white">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="date">Sort by Date</SelectItem>
              <SelectItem value="rating">Sort by Rating</SelectItem>
              <SelectItem value="project">Sort by Project</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-6 gap-4">
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-white">{stats.total}</div>
              <div className="text-sm text-slate-400">Total</div>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-yellow-400">{stats.pending}</div>
              <div className="text-sm text-slate-400">Pending</div>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-green-400">{stats.approved}</div>
              <div className="text-sm text-slate-400">Approved</div>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-orange-400">{stats.flagged}</div>
              <div className="text-sm text-slate-400">Flagged</div>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-red-400">{stats.rejected}</div>
              <div className="text-sm text-slate-400">Rejected</div>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-blue-400">{stats.averageRating.toFixed(1)}</div>
              <div className="text-sm text-slate-400">Avg Rating</div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Feedback List */}
      <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
        <CardHeader>
          <CardTitle className="text-white flex items-center gap-2">
            <MessageSquare className="h-5 w-5" />
            Feedback Submissions
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <Table>
            <TableHeader>
              <TableRow className="border-slate-700">
                <TableHead className="text-slate-300">Project</TableHead>
                <TableHead className="text-slate-300">Tester</TableHead>
                <TableHead className="text-slate-300">Rating</TableHead>
                <TableHead className="text-slate-300">Context</TableHead>
                <TableHead className="text-slate-300">Status</TableHead>
                <TableHead className="text-slate-300">Quality</TableHead>
                <TableHead className="text-slate-300">Submitted</TableHead>
                <TableHead className="text-slate-300">Actions</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {filteredFeedback.map((item) => (
                <TableRow key={item.id} className="border-slate-700">
                  <TableCell className="text-white">
                    <div>
                      <div className="font-medium">{item.testingRequest.projectVersion.project.title}</div>
                      <div className="text-sm text-slate-400">v{item.testingRequest.projectVersion.versionNumber}</div>
                    </div>
                  </TableCell>
                  <TableCell className="text-slate-300">
                    <div>
                      <div>{item.user.name}</div>
                      <div className="text-sm text-slate-400">{item.user.email}</div>
                    </div>
                  </TableCell>
                  <TableCell>
                    <div className="flex items-center gap-1">
                      {renderStars(item.feedbackData.rating)}
                      <span className="ml-2 text-slate-300">{item.feedbackData.rating}/5</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline" className="text-slate-300">
                      {item.testingContext}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <Badge className={getStatusColor(item.reviewStatus)}>{item.reviewStatus}</Badge>
                  </TableCell>
                  <TableCell>
                    {item.qualityRating ? (
                      <div className={`flex items-center gap-1 ${getQualityColor(item.qualityRating)}`}>
                        {item.qualityRating === 'positive' ? <ThumbsUp className="h-4 w-4" /> : <ThumbsDown className="h-4 w-4" />}
                        <span className="capitalize">{item.qualityRating}</span>
                      </div>
                    ) : (
                      <span className="text-slate-500">-</span>
                    )}
                  </TableCell>
                  <TableCell className="text-slate-400">{new Date(item.submittedAt).toLocaleDateString()}</TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      <Button size="sm" variant="outline" onClick={() => setSelectedFeedback(item)} className="h-8 w-8 p-0">
                        <Eye className="h-4 w-4" />
                      </Button>
                      {item.reviewStatus === 'pending' && (
                        <>
                          <Button size="sm" variant="outline" onClick={() => updateFeedbackStatus(item.id, 'approved', 'positive')} className="h-8 w-8 p-0">
                            <CheckCircle className="h-4 w-4 text-green-500" />
                          </Button>
                          <Button size="sm" variant="outline" onClick={() => updateFeedbackStatus(item.id, 'flagged')} className="h-8 w-8 p-0">
                            <Flag className="h-4 w-4 text-orange-500" />
                          </Button>
                          <Button size="sm" variant="outline" onClick={() => updateFeedbackStatus(item.id, 'rejected', 'negative')} className="h-8 w-8 p-0">
                            <XCircle className="h-4 w-4 text-red-500" />
                          </Button>
                        </>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      {/* Feedback Detail Dialog */}
      {selectedFeedback && (
        <Dialog open={!!selectedFeedback} onOpenChange={() => setSelectedFeedback(null)}>
          <DialogContent className="bg-slate-800 border-slate-700 text-white max-w-4xl max-h-[80vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <MessageSquare className="h-5 w-5" />
                Feedback Details
              </DialogTitle>
              <DialogDescription className="text-slate-400">
                {selectedFeedback.testingRequest.projectVersion.project.title} v{selectedFeedback.testingRequest.projectVersion.versionNumber} - by{' '}
                {selectedFeedback.user.name}
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-6">
              {/* Header Info */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <p className="text-sm text-slate-400">Overall Rating</p>
                  <div className="flex items-center gap-2">
                    {renderStars(selectedFeedback.feedbackData.rating)}
                    <span className="text-white text-lg">{selectedFeedback.feedbackData.rating}/5</span>
                  </div>
                </div>
                <div className="space-y-2">
                  <p className="text-sm text-slate-400">Testing Context</p>
                  <Badge variant="outline" className="text-slate-300">
                    {selectedFeedback.testingContext}
                    {selectedFeedback.session && ` - ${selectedFeedback.session.sessionName}`}
                  </Badge>
                </div>
              </div>

              {/* Feedback Responses */}
              <div className="space-y-4">
                <h3 className="text-lg font-semibold text-white">Feedback Responses</h3>
                {Object.entries(selectedFeedback.feedbackData.responses).map(([question, answer]) => (
                  <div key={question} className="space-y-2">
                    <p className="text-sm font-medium text-slate-300">{question}</p>
                    <div className="bg-slate-700/30 p-3 rounded-lg">
                      <p className="text-white">{answer}</p>
                    </div>
                  </div>
                ))}
              </div>

              {/* General Comments */}
              {selectedFeedback.feedbackData.generalComments && (
                <div className="space-y-2">
                  <h3 className="text-lg font-semibold text-white">General Comments</h3>
                  <div className="bg-slate-700/30 p-4 rounded-lg">
                    <p className="text-white">{selectedFeedback.feedbackData.generalComments}</p>
                  </div>
                </div>
              )}

              {/* Developer Response */}
              {selectedFeedback.developerResponse && (
                <div className="space-y-2">
                  <h3 className="text-lg font-semibold text-white">Developer Response</h3>
                  <div className="bg-blue-900/20 border border-blue-600/30 p-4 rounded-lg">
                    <p className="text-blue-300">{selectedFeedback.developerResponse}</p>
                  </div>
                </div>
              )}

              {/* Review Actions */}
              {selectedFeedback.reviewStatus === 'pending' && (
                <div className="space-y-3">
                  <h3 className="text-lg font-semibold text-white">Review Actions</h3>
                  <div className="flex gap-3">
                    <Button onClick={() => updateFeedbackStatus(selectedFeedback.id, 'approved', 'positive')} className="bg-green-600 hover:bg-green-700">
                      <ThumbsUp className="h-4 w-4 mr-2" />
                      Approve (Positive)
                    </Button>
                    <Button onClick={() => updateFeedbackStatus(selectedFeedback.id, 'approved', 'neutral')} variant="outline">
                      <CheckCircle className="h-4 w-4 mr-2" />
                      Approve (Neutral)
                    </Button>
                    <Button onClick={() => updateFeedbackStatus(selectedFeedback.id, 'flagged')} className="bg-orange-600 hover:bg-orange-700">
                      <Flag className="h-4 w-4 mr-2" />
                      Flag for Review
                    </Button>
                    <Button onClick={() => updateFeedbackStatus(selectedFeedback.id, 'rejected', 'negative')} variant="destructive">
                      <XCircle className="h-4 w-4 mr-2" />
                      Reject
                    </Button>
                  </div>
                </div>
              )}
            </div>
          </DialogContent>
        </Dialog>
      )}
    </div>
  );
}
