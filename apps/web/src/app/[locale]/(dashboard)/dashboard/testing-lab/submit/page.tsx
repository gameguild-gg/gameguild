'use client';

import { useState } from 'react';
import { useSession } from 'next-auth/react';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Checkbox } from '@/components/ui/checkbox';
import { Calendar } from '@/components/ui/calendar';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { CalendarIcon, Upload, Link as LinkIcon, FileText, AlertCircle } from 'lucide-react';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { format } from 'date-fns';
import { cn } from '@/lib/utils';

interface SubmissionForm {
  title: string;
  description: string;
  versionNumber: string;
  submissionType: 'file' | 'url';
  fileUrl?: string;
  downloadUrl?: string;
  testingInstructions: string;
  testingInstructionsType: 'inline' | 'file' | 'url';
  testingInstructionsFile?: File;
  testingInstructionsUrl?: string;
  feedbackForm: string;
  maxTesters?: number;
  startDate?: Date;
  endDate?: Date;
  requiresTeamAccess: boolean;
}

export default function SubmitVersionPage() {
  const { data: session } = useSession();
  const router = useRouter();
  const [form, setForm] = useState<SubmissionForm>({
    title: '',
    description: '',
    versionNumber: '',
    submissionType: 'url',
    testingInstructions: '',
    testingInstructionsType: 'inline',
    feedbackForm: '',
    requiresTeamAccess: false,
  });
  const [submitting, setSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  // Determine team from user email
  const getTeamFromEmail = (email: string) => {
    // Extract team number from email pattern if it exists
    // For now, return a placeholder
    return 'fa23-capstone-2023-24-t01';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitting(true);
    setErrors([]);

    try {
      // Validate required fields
      const validationErrors: string[] = [];
      
      if (!form.title.trim()) validationErrors.push('Title is required');
      if (!form.versionNumber.trim()) validationErrors.push('Version number is required');
      if (form.submissionType === 'url' && !form.downloadUrl?.trim()) {
        validationErrors.push('Download URL is required');
      }
      if (!form.testingInstructions.trim() && form.testingInstructionsType === 'inline') {
        validationErrors.push('Testing instructions are required');
      }
      if (form.testingInstructionsType === 'url' && !form.testingInstructionsUrl?.trim()) {
        validationErrors.push('Testing instructions URL is required');
      }
      if (!form.feedbackForm.trim()) validationErrors.push('Feedback form is required');

      if (validationErrors.length > 0) {
        setErrors(validationErrors);
        return;
      }

      // TODO: Submit to API
      console.log('Submitting testing request:', form);
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      // Redirect to testing lab page
      router.push('/dashboard/testing-lab');
    } catch (error) {
      console.error('Error submitting testing request:', error);
      setErrors(['Failed to submit testing request. Please try again.']);
    } finally {
      setSubmitting(false);
    }
  };

  const updateForm = (field: keyof SubmissionForm, value: any) => {
    setForm(prev => ({ ...prev, [field]: value }));
  };

  const teamName = session?.user?.email ? getTeamFromEmail(session.user.email) : '';

  return (
    <div className="container mx-auto p-6 max-w-4xl">
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2">Submit Version for Testing</h1>
        <p className="text-muted-foreground">
          Submit a new version of your project for community testing
        </p>
      </div>

      {errors.length > 0 && (
        <Alert className="mb-6">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>
            <ul className="list-disc list-inside">
              {errors.map((error, index) => (
                <li key={index}>{error}</li>
              ))}
            </ul>
          </AlertDescription>
        </Alert>
      )}

      <form onSubmit={handleSubmit} className="space-y-8">
        {/* Project Information */}
        <Card>
          <CardHeader>
            <CardTitle>Project Information</CardTitle>
            <CardDescription>
              Basic information about your project and version
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label htmlFor="team">Team</Label>
              <Input
                id="team"
                value={teamName}
                disabled
                className="bg-muted"
              />
              <p className="text-sm text-muted-foreground mt-1">
                Automatically detected from your email domain
              </p>
            </div>

            <div>
              <Label htmlFor="title">Title *</Label>
              <Input
                id="title"
                placeholder="e.g., Alpha Build - Core Gameplay"
                value={form.title}
                onChange={(e) => updateForm('title', e.target.value)}
                required
              />
            </div>

            <div>
              <Label htmlFor="versionNumber">Version Number *</Label>
              <Input
                id="versionNumber"
                placeholder="e.g., v0.1.0-alpha, v1.0.0"
                value={form.versionNumber}
                onChange={(e) => updateForm('versionNumber', e.target.value)}
                required
              />
            </div>

            <div>
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                placeholder="Describe what's new in this version, key features to test, known issues..."
                value={form.description}
                onChange={(e) => updateForm('description', e.target.value)}
                rows={4}
              />
            </div>
          </CardContent>
        </Card>

        {/* File/URL Submission */}
        <Card>
          <CardHeader>
            <CardTitle>Game Submission</CardTitle>
            <CardDescription>
              How testers will access your game
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label>Submission Type *</Label>
              <RadioGroup
                value={form.submissionType}
                onValueChange={(value: 'file' | 'url') => updateForm('submissionType', value)}
                className="mt-2"
              >
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="url" id="url" />
                  <Label htmlFor="url" className="flex items-center">
                    <LinkIcon className="mr-2 h-4 w-4" />
                    Download URL (Recommended)
                  </Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="file" id="file" />
                  <Label htmlFor="file" className="flex items-center">
                    <Upload className="mr-2 h-4 w-4" />
                    File Upload (Future Feature)
                  </Label>
                </div>
              </RadioGroup>
            </div>

            {form.submissionType === 'url' && (
              <div>
                <Label htmlFor="downloadUrl">Download URL *</Label>
                <Input
                  id="downloadUrl"
                  placeholder="https://drive.google.com/... or https://github.com/..."
                  value={form.downloadUrl || ''}
                  onChange={(e) => updateForm('downloadUrl', e.target.value)}
                  required
                />
                <p className="text-sm text-muted-foreground mt-1">
                  Link to where testers can download your game build
                </p>
              </div>
            )}

            {form.submissionType === 'file' && (
              <div>
                <Label htmlFor="file">Game Build File</Label>
                <div className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-8 text-center">
                  <Upload className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
                  <p className="text-muted-foreground">File upload will be available soon</p>
                  <p className="text-sm text-muted-foreground">For now, please use a download URL</p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Testing Instructions */}
        <Card>
          <CardHeader>
            <CardTitle>Testing Instructions</CardTitle>
            <CardDescription>
              Guide testers on how to test your game effectively
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label>Instructions Type *</Label>
              <RadioGroup
                value={form.testingInstructionsType}
                onValueChange={(value: 'inline' | 'file' | 'url') => updateForm('testingInstructionsType', value)}
                className="mt-2"
              >
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="inline" id="inline" />
                  <Label htmlFor="inline">Write instructions here</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="url" id="instructions-url" />
                  <Label htmlFor="instructions-url">Link to instructions</Label>
                </div>
                <div className="flex items-center space-x-2">
                  <RadioGroupItem value="file" id="instructions-file" />
                  <Label htmlFor="instructions-file">Upload instructions file</Label>
                </div>
              </RadioGroup>
            </div>

            {form.testingInstructionsType === 'inline' && (
              <div>
                <Label htmlFor="testingInstructions">Testing Instructions *</Label>
                <Textarea
                  id="testingInstructions"
                  placeholder="1. Download and extract the game files...&#10;2. Launch the game executable...&#10;3. Test the main menu navigation...&#10;4. Try the combat system...&#10;5. Look for any bugs or issues..."
                  value={form.testingInstructions}
                  onChange={(e) => updateForm('testingInstructions', e.target.value)}
                  rows={8}
                  required
                />
              </div>
            )}

            {form.testingInstructionsType === 'url' && (
              <div>
                <Label htmlFor="testingInstructionsUrl">Instructions URL *</Label>
                <Input
                  id="testingInstructionsUrl"
                  placeholder="https://docs.google.com/... or https://github.com/..."
                  value={form.testingInstructionsUrl || ''}
                  onChange={(e) => updateForm('testingInstructionsUrl', e.target.value)}
                  required
                />
              </div>
            )}

            {form.testingInstructionsType === 'file' && (
              <div>
                <Label htmlFor="instructionsFile">Instructions File</Label>
                <div className="border-2 border-dashed border-muted-foreground/25 rounded-lg p-8 text-center">
                  <FileText className="mx-auto h-12 w-12 text-muted-foreground mb-4" />
                  <p className="text-muted-foreground">File upload will be available soon</p>
                  <p className="text-sm text-muted-foreground">For now, please use inline instructions or a URL</p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Feedback Form */}
        <Card>
          <CardHeader>
            <CardTitle>Feedback Form</CardTitle>
            <CardDescription>
              Define what feedback you want from testers
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div>
              <Label htmlFor="feedbackForm">Feedback Questions *</Label>
              <Textarea
                id="feedbackForm"
                placeholder="1. How intuitive is the game's control scheme?&#10;2. Did you encounter any bugs or glitches?&#10;3. How would you rate the visual design?&#10;4. What features would you like to see improved?&#10;5. Overall rating (1-10)?"
                value={form.feedbackForm}
                onChange={(e) => updateForm('feedbackForm', e.target.value)}
                rows={8}
                required
              />
              <p className="text-sm text-muted-foreground mt-1">
                Provide specific questions or criteria you want testers to evaluate
              </p>
            </div>
          </CardContent>
        </Card>

        {/* Testing Configuration */}
        <Card>
          <CardHeader>
            <CardTitle>Testing Configuration</CardTitle>
            <CardDescription>
              Optional settings for your testing request
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <Label htmlFor="maxTesters">Maximum Number of Testers</Label>
              <Input
                id="maxTesters"
                type="number"
                placeholder="Leave empty for unlimited"
                value={form.maxTesters || ''}
                onChange={(e) => updateForm('maxTesters', e.target.value ? parseInt(e.target.value) : undefined)}
                min="1"
              />
            </div>

            <div className="space-y-4">
              <div>
                <Label>Testing Period</Label>
                <div className="grid grid-cols-2 gap-4 mt-2">
                  <div>
                    <Label htmlFor="startDate" className="text-sm">Start Date</Label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant="outline"
                          className={cn(
                            'w-full justify-start text-left font-normal',
                            !form.startDate && 'text-muted-foreground'
                          )}
                        >
                          <CalendarIcon className="mr-2 h-4 w-4" />
                          {form.startDate ? format(form.startDate, 'PPP') : 'Pick a date'}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0">
                        <Calendar
                          mode="single"
                          selected={form.startDate}
                          onSelect={(date) => updateForm('startDate', date)}
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>
                  </div>
                  <div>
                    <Label htmlFor="endDate" className="text-sm">End Date</Label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant="outline"
                          className={cn(
                            'w-full justify-start text-left font-normal',
                            !form.endDate && 'text-muted-foreground'
                          )}
                        >
                          <CalendarIcon className="mr-2 h-4 w-4" />
                          {form.endDate ? format(form.endDate, 'PPP') : 'Pick a date'}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0">
                        <Calendar
                          mode="single"
                          selected={form.endDate}
                          onSelect={(date) => updateForm('endDate', date)}
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>
                  </div>
                </div>
                <p className="text-sm text-muted-foreground mt-1">
                  Leave empty to keep the testing request open indefinitely
                </p>
              </div>
            </div>

            <div className="flex items-center space-x-2">
              <Checkbox
                id="requiresTeamAccess"
                checked={form.requiresTeamAccess}
                onCheckedChange={(checked) => updateForm('requiresTeamAccess', checked)}
              />
              <Label htmlFor="requiresTeamAccess" className="text-sm">
                Requires team repository access (for builds from private repos)
              </Label>
            </div>
          </CardContent>
        </Card>

        {/* Submit Button */}
        <div className="flex justify-between">
          <Button
            type="button"
            variant="outline"
            onClick={() => router.back()}
            disabled={submitting}
          >
            Cancel
          </Button>
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Submitting...' : 'Submit for Testing'}
          </Button>
        </div>
      </form>
    </div>
  );
}
