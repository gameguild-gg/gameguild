'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Checkbox } from '@/components/ui/checkbox';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { testingLabApi } from '@/lib/api/testing-lab/testing-lab-api';
import { Upload, FileText, Link as LinkIcon, Save, Send, AlertCircle } from 'lucide-react';

interface SubmissionForm {
  title: string;
  description: string;
  versionNumber: string;
  teamIdentifier: string;
  downloadUrl: string;
  instructionsType: 'inline' | 'file' | 'url';
  instructionsContent: string;
  instructionsUrl: string;
  feedbackFormContent: string;
  maxTesters: number;
  startDate: string;
  endDate: string;
}

export function VersionSubmissionForm() {
  const router = useRouter();
  const [form, setForm] = useState<SubmissionForm>({
    title: '',
    description: '',
    versionNumber: '',
    teamIdentifier: '',
    downloadUrl: '',
    instructionsType: 'inline',
    instructionsContent: '',
    instructionsUrl: '',
    feedbackFormContent: '',
    maxTesters: 8,
    startDate: '',
    endDate: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDraft, setIsDraft] = useState(false);

  const handleInputChange = (field: keyof SubmissionForm, value: string | number) => {
    setForm((prev) => ({ ...prev, [field]: value }));
  };

  const generateTeamIdentifier = (semester: string, year: string, teamNumber: string) => {
    if (semester && year && teamNumber) {
      return `${semester}${year.slice(-2)}-capstone-${year}-${teamNumber.padStart(2, '0')}`;
    }
    return '';
  };

  const handleSubmit = async (saveAsDraft = false) => {
    setIsSubmitting(true);
    setIsDraft(saveAsDraft);

    try {
      // Validate required fields
      if (!form.title || !form.versionNumber || !form.teamIdentifier) {
        throw new Error('Please fill in all required fields');
      }

      // Prepare submission data
      const submissionData = {
        title: form.title.trim(),
        description: form.description.trim() || undefined,
        versionNumber: form.versionNumber.trim(),
        teamIdentifier: form.teamIdentifier.trim(),
        downloadUrl: form.downloadUrl.trim() || undefined,
        instructionsType: form.instructionsType as 'inline' | 'url' | 'file',
        instructionsContent: form.instructionsType === 'inline' ? form.instructionsContent.trim() || undefined : undefined,
        instructionsUrl: form.instructionsType === 'url' ? form.instructionsUrl.trim() || undefined : undefined,
        feedbackFormContent: form.feedbackFormContent.trim() || undefined,
        maxTesters: form.maxTesters > 0 ? form.maxTesters : undefined,
        startDate: form.startDate || undefined,
        endDate: form.endDate || undefined,
      };

      // Submit to API
      const response = await testingLabApi.createSimpleTestingRequest(submissionData);
      
      console.log('Submission successful:', response);

      // Redirect to testing lab dashboard
      router.push('/dashboard/testing-lab');
    } catch (error) {
      console.error('Submission error:', error);
      alert('Failed to submit version. Please try again.');
    } finally {
      setIsSubmitting(false);
      setIsDraft(false);
    }
  };

  return (
    <div className="max-w-4xl mx-auto space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold text-white">Submit New Version</h1>
        <p className="text-slate-400 mt-1">
          Submit your game version for testing by your peers
        </p>
      </div>

      <form onSubmit={(e) => e.preventDefault()} className="space-y-6">
        {/* Basic Information */}
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="text-white flex items-center gap-2">
              <FileText className="h-5 w-5" />
              Basic Information
            </CardTitle>
            <CardDescription className="text-slate-400">
              Provide basic details about your game version
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="title" className="text-slate-300">
                  Game Title *
                </Label>
                <Input
                  id="title"
                  value={form.title}
                  onChange={(e) => handleInputChange('title', e.target.value)}
                  placeholder="e.g., Space Adventure"
                  className="bg-slate-700/50 border-slate-600 text-white"
                  required
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="versionNumber" className="text-slate-300">
                  Version Number *
                </Label>
                <Input
                  id="versionNumber"
                  value={form.versionNumber}
                  onChange={(e) => handleInputChange('versionNumber', e.target.value)}
                  placeholder="e.g., 1.2.0"
                  className="bg-slate-700/50 border-slate-600 text-white"
                  required
                />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="description" className="text-slate-300">
                Description
              </Label>
              <Textarea
                id="description"
                value={form.description}
                onChange={(e) => handleInputChange('description', e.target.value)}
                placeholder="Brief description of your game and what's new in this version"
                rows={3}
                className="bg-slate-700/50 border-slate-600 text-white"
              />
            </div>

            {/* Team Identifier Helper */}
            <div className="space-y-4">
              <Label className="text-slate-300">Team Identifier</Label>
              <div className="grid grid-cols-1 md:grid-cols-4 gap-2">
                <Select onValueChange={(value) => handleInputChange('teamIdentifier', generateTeamIdentifier(value, '2024', '03'))}>
                  <SelectTrigger className="bg-slate-700/50 border-slate-600 text-white">
                    <SelectValue placeholder="Semester" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="fa">Fall</SelectItem>
                    <SelectItem value="sp">Spring</SelectItem>
                  </SelectContent>
                </Select>
                <Input
                  placeholder="Year (2024)"
                  className="bg-slate-700/50 border-slate-600 text-white"
                  defaultValue="2024"
                />
                <Input
                  placeholder="Team # (01-15)"
                  className="bg-slate-700/50 border-slate-600 text-white"
                  defaultValue="03"
                />
                <Input
                  value={form.teamIdentifier || 'fa24-capstone-2024-03'}
                  onChange={(e) => handleInputChange('teamIdentifier', e.target.value)}
                  className="bg-slate-700/50 border-slate-600 text-white"
                />
              </div>
              <p className="text-xs text-slate-500">
                Auto-generated team identifier following the required pattern
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Download Information */}
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="text-white flex items-center gap-2">
              <Upload className="h-5 w-5" />
              Game Download
            </CardTitle>
            <CardDescription className="text-slate-400">
              Provide a link to download your game build
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="downloadUrl" className="text-slate-300">
                Download URL *
              </Label>
              <Input
                id="downloadUrl"
                value={form.downloadUrl}
                onChange={(e) => handleInputChange('downloadUrl', e.target.value)}
                placeholder="https://drive.google.com/... or similar file sharing link"
                className="bg-slate-700/50 border-slate-600 text-white"
                required
              />
              <Alert className="bg-blue-900/20 border-blue-600/30">
                <AlertCircle className="h-4 w-4 text-blue-400" />
                <AlertDescription className="text-blue-300">
                  Make sure your download link is accessible to anyone with the link. 
                  Google Drive, Dropbox, or OneDrive shared links work well.
                </AlertDescription>
              </Alert>
            </div>

            <div className="space-y-2">
              <Label htmlFor="maxTesters" className="text-slate-300">
                Maximum Testers
              </Label>
              <Select
                value={form.maxTesters.toString()}
                onValueChange={(value) => handleInputChange('maxTesters', parseInt(value))}
              >
                <SelectTrigger className="bg-slate-700/50 border-slate-600 text-white">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="5">5 testers</SelectItem>
                  <SelectItem value="8">8 testers (recommended)</SelectItem>
                  <SelectItem value="10">10 testers</SelectItem>
                  <SelectItem value="15">15 testers</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </CardContent>
        </Card>

        {/* Testing Instructions */}
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="text-white flex items-center gap-2">
              <FileText className="h-5 w-5" />
              Testing Instructions
            </CardTitle>
            <CardDescription className="text-slate-400">
              Guide testers on how to play and what to focus on
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-3">
              <Label className="text-slate-300">Instruction Type</Label>
              <RadioGroup
                value={form.instructionsType}
                onValueChange={(value: 'inline' | 'file' | 'url') => handleInputChange('instructionsType', value)}
                className="flex space-x-6"
              >
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="inline" id="inline" />
                  <Label htmlFor="inline" className="text-slate-300">Text Instructions</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="url" id="url" />
                  <Label htmlFor="url" className="text-slate-300">URL Link</Label>
                </div>
              </RadioGroup>
            </div>

            {form.instructionsType === 'inline' && (
              <div className="space-y-2">
                <Label htmlFor="instructionsContent" className="text-slate-300">
                  Instructions Content
                </Label>
                <Textarea
                  id="instructionsContent"
                  value={form.instructionsContent}
                  onChange={(e) => handleInputChange('instructionsContent', e.target.value)}
                  placeholder="Explain how to play the game, controls, objectives, and what aspects testers should focus on..."
                  rows={5}
                  className="bg-slate-700/50 border-slate-600 text-white"
                />
              </div>
            )}

            {form.instructionsType === 'url' && (
              <div className="space-y-2">
                <Label htmlFor="instructionsUrl" className="text-slate-300">
                  Instructions URL
                </Label>
                <Input
                  id="instructionsUrl"
                  value={form.instructionsUrl}
                  onChange={(e) => handleInputChange('instructionsUrl', e.target.value)}
                  placeholder="https://docs.google.com/... or similar document link"
                  className="bg-slate-700/50 border-slate-600 text-white"
                />
              </div>
            )}
          </CardContent>
        </Card>

        {/* Feedback Form */}
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="text-white flex items-center gap-2">
              <FileText className="h-5 w-5" />
              Feedback Form
            </CardTitle>
            <CardDescription className="text-slate-400">
              Create questions for testers to answer
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="feedbackFormContent" className="text-slate-300">
                Feedback Questions
              </Label>
              <Textarea
                id="feedbackFormContent"
                value={form.feedbackFormContent}
                onChange={(e) => handleInputChange('feedbackFormContent', e.target.value)}
                placeholder="Enter questions you want testers to answer, one per line:&#10;- How was the difficulty level?&#10;- Were the controls intuitive?&#10;- What did you like most about the game?&#10;- What could be improved?"
                rows={6}
                className="bg-slate-700/50 border-slate-600 text-white"
              />
            </div>
            <Alert className="bg-green-900/20 border-green-600/30">
              <AlertCircle className="h-4 w-4 text-green-400" />
              <AlertDescription className="text-green-300">
                Leave this blank to use the default feedback form with standard gaming questions.
              </AlertDescription>
            </Alert>
          </CardContent>
        </Card>

        {/* Testing Schedule */}
        <Card className="bg-slate-800/50 border-slate-700/50 backdrop-blur-sm">
          <CardHeader>
            <CardTitle className="text-white flex items-center gap-2">
              <FileText className="h-5 w-5" />
              Testing Schedule (Optional)
            </CardTitle>
            <CardDescription className="text-slate-400">
              Set when testing should be available
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="startDate" className="text-slate-300">
                  Start Date
                </Label>
                <Input
                  id="startDate"
                  type="datetime-local"
                  value={form.startDate}
                  onChange={(e) => handleInputChange('startDate', e.target.value)}
                  className="bg-slate-700/50 border-slate-600 text-white"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="endDate" className="text-slate-300">
                  End Date
                </Label>
                <Input
                  id="endDate"
                  type="datetime-local"
                  value={form.endDate}
                  onChange={(e) => handleInputChange('endDate', e.target.value)}
                  className="bg-slate-700/50 border-slate-600 text-white"
                />
              </div>
            </div>
            <p className="text-xs text-slate-500">
              Leave dates blank to make testing available immediately with no end date
            </p>
          </CardContent>
        </Card>

        {/* Submit Actions */}
        <div className="flex gap-4 justify-end">
          <Button
            type="button"
            variant="outline"
            onClick={() => handleSubmit(true)}
            disabled={isSubmitting}
            className="border-slate-600 text-slate-300 hover:bg-slate-700"
          >
            <Save className="h-4 w-4 mr-2" />
            {isDraft ? 'Saving Draft...' : 'Save as Draft'}
          </Button>
          <Button
            type="button"
            onClick={() => handleSubmit(false)}
            disabled={isSubmitting || !form.title || !form.versionNumber || !form.downloadUrl}
            className="bg-blue-600 hover:bg-blue-700"
          >
            <Send className="h-4 w-4 mr-2" />
            {isSubmitting && !isDraft ? 'Submitting...' : 'Submit for Testing'}
          </Button>
        </div>
      </form>
    </div>
  );
}
