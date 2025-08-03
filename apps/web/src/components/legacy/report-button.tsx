'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { AlertTriangle, Flag, Loader2 } from 'lucide-react';
import { useToast } from '@/lib/old/hooks/use-toast';

interface ReportButtonProps {
  readonly reportType: 'course' | 'content' | 'review';
  readonly targetId: string;
  readonly targetTitle?: string;
  readonly variant?: 'default' | 'ghost' | 'outline' | 'secondary';
  readonly size?: 'default' | 'sm' | 'lg' | 'icon';
}

const reportReasons = {
  course: [
    { value: 'inappropriate-content', label: 'Inappropriate Content' },
    { value: 'misleading-information', label: 'Misleading Information' },
    { value: 'technical-issues', label: 'Technical Issues' },
    { value: 'copyright-violation', label: 'Copyright Violation' },
    { value: 'poor-quality', label: 'Poor Quality' },
    { value: 'other', label: 'Other' },
  ],
  content: [
    { value: 'incorrect-information', label: 'Incorrect Information' },
    { value: 'inappropriate-content', label: 'Inappropriate Content' },
    { value: 'broken-links', label: 'Broken Links/Resources' },
    { value: 'outdated-content', label: 'Outdated Content' },
    { value: 'accessibility-issues', label: 'Accessibility Issues' },
    { value: 'other', label: 'Other' },
  ],
  review: [
    { value: 'inappropriate-language', label: 'Inappropriate Language' },
    { value: 'biased-review', label: 'Biased/Unfair Review' },
    { value: 'plagiarism', label: 'Plagiarism' },
    { value: 'harassment', label: 'Harassment' },
    { value: 'fake-review', label: 'Fake Review' },
    { value: 'other', label: 'Other' },
  ],
};

export function ReportButton({ reportType, targetId, targetTitle, variant = 'ghost', size = 'sm' }: ReportButtonProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [reason, setReason] = useState('');
  const [description, setDescription] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const { toast } = useToast();

  const handleSubmit = async () => {
    if (!reason) {
      toast({
        title: 'Error',
        description: 'Please select a reason for reporting',
        variant: 'destructive',
      });
      return;
    }

    setIsSubmitting(true);

    try {
      const { submitReport } = await import('@/lib/core/actions');

      const result = await submitReport({
        reportType,
        targetId,
        reason,
        description,
        targetTitle,
      });

      if (result.success) {
        toast({
          title: 'Report Submitted',
          description: 'Thank you for reporting. We will review this shortly.',
        });
        setIsOpen(false);
        setReason('');
        setDescription('');
      } else {
        throw new Error('Failed to submit report');
      }
    } catch (error) {
      console.error('Error submitting report:', error);
      toast({
        title: 'Error',
        description: 'Failed to submit report. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setIsSubmitting(false);
    }
  };

  const reasons = reportReasons[reportType];

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <Button variant={variant} size={size} className="text-red-400 hover:text-red-300">
          <Flag className="h-4 w-4 mr-1" />
          Report
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-red-500" />
            Report {reportType.charAt(0).toUpperCase() + reportType.slice(1)}
          </DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          {targetTitle && (
            <div className="text-sm text-muted-foreground">
              <strong>Reporting:</strong> {targetTitle}
            </div>
          )}

          <div className="space-y-2">
            <label className="text-sm font-medium">Reason for reporting</label>
            <Select value={reason} onValueChange={setReason}>
              <SelectTrigger>
                <SelectValue placeholder="Select a reason" />
              </SelectTrigger>
              <SelectContent>
                {reasons.map((reasonOption) => (
                  <SelectItem key={reasonOption.value} value={reasonOption.value}>
                    {reasonOption.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <label className="text-sm font-medium">Additional details (optional)</label>
            <Textarea placeholder="Provide additional context about the issue..." value={description} onChange={(e) => setDescription(e.target.value)} rows={3} />
          </div>

          <div className="flex gap-2 justify-end">
            <Button variant="outline" onClick={() => setIsOpen(false)} disabled={isSubmitting}>
              Cancel
            </Button>
            <Button onClick={handleSubmit} disabled={isSubmitting || !reason} variant="destructive">
              {isSubmitting && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
              Submit Report
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
