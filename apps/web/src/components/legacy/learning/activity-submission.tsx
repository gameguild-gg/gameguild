'use client';

import { useState } from 'react';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Badge } from '@game-guild/ui/components/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@game-guild/ui/components/dialog';
import { Upload, FileText, Code, Award, Clock, CheckCircle, AlertCircle, Loader2 } from 'lucide-react';
import { useToast } from '@/lib/old/hooks/use-toast';

interface ActivitySubmissionProps {
  readonly activityId: string;
  readonly activityTitle: string;
  readonly activityType: 'code' | 'text' | 'file' | 'quiz';
  readonly isGraded: boolean;
  readonly maxAttempts?: number;
  readonly currentAttempts?: number;
  readonly onSubmissionComplete?: (submission: any) => void;
}

interface SubmissionData {
  textResponse?: string;
  codeResponse?: string;
  fileResponse?: File[];
  answers?: Record<string, any>;
}

export function ActivitySubmission({
  activityId,
  activityTitle,
  activityType,
  isGraded,
  maxAttempts = 3,
  currentAttempts = 0,
  onSubmissionComplete,
}: ActivitySubmissionProps) {
  const [submissionData, setSubmissionData] = useState<SubmissionData>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showConfirmDialog, setShowConfirmDialog] = useState(false);
  const [files, setFiles] = useState<File[]>([]);
  const { toast } = useToast();

  const canSubmit = currentAttempts < maxAttempts;
  const attemptsRemaining = maxAttempts - currentAttempts;

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFiles = Array.from(event.target.files || []);
    setFiles(selectedFiles);
    setSubmissionData(prev => ({ ...prev, fileResponse: selectedFiles }));
  };

  const handleSubmit = async () => {
    if (!canSubmit) {
      toast({
        title: 'Maximum attempts reached',
        description: `You have used all ${maxAttempts} attempts for this activity.`,
        variant: 'destructive',
      });
      return;
    }

    setIsSubmitting(true);
    setShowConfirmDialog(false);

    try {
      const { submitActivity } = await import('@/lib/courses/server-actions');
      
      const result = await submitActivity({
        activityId,
        activityType,
        submissionData,
        isGraded,
        attempt: currentAttempts + 1,
      });

      if (result.success) {
        const submission = result;
        
        toast({
          title: 'Submission successful',
          description: isGraded 
            ? 'Your submission has been sent for grading.'
            : 'Activity completed successfully!',
        });

        onSubmissionComplete?.(result.submission);
      } else {
        throw new Error('Failed to submit activity');
      }
    } catch (error) {
      console.error('Error submitting activity:', error);
      toast({
        title: 'Submission failed',
        description: 'Failed to submit activity. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const renderSubmissionInterface = () => {
    switch (activityType) {
      case 'text':
        return (
          <div className="space-y-4">
            <label className="text-sm font-medium">Your Response</label>
            <Textarea
              placeholder="Enter your response here..."
              value={submissionData.textResponse || ''}
              onChange={(e) => setSubmissionData(prev => ({ ...prev, textResponse: e.target.value }))}
              rows={8}
              className="resize-y"
            />
          </div>
        );

      case 'code':
        return (
          <div className="space-y-4">
            <label className="text-sm font-medium">Code Solution</label>
            <Textarea
              placeholder="// Enter your code here..."
              value={submissionData.codeResponse || ''}
              onChange={(e) => setSubmissionData(prev => ({ ...prev, codeResponse: e.target.value }))}
              rows={12}
              className="font-mono text-sm resize-y"
            />
          </div>
        );

      case 'file':
        return (
          <div className="space-y-4">
            <label className="text-sm font-medium">Upload Files</label>
            <div className="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center">
              <Upload className="h-8 w-8 mx-auto mb-2 text-gray-400" />
              <input
                type="file"
                multiple
                onChange={handleFileChange}
                className="hidden"
                id="file-upload"
                accept=".pdf,.doc,.docx,.txt,.zip,.png,.jpg,.jpeg"
              />
              <label
                htmlFor="file-upload"
                className="cursor-pointer text-blue-600 hover:text-blue-800"
              >
                Click to upload files
              </label>
              <p className="text-sm text-gray-500 mt-1">
                Supported: PDF, DOC, TXT, ZIP, Images (max 10MB each)
              </p>
            </div>
            {files.length > 0 && (
              <div className="space-y-2">
                <p className="text-sm font-medium">Selected files:</p>
                {files.map((file, index) => (
                  <div key={index} className="flex items-center gap-2 text-sm">
                    <FileText className="h-4 w-4" />
                    <span>{file.name}</span>
                    <span className="text-gray-500">({Math.round(file.size / 1024)} KB)</span>
                  </div>
                ))}
              </div>
            )}
          </div>
        );

      default:
        return (
          <div className="text-center py-8 text-gray-500">
            Activity interface not implemented for type: {activityType}
          </div>
        );
    }
  };

  const hasContent = () => {
    switch (activityType) {
      case 'text':
        return (submissionData.textResponse?.trim().length || 0) > 0;
      case 'code':
        return (submissionData.codeResponse?.trim().length || 0) > 0;
      case 'file':
        return files.length > 0;
      default:
        return false;
    }
  };

  return (
    <>
      <Card className="w-full">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2">
              {activityType === 'code' && <Code className="h-5 w-5" />}
              {activityType === 'text' && <FileText className="h-5 w-5" />}
              {activityType === 'file' && <Upload className="h-5 w-5" />}
              {activityType === 'quiz' && <Award className="h-5 w-5" />}
              {activityTitle}
            </CardTitle>
            <div className="flex gap-2">
              {isGraded && (
                <Badge variant="outline" className="border-yellow-500 text-yellow-600">
                  <Award className="h-3 w-3 mr-1" />
                  Graded
                </Badge>
              )}
              <Badge variant="outline">
                <Clock className="h-3 w-3 mr-1" />
                {attemptsRemaining} attempts left
              </Badge>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-6">
          {renderSubmissionInterface()}
          
          <div className="flex items-center justify-between pt-4 border-t">
            <div className="flex items-center gap-2 text-sm text-gray-600">
              {currentAttempts > 0 && (
                <>
                  <AlertCircle className="h-4 w-4" />
                  Attempt {currentAttempts + 1} of {maxAttempts}
                </>
              )}
            </div>
            <Button
              onClick={() => setShowConfirmDialog(true)}
              disabled={!hasContent() || !canSubmit || isSubmitting}
              className="px-8"
            >
              {isSubmitting && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
              {isGraded ? 'Submit for Grading' : 'Submit Activity'}
            </Button>
          </div>
        </CardContent>
      </Card>

      <Dialog open={showConfirmDialog} onOpenChange={setShowConfirmDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Confirm Submission</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <p>
              Are you sure you want to submit this activity? 
              {isGraded && ' Once submitted, it will be sent for grading.'}
            </p>
            {attemptsRemaining <= 1 && (
              <div className="p-3 bg-yellow-50 border border-yellow-200 rounded-md">
                <div className="flex items-center gap-2 text-yellow-800">
                  <AlertCircle className="h-4 w-4" />
                  <span className="text-sm font-medium">This is your final attempt!</span>
                </div>
              </div>
            )}
            <div className="flex gap-2 justify-end">
              <Button
                variant="outline"
                onClick={() => setShowConfirmDialog(false)}
                disabled={isSubmitting}
              >
                Cancel
              </Button>
              <Button onClick={handleSubmit} disabled={isSubmitting}>
                {isSubmitting && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                Confirm Submission
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
