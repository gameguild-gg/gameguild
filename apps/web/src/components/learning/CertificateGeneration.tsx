'use client';

import { useState } from 'react';
import { Button } from '@game-guild/ui/components';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components';
import { Badge } from '@game-guild/ui/components';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@game-guild/ui/components';
import { Award, Download, Share2, Check, Loader2, Calendar, User } from 'lucide-react';
import { useToast } from '@/hooks/use-toast';

interface CertificateProps {
  readonly courseId: string;
  readonly courseTitle: string;
  readonly completionDate: string;
  readonly studentName: string;
  readonly instructorName?: string;
  readonly skillsLearned?: string[];
  readonly finalGrade?: number;
  readonly certificateId?: string;
}

export function CertificateGeneration({ 
  courseId, 
  courseTitle, 
  completionDate, 
  studentName,
  instructorName = 'Game Guild Academy',
  skillsLearned = [],
  finalGrade,
  certificateId
}: CertificateProps) {
  const [isGenerating, setIsGenerating] = useState(false);
  const [showPreview, setShowPreview] = useState(false);
  const [certificateUrl, setCertificateUrl] = useState<string | null>(null);
  const [isDownloading, setIsDownloading] = useState(false);
  const { toast } = useToast();

  const handleGenerateCertificate = async () => {
    setIsGenerating(true);

    try {
      const { generateCertificate } = await import('@/lib/certificates/actions');
      
      const result = await generateCertificate({
        courseId,
        courseTitle,
        studentName,
        completionDate,
        instructorName,
        skillsLearned,
        finalGrade,
      });

      if (result.success) {
        setCertificateUrl(result.certificate.downloadUrl);
        
        toast({
          title: 'Certificate generated!',
          description: 'Your certificate of completion has been generated successfully.',
        });
      } else {
        throw new Error('Failed to generate certificate');
      }
    } catch (error) {
      console.error('Error generating certificate:', error);
      toast({
        title: 'Generation failed',
        description: 'Failed to generate certificate. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setIsGenerating(false);
    }
  };

  const handleDownloadCertificate = async () => {
    if (!certificateUrl) return;

    setIsDownloading(true);

    try {
      const response = await fetch(certificateUrl);
      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${courseTitle.replace(/\s+/g, '_')}_Certificate.pdf`;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);

        toast({
          title: 'Download started',
          description: 'Your certificate is being downloaded.',
        });
      } else {
        throw new Error('Failed to download certificate');
      }
    } catch (error) {
      console.error('Error downloading certificate:', error);
      toast({
        title: 'Download failed',
        description: 'Failed to download certificate. Please try again.',
        variant: 'destructive',
      });
    } finally {
      setIsDownloading(false);
    }
  };

  const handleShareCertificate = () => {
    if (!certificateUrl) return;

    // Copy certificate URL to clipboard for sharing
    navigator.clipboard.writeText(certificateUrl).then(() => {
      toast({
        title: 'Link copied',
        description: 'Certificate link copied to clipboard for sharing.',
      });
    });
  };

  return (
    <>
      <Card className="w-full border-green-200 bg-green-50/50">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle className="flex items-center gap-2 text-green-800">
              <Award className="h-6 w-6" />
              Certificate Available
            </CardTitle>
            <Badge className="bg-green-500 text-white">
              <Check className="h-3 w-3 mr-1" />
              Course Completed
            </Badge>
          </div>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="text-center space-y-2">
            <h3 className="text-xl font-semibold text-green-800">
              Congratulations, {studentName}!
            </h3>
            <p className="text-green-700">
              You have successfully completed <strong>{courseTitle}</strong>
            </p>
            {finalGrade && (
              <p className="text-sm text-green-600">
                Final Grade: <span className="font-mono font-semibold">{finalGrade}%</span>
              </p>
            )}
          </div>

          <div className="grid md:grid-cols-2 gap-4 p-4 bg-white rounded-lg border border-green-200">
            <div className="space-y-2">
              <div className="flex items-center gap-2 text-sm text-gray-600">
                <Calendar className="h-4 w-4" />
                <span>Completed: {new Date(completionDate).toLocaleDateString()}</span>
              </div>
              <div className="flex items-center gap-2 text-sm text-gray-600">
                <User className="h-4 w-4" />
                <span>Instructor: {instructorName}</span>
              </div>
              {certificateId && (
                <div className="text-xs text-gray-500 font-mono">
                  Certificate ID: {certificateId}
                </div>
              )}
            </div>

            {skillsLearned.length > 0 && (
              <div className="space-y-2">
                <h4 className="text-sm font-medium text-gray-700">Skills Learned:</h4>
                <div className="flex flex-wrap gap-1">
                  {skillsLearned.slice(0, 4).map((skill, index) => (
                    <Badge key={index} variant="outline" className="text-xs">
                      {skill}
                    </Badge>
                  ))}
                  {skillsLearned.length > 4 && (
                    <Badge variant="outline" className="text-xs">
                      +{skillsLearned.length - 4} more
                    </Badge>
                  )}
                </div>
              </div>
            )}
          </div>

          <div className="flex flex-col sm:flex-row gap-3">
            {!certificateUrl ? (
              <Button
                onClick={handleGenerateCertificate}
                disabled={isGenerating}
                className="flex-1 bg-green-600 hover:bg-green-700"
              >
                {isGenerating && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                <Award className="h-4 w-4 mr-2" />
                Generate Certificate
              </Button>
            ) : (
              <>
                <Button
                  onClick={handleDownloadCertificate}
                  disabled={isDownloading}
                  className="flex-1 bg-green-600 hover:bg-green-700"
                >
                  {isDownloading && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                  <Download className="h-4 w-4 mr-2" />
                  Download PDF
                </Button>
                <Button
                  variant="outline"
                  onClick={() => setShowPreview(true)}
                  className="border-green-300 text-green-700 hover:bg-green-50"
                >
                  Preview
                </Button>
                <Button
                  variant="outline"
                  onClick={handleShareCertificate}
                  className="border-green-300 text-green-700 hover:bg-green-50"
                >
                  <Share2 className="h-4 w-4 mr-2" />
                  Share
                </Button>
              </>
            )}
          </div>

          <div className="text-center text-xs text-gray-500">
            This certificate verifies that you have successfully completed all requirements
            for the course and demonstrates your achievement in game development skills.
          </div>
        </CardContent>
      </Card>

      {/* Certificate Preview Dialog */}
      <Dialog open={showPreview} onOpenChange={setShowPreview}>
        <DialogContent className="max-w-4xl">
          <DialogHeader>
            <DialogTitle>Certificate Preview</DialogTitle>
          </DialogHeader>
          <div className="w-full">
            {certificateUrl && (
              <iframe
                src={certificateUrl}
                className="w-full h-96 border rounded-lg"
                title="Certificate Preview"
              />
            )}
            <div className="flex justify-end gap-2 mt-4">
              <Button variant="outline" onClick={() => setShowPreview(false)}>
                Close
              </Button>
              <Button onClick={handleDownloadCertificate} disabled={isDownloading}>
                {isDownloading && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                <Download className="h-4 w-4 mr-2" />
                Download
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}

/* Component for certificate notification */
export function CertificateNotification({ 
  courseTitle, 
  onViewCertificate 
}: { 
  courseTitle: string; 
  onViewCertificate: () => void;
}) {
  return (
    <Card className="border-green-300 bg-gradient-to-r from-green-50 to-emerald-50">
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-green-100 rounded-full">
              <Award className="h-5 w-5 text-green-600" />
            </div>
            <div>
              <h4 className="font-medium text-green-800">Certificate Ready!</h4>
              <p className="text-sm text-green-600">
                Your certificate for "{courseTitle}" is now available
              </p>
            </div>
          </div>
          <Button
            onClick={onViewCertificate}
            size="sm"
            className="bg-green-600 hover:bg-green-700"
          >
            View Certificate
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
