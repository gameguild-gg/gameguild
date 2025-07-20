'use client';

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Checkbox } from '@/components/ui/checkbox';
import { ArrowLeft, Briefcase, Upload, Calendar, Users } from 'lucide-react';

interface SubmitGameProps {
  onBack: () => void;
  onSuccess: () => void;
}

interface GameSubmission {
  gameTitle: string;
  studioName: string;
  contactEmail: string;
  gameDescription: string;
  platform: string[];
  genre: string;
  buildVersion: string;
  testingType: string;
  expectedDuration: string;
  maxTesters: string;
  timeline: string;
  specialRequirements: string;
  buildDownload: string;
  testingGoals: string;
  agreeToTerms: boolean;
}

const PLATFORMS = ['PC/Windows', 'Mac', 'PlayStation 5', 'PlayStation 4', 'Xbox Series X/S', 'Xbox One', 'Nintendo Switch', 'iOS', 'Android', 'Web Browser'];

const GENRES = [
  'Action',
  'Adventure',
  'RPG',
  'Strategy',
  'Simulation',
  'Sports',
  'Racing',
  'Puzzle',
  'Platformer',
  'Fighting',
  'Shooter',
  'Horror',
  'Indie',
  'Casual',
  'Educational',
  'Multiplayer',
];

const TESTING_TYPES = [
  'Alpha Testing',
  'Beta Testing',
  'Gameplay Testing',
  'UI/UX Testing',
  'Bug Testing',
  'Performance Testing',
  'Accessibility Testing',
  'Multiplayer Testing',
];

