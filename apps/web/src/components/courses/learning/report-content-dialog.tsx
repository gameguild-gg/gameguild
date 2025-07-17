'use client';

import React, { useState } from 'react';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Label } from '@/components/ui/label';
import { AlertTriangle, Copyright, Flag, Shield, Spam } from 'lucide-react';

interface ReportContentDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  contentId: string;
  contentTitle: string;
  onSubmit: (contentId: string, reason: string, description: string) => void;
}

const reportReasons = [
  {
    id: 'inappropriate',
    label: 'Inappropriate Content',
    description: 'Contains offensive, harmful, or inappropriate material',
    icon: AlertTriangle,
  },
  {
    id: 'misinformation',
    label: 'Misinformation',
    description: 'Contains false or misleading information',
    icon: Shield,
  },
  {
    id: 'copyright',
    label: 'Copyright Violation',
    description: 'Violates copyright or intellectual property rights',
    icon: Copyright,
  },
  {
    id: 'spam',
    label: 'Spam or Low Quality',
    description: 'Spam, low quality, or irrelevant content',
    icon: Spam,
  },
  {
    id: 'technical',
    label: 'Technical Issue',
    description: 'Content has technical problems or errors',
    icon: Flag,
  },
];

export function ReportContentDialog({ open, onOpenChange, contentId, contentTitle, onSubmit }: ReportContentDialogProps) {
  const [selectedReason, setSelectedReason] = useState('');
  const [description, setDescription] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async () => {
    if (!selectedReason.trim()) return;

    setIsSubmitting(true);
    try {
      await onSubmit(contentId, selectedReason, description);
      // Reset form
      setSelectedReason('');
      setDescription('');
      onOpenChange(false);
    } catch (error) {
      console.error('Error submitting report:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const selectedReasonData = reportReasons.find((r) => r.id === selectedReason);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px] bg-gray-800 border-gray-700">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-white">
            <Flag className="h-5 w-5 text-red-500" />
            Report Content
          </DialogTitle>
          <DialogDescription className="text-gray-300">
            Report "{contentTitle}" for review. Please select a reason and provide additional details.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-4">
          <div>
            <Label className="text-sm font-medium text-gray-200 mb-3 block">What's wrong with this content?</Label>
            <RadioGroup value={selectedReason} onValueChange={setSelectedReason} className="space-y-3">
              {reportReasons.map((reason) => {
                const IconComponent = reason.icon;
                return (
                  <div key={reason.id} className="flex items-start space-x-3">
                    <RadioGroupItem value={reason.id} id={reason.id} className="mt-1" />
                    <div className="flex-1">
                      <Label htmlFor={reason.id} className="flex items-center gap-2 cursor-pointer text-gray-200">
                        <IconComponent className="h-4 w-4" />
                        {reason.label}
                      </Label>
                      <p className="text-xs text-gray-400 mt-1">{reason.description}</p>
                    </div>
                  </div>
                );
              })}
            </RadioGroup>
          </div>

          <div>
            <Label htmlFor="description" className="text-sm font-medium text-gray-200 mb-2 block">
              Additional Details (Optional)
            </Label>
            <Textarea
              id="description"
              placeholder="Provide more context about the issue..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="bg-gray-700 border-gray-600 text-gray-100 placeholder-gray-400"
              rows={3}
            />
          </div>

          {selectedReasonData && (
            <div className="bg-gray-700/50 p-3 rounded-lg border border-gray-600">
              <div className="flex items-center gap-2 text-gray-200 text-sm">
                <selectedReasonData.icon className="h-4 w-4" />
                You're reporting this content for: {selectedReasonData.label}
              </div>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} className="border-gray-600">
            Cancel
          </Button>
          <Button onClick={handleSubmit} disabled={!selectedReason.trim() || isSubmitting} className="bg-red-600 hover:bg-red-700">
            {isSubmitting ? 'Submitting...' : 'Submit Report'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
