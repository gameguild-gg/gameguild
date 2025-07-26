'use client';

import React, { useState, useEffect, useMemo } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { MessageSquare, Eye, Flag, ThumbsUp, ThumbsDown, Star, Search } from 'lucide-react';
import type { TestingFeedback } from '@/lib/api/generated/types.gen';

interface TestingFeedbackListProps {
  data: {
    testingFeedbacks: TestingFeedback[];
    total: number;
  };
}

export function TestingFeedbackList({ data }: TestingFeedbackListProps) {
  const [feedback, setFeedback] = useState<TestingFeedback[]>(data.testingFeedbacks || []);
  const [selectedFeedback, setSelectedFeedback] = useState<TestingFeedback | null>(null);
  const [loading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');

  // Update feedback when data prop changes
  useEffect(() => {
    setFeedback(data.testingFeedbacks || []);
  }, [data]);

  // Helper functions for rendering
  const renderStars = (rating: number | null | undefined) => {
    const stars = [];
    const ratingValue = rating || 0;
    for (let i = 1; i <= 5; i++) {
      stars.push(<Star key={i} className={`h-4 w-4 ${i <= ratingValue ? 'text-yellow-400 fill-current' : 'text-slate-300'}`} />);
    }
    return stars;
  };

  const getStatusColor = (isReported: boolean | undefined) => {
    return isReported ? 'bg-red-100 text-red-800' : 'bg-green-100 text-green-800';
  };

  const updateFeedbackStatus = async (id: string, action: string) => {
    try {
      console.log(`Updating feedback ${id} with action: ${action}`);
      // TODO: Implement API call to update feedback status
      setSelectedFeedback(null);
    } catch (error) {
      console.error('Error updating feedback status:', error);
    }
  };

  // Statistics
  const stats = useMemo(() => {
    const total = feedback.length;
    const reported = feedback.filter((f) => f.isReported).length;
    const active = total - reported;
    const averageRating = feedback.length > 0 ? feedback.reduce((sum, f) => sum + (f.overallRating || 0), 0) / feedback.length : 0;

    return {
      total,
      active,
      reported,
      averageRating,
    };
  }, [feedback]);

  // Filtering logic
  const filteredFeedback = feedback.filter((item) => {
    if (searchTerm) {
      const lowerSearchTerm = searchTerm.toLowerCase();
      const matchesSearch =
        item.testingRequest?.title?.toLowerCase().includes(lowerSearchTerm) ||
        item.additionalNotes?.toLowerCase().includes(lowerSearchTerm) ||
        item.id?.toString().includes(lowerSearchTerm);

      if (!matchesSearch) {
        return false;
      }
    }
    return true;
  });

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
          <h1 className="text-3xl font-bold">Feedback Management</h1>
          <p className="text-muted-foreground mt-1">Review and manage testing feedback from testers</p>
        </div>
      </div>

      {/* Search */}
      <div className="flex items-center gap-4">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
          <Input placeholder="Search feedback..." value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} className="pl-9" />
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card>
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold">{stats.total}</div>
              <div className="text-sm text-muted-foreground">Total</div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-green-600">{stats.active}</div>
              <div className="text-sm text-muted-foreground">Active</div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-red-600">{stats.reported}</div>
              <div className="text-sm text-muted-foreground">Reported</div>
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="p-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-blue-600">{stats.averageRating.toFixed(1)}</div>
              <div className="text-sm text-muted-foreground">Avg Rating</div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Feedback List */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MessageSquare className="h-5 w-5" />
            Feedback Submissions ({filteredFeedback.length})
          </CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          {filteredFeedback.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">No feedback found.</div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Feedback ID</TableHead>
                  <TableHead>Testing Request</TableHead>
                  <TableHead>Rating</TableHead>
                  <TableHead>Recommendation</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Date</TableHead>
                  <TableHead>Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredFeedback.map((item) => (
                  <TableRow key={item.id}>
                    <TableCell>
                      <div className="font-mono text-sm">{item.id}</div>
                    </TableCell>
                    <TableCell>
                      <div className="font-medium">{item.testingRequest?.title || 'Unknown Request'}</div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {renderStars(item.overallRating)}
                        <span className="ml-2">{item.overallRating || 0}/5</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        {item.wouldRecommend ? <ThumbsUp className="h-4 w-4 text-green-500" /> : <ThumbsDown className="h-4 w-4 text-red-500" />}
                        <span>{item.wouldRecommend ? 'Yes' : 'No'}</span>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge className={getStatusColor(item.isReported)}>{item.isReported ? 'Reported' : 'Active'}</Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">{item.createdAt ? new Date(item.createdAt).toLocaleDateString() : 'N/A'}</TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button size="sm" variant="outline" onClick={() => setSelectedFeedback(item)} className="h-8 w-8 p-0">
                          <Eye className="h-4 w-4" />
                        </Button>
                        {!item.isReported && (
                          <Button size="sm" variant="outline" onClick={() => updateFeedbackStatus(item.id || '', 'report')} className="h-8 w-8 p-0">
                            <Flag className="h-4 w-4" />
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Feedback Detail Dialog */}
      <Dialog open={!!selectedFeedback} onOpenChange={(open) => !open && setSelectedFeedback(null)}>
        <DialogContent className="max-w-4xl max-h-[80vh] overflow-y-auto">
          {selectedFeedback && (
            <>
              <DialogHeader>
                <DialogTitle>Feedback Details - {selectedFeedback.testingRequest?.title || 'Unknown Request'}</DialogTitle>
                <DialogDescription>
                  Submitted on {selectedFeedback.createdAt ? new Date(selectedFeedback.createdAt).toLocaleDateString() : 'Unknown date'}
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-4">
                <div>
                  <h4 className="font-medium mb-2">Overall Rating</h4>
                  <div className="flex items-center gap-2">
                    {renderStars(selectedFeedback.overallRating)}
                    <span>({selectedFeedback.overallRating || 0}/5)</span>
                  </div>
                </div>
                {selectedFeedback.additionalNotes && (
                  <div>
                    <h4 className="font-medium mb-2">Additional Notes</h4>
                    <p className="text-muted-foreground">{selectedFeedback.additionalNotes}</p>
                  </div>
                )}
                <div>
                  <h4 className="font-medium mb-2">Feedback Data</h4>
                  <pre className="text-sm bg-muted p-3 rounded overflow-auto">{selectedFeedback.feedbackData || 'No data available'}</pre>
                </div>
                <div>
                  <h4 className="font-medium mb-2">Would Recommend</h4>
                  <div className="flex items-center gap-2">
                    {selectedFeedback.wouldRecommend ? <ThumbsUp className="h-4 w-4 text-green-400" /> : <ThumbsDown className="h-4 w-4 text-red-400" />}
                    <span>{selectedFeedback.wouldRecommend ? 'Yes' : 'No'}</span>
                  </div>
                </div>
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