export function SubmitGame({ onBack, onSuccess }: SubmitGameProps) {
  const [submission, setSubmission] = useState<GameSubmission>({
    gameTitle: '',
    studioName: '',
    contactEmail: '',
    gameDescription: '',
    platform: [],
    genre: '',
    buildVersion: '',
    testingType: '',
    expectedDuration: '',
    maxTesters: '',
    timeline: '',
    specialRequirements: '',
    buildDownload: '',
    testingGoals: '',
    agreeToTerms: false,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handlePlatformToggle = (platform: string) => {
    setSubmission((prev) => ({
      ...prev,
      platform: prev.platform.includes(platform) ? prev.platform.filter((p) => p !== platform) : [...prev.platform, platform],
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!submission.agreeToTerms) {
      alert('Please agree to the terms and conditions');
      return;
    }

    setIsSubmitting(true);

    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 3000));

    setIsSubmitting(false);
    onSuccess();
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="flex items-center gap-4 mb-8">
          <Button onClick={onBack} variant="ghost" className="text-slate-400 hover:text-white">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back
          </Button>

          <div>
            <h1 className="text-3xl font-bold bg-gradient-to-r from-purple-400 to-blue-400 bg-clip-text text-transparent">Submit Game for Testing</h1>
            <p className="text-slate-400">Get quality feedback from experienced testers</p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-8">
          {/* Game Information */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold bg-gradient-to-r from-purple-400 to-blue-400 bg-clip-text text-transparent flex items-center gap-2">
                <Briefcase className="h-5 w-5" />
                Game Information
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="gameTitle" className="text-slate-200">
                    Game Title *
                  </Label>
                  <Input
                    id="gameTitle"
                    value={submission.gameTitle}
                    onChange={(e) => setSubmission((prev) => ({ ...prev, gameTitle: e.target.value }))}
                    required
                    className="bg-slate-800/50 border-slate-600 text-white"
                  />
                </div>
                <div>
                  <Label htmlFor="studioName" className="text-slate-200">
                    Studio/Developer Name *
                  </Label>
                  <Input
                    id="studioName"
                    value={submission.studioName}
                    onChange={(e) => setSubmission((prev) => ({ ...prev, studioName: e.target.value }))}
                    required
                    className="bg-slate-800/50 border-slate-600 text-white"
                  />
                </div>
              </div>

              <div>
                <Label htmlFor="contactEmail" className="text-slate-200">
                  Contact Email *
                </Label>
                <Input
                  id="contactEmail"
                  type="email"
                  value={submission.contactEmail}
                  onChange={(e) => setSubmission((prev) => ({ ...prev, contactEmail: e.target.value }))}
                  required
                  className="bg-slate-800/50 border-slate-600 text-white"
                />
              </div>

              <div>
                <Label htmlFor="gameDescription" className="text-slate-200">
                  Game Description *
                </Label>
                <Textarea
                  id="gameDescription"
                  value={submission.gameDescription}
                  onChange={(e) => setSubmission((prev) => ({ ...prev, gameDescription: e.target.value }))}
                  placeholder="Describe your game, its core mechanics, target audience, and what makes it unique..."
                  rows={4}
                  required
                  className="bg-slate-800/50 border-slate-600 text-white"
                />
              </div>

              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="genre" className="text-slate-200">
                    Primary Genre *
                  </Label>
                  <select
                    id="genre"
                    value={submission.genre}
                    onChange={(e) => setSubmission((prev) => ({ ...prev, genre: e.target.value }))}
                    required
                    className="w-full mt-1 bg-slate-800/50 border border-slate-600 rounded-md px-3 py-2 text-white"
                  >
                    <option value="">Select genre</option>
                    {GENRES.map((genre) => (
                      <option key={genre} value={genre}>
                        {genre}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <Label htmlFor="buildVersion" className="text-slate-200">
                    Build Version *
                  </Label>
                  <Input
                    id="buildVersion"
                    value={submission.buildVersion}
                    onChange={(e) => setSubmission((prev) => ({ ...prev, buildVersion: e.target.value }))}
                    placeholder="e.g., 0.1.0, Alpha 1.2, Beta 2.0"
                    required
                    className="bg-slate-800/50 border-slate-600 text-white"
                  />
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Platform & Testing Details */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold bg-gradient-to-r from-purple-400 to-blue-400 bg-clip-text text-transparent">
                Platform & Testing Details
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              <div>
                <Label className="text-slate-200">Target Platforms *</Label>
                <p className="text-xs text-slate-400 mb-3">Select all platforms your game supports</p>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
                  {PLATFORMS.map((platform) => {
                    const isSelected = submission.platform.includes(platform);
                    return (
                      <button
                        key={platform}
                        type="button"
                        onClick={() => handlePlatformToggle(platform)}
                        className={`p-3 rounded-lg border transition-colors text-left ${
                          isSelected
                            ? 'border-purple-500 bg-purple-900/20 text-purple-300'
                            : 'border-slate-600 bg-slate-800/30 text-slate-400 hover:border-slate-500'
                        }`}
                      >
                        <div className="text-sm font-medium">{platform}</div>
                      </button>
                    );
                  })}
                </div>
              </div>

              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="testingType" className="text-slate-200">
                    Testing Type *
                  </Label>
                  <select
                    id="testingType"
                    value={submission.testingType}
                    onChange={(e) => setSubmission((prev) => ({ ...prev, testingType: e.target.value }))}
                    required
                    className="w-full mt-1 bg-slate-800/50 border border-slate-600 rounded-md px-3 py-2 text-white"
                  >
                    <option value="">Select testing type</option>
                    {TESTING_TYPES.map((type) => (
                      <option key={type} value={type}>
                        {type}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <Label htmlFor="expectedDuration" className="text-slate-200">
                    Expected Session Duration *
                  </Label>
                  <select
                    id="expectedDuration"
                    value={submission.expectedDuration}
                    onChange={(e) => setSubmission((prev) => ({ ...prev, expectedDuration: e.target.value }))}
                    required
                    className="w-full mt-1 bg-slate-800/50 border border-slate-600 rounded-md px-3 py-2 text-white"
                  >
                    <option value="">Select duration</option>
                    <option value="30">30 minutes</option>
                    <option value="60">1 hour</option>
                    <option value="90">1.5 hours</option>
                    <option value="120">2 hours</option>
                    <option value="180">3 hours</option>
                    <option value="custom">Custom duration</option>
                  </select>
                </div>
              </div>

              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="maxTesters" className="text-slate-200">
                    Maximum Testers *
                  </Label>
                  <Input
                    id="maxTesters"
                    type="number"
                    min="1"
                    max="50"
                    value={submission.maxTesters}
                    onChange={(e) => setSubmission((prev) => ({ ...prev, maxTesters: e.target.value }))}
                    required
                    className="bg-slate-800/50 border-slate-600 text-white"
                  />
                </div>
                <div>
                  <Label htmlFor="timeline" className="text-slate-200">
                    Testing Timeline *
                  </Label>
                  <select
                    id="timeline"
                    value={submission.timeline}
                    onChange={(e) => setSubmission((prev) => ({ ...prev, timeline: e.target.value }))}
                    required
                    className="w-full mt-1 bg-slate-800/50 border border-slate-600 rounded-md px-3 py-2 text-white"
                  >
                    <option value="">Select timeline</option>
                    <option value="asap">ASAP (Within 1 week)</option>
                    <option value="2weeks">Within 2 weeks</option>
                    <option value="month">Within 1 month</option>
                    <option value="flexible">Flexible timing</option>
                  </select>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Build & Requirements */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold bg-gradient-to-r from-purple-400 to-blue-400 bg-clip-text text-transparent flex items-center gap-2">
                <Upload className="h-5 w-5" />
                Build & Requirements
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <Label htmlFor="buildDownload" className="text-slate-200">
                  Build Download Link *
                </Label>
                <Input
                  id="buildDownload"
                  type="url"
                  value={submission.buildDownload}
                  onChange={(e) => setSubmission((prev) => ({ ...prev, buildDownload: e.target.value }))}
                  placeholder="https://drive.google.com/... or https://dropbox.com/..."
                  required
                  className="bg-slate-800/50 border-slate-600 text-white"
                />
                <p className="text-xs text-slate-400 mt-1">Provide a secure download link (Google Drive, Dropbox, etc.)</p>
              </div>

              <div>
                <Label htmlFor="specialRequirements" className="text-slate-200">
                  Special Requirements
                </Label>
                <Textarea
                  id="specialRequirements"
                  value={submission.specialRequirements}
                  onChange={(e) => setSubmission((prev) => ({ ...prev, specialRequirements: e.target.value }))}
                  placeholder="Any special hardware, software, or setup requirements for testing..."
                  rows={3}
                  className="bg-slate-800/50 border-slate-600 text-white"
                />
              </div>

              <div>
                <Label htmlFor="testingGoals" className="text-slate-200">
                  Testing Goals *
                </Label>
                <Textarea
                  id="testingGoals"
                  value={submission.testingGoals}
                  onChange={(e) => setSubmission((prev) => ({ ...prev, testingGoals: e.target.value }))}
                  placeholder="What specific feedback are you looking for? What aspects of the game should testers focus on?"
                  rows={4}
                  required
                  className="bg-slate-800/50 border-slate-600 text-white"
                />
              </div>
            </CardContent>
          </Card>

          {/* Terms and Submit */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardContent className="pt-6">
              <div className="space-y-6">
                <div className="flex items-start gap-3">
                  <Checkbox
                    id="terms"
                    checked={submission.agreeToTerms}
                    onCheckedChange={(checked) => setSubmission((prev) => ({ ...prev, agreeToTerms: !!checked }))}
                    className="mt-1"
                  />
                  <Label htmlFor="terms" className="text-slate-200 text-sm leading-relaxed">
                    I agree to the <span className="text-purple-400 hover:underline cursor-pointer">Developer Terms of Service</span> and{' '}
                    <span className="text-purple-400 hover:underline cursor-pointer">Testing Lab Guidelines</span>. I understand that my game will be reviewed
                    before testing sessions are scheduled.
                  </Label>
                </div>

                <Button
                  type="submit"
                  disabled={isSubmitting || !submission.agreeToTerms}
                  className="w-full bg-gradient-to-r from-purple-600 to-purple-700 hover:from-purple-700 hover:to-purple-800 text-white border-0"
                  size="lg"
                >
                  {isSubmitting ? 'Submitting Game...' : 'Submit Game for Review'}
                </Button>
              </div>
            </CardContent>
          </Card>
        </form>
      </div>
    </div>
  );
}
