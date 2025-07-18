'use client';

import { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Checkbox } from '@/components/ui/checkbox';
import { Badge } from '@/components/ui/badge';
import { ArrowLeft, UserPlus, Gamepad2, Monitor, Smartphone } from 'lucide-react';

interface JoinAsTesterProps {
  onBack: () => void;
  onSuccess: () => void;
}

interface TesterProfile {
  name: string;
  email: string;
  discord: string;
  experience: string;
  platforms: string[];
  genres: string[];
  availability: string;
  motivation: string;
  agreeToTerms: boolean;
}

const PLATFORMS = [
  { id: 'pc', name: 'PC/Windows', icon: Monitor },
  { id: 'mac', name: 'Mac', icon: Monitor },
  { id: 'playstation', name: 'PlayStation', icon: Gamepad2 },
  { id: 'xbox', name: 'Xbox', icon: Gamepad2 },
  { id: 'nintendo', name: 'Nintendo Switch', icon: Gamepad2 },
  { id: 'mobile-ios', name: 'iOS', icon: Smartphone },
  { id: 'mobile-android', name: 'Android', icon: Smartphone },
];

const GENRES = [
  'Action', 'Adventure', 'RPG', 'Strategy', 'Simulation', 'Sports',
  'Racing', 'Puzzle', 'Platformer', 'Fighting', 'Shooter', 'Horror',
  'Indie', 'Casual', 'Educational', 'Multiplayer'
];

export function JoinAsTester({ onBack, onSuccess }: JoinAsTesterProps) {
  const [profile, setProfile] = useState<TesterProfile>({
    name: '',
    email: '',
    discord: '',
    experience: '',
    platforms: [],
    genres: [],
    availability: '',
    motivation: '',
    agreeToTerms: false,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handlePlatformToggle = (platformId: string) => {
    setProfile(prev => ({
      ...prev,
      platforms: prev.platforms.includes(platformId)
        ? prev.platforms.filter(p => p !== platformId)
        : [...prev.platforms, platformId]
    }));
  };

  const handleGenreToggle = (genre: string) => {
    setProfile(prev => ({
      ...prev,
      genres: prev.genres.includes(genre)
        ? prev.genres.filter(g => g !== genre)
        : [...prev.genres, genre]
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!profile.agreeToTerms) {
      alert('Please agree to the terms and conditions');
      return;
    }

    setIsSubmitting(true);
    
    // Simulate API call
    await new Promise(resolve => setTimeout(resolve, 2000));
    
    setIsSubmitting(false);
    onSuccess();
  };

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="max-w-3xl mx-auto">
        {/* Header */}
        <div className="flex items-center gap-4 mb-8">
          <Button onClick={onBack} variant="ghost" className="text-slate-400 hover:text-white">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back
          </Button>
          
          <div>
            <h1 className="text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
              Join as Game Tester
            </h1>
            <p className="text-slate-400">Fill out your profile to get started</p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-8">
          {/* Basic Information */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent flex items-center gap-2">
                <UserPlus className="h-5 w-5" />
                Basic Information
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid md:grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="name" className="text-slate-200">Full Name *</Label>
                  <Input
                    id="name"
                    value={profile.name}
                    onChange={(e) => setProfile(prev => ({ ...prev, name: e.target.value }))}
                    required
                    className="bg-slate-800/50 border-slate-600 text-white"
                  />
                </div>
                <div>
                  <Label htmlFor="email" className="text-slate-200">Email Address *</Label>
                  <Input
                    id="email"
                    type="email"
                    value={profile.email}
                    onChange={(e) => setProfile(prev => ({ ...prev, email: e.target.value }))}
                    required
                    className="bg-slate-800/50 border-slate-600 text-white"
                  />
                </div>
              </div>
              
              <div>
                <Label htmlFor="discord" className="text-slate-200">Discord Username</Label>
                <Input
                  id="discord"
                  value={profile.discord}
                  onChange={(e) => setProfile(prev => ({ ...prev, discord: e.target.value }))}
                  placeholder="username#1234"
                  className="bg-slate-800/50 border-slate-600 text-white"
                />
                <p className="text-xs text-slate-400 mt-1">Used for testing session communication</p>
              </div>
            </CardContent>
          </Card>

          {/* Gaming Experience */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                Gaming Experience
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              <div>
                <Label htmlFor="experience" className="text-slate-200">Gaming Experience Level *</Label>
                <select
                  id="experience"
                  value={profile.experience}
                  onChange={(e) => setProfile(prev => ({ ...prev, experience: e.target.value }))}
                  required
                  className="w-full mt-1 bg-slate-800/50 border border-slate-600 rounded-md px-3 py-2 text-white"
                >
                  <option value="">Select your experience level</option>
                  <option value="casual">Casual Gamer (Few hours per week)</option>
                  <option value="regular">Regular Gamer (Several hours per week)</option>
                  <option value="enthusiast">Gaming Enthusiast (Daily gaming)</option>
                  <option value="professional">Professional/Competitive</option>
                </select>
              </div>

              <div>
                <Label className="text-slate-200">Gaming Platforms *</Label>
                <p className="text-xs text-slate-400 mb-3">Select all platforms you have access to</p>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
                  {PLATFORMS.map((platform) => {
                    const Icon = platform.icon;
                    const isSelected = profile.platforms.includes(platform.id);
                    return (
                      <button
                        key={platform.id}
                        type="button"
                        onClick={() => handlePlatformToggle(platform.id)}
                        className={`p-3 rounded-lg border transition-colors text-left ${
                          isSelected
                            ? 'border-blue-500 bg-blue-900/20 text-blue-300'
                            : 'border-slate-600 bg-slate-800/30 text-slate-400 hover:border-slate-500'
                        }`}
                      >
                        <Icon className="h-4 w-4 mb-1" />
                        <div className="text-sm font-medium">{platform.name}</div>
                      </button>
                    );
                  })}
                </div>
              </div>

              <div>
                <Label className="text-slate-200">Preferred Game Genres</Label>
                <p className="text-xs text-slate-400 mb-3">Select genres you enjoy testing (optional)</p>
                <div className="flex flex-wrap gap-2">
                  {GENRES.map((genre) => {
                    const isSelected = profile.genres.includes(genre);
                    return (
                      <button
                        key={genre}
                        type="button"
                        onClick={() => handleGenreToggle(genre)}
                        className={`px-3 py-1 rounded-full text-sm transition-colors ${
                          isSelected
                            ? 'bg-blue-600 text-white'
                            : 'bg-slate-800/50 text-slate-400 hover:bg-slate-700/50'
                        }`}
                      >
                        {genre}
                      </button>
                    );
                  })}
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Availability & Motivation */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                Availability & Motivation
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <Label htmlFor="availability" className="text-slate-200">Weekly Availability *</Label>
                <select
                  id="availability"
                  value={profile.availability}
                  onChange={(e) => setProfile(prev => ({ ...prev, availability: e.target.value }))}
                  required
                  className="w-full mt-1 bg-slate-800/50 border border-slate-600 rounded-md px-3 py-2 text-white"
                >
                  <option value="">Select your availability</option>
                  <option value="1-3">1-3 hours per week</option>
                  <option value="4-6">4-6 hours per week</option>
                  <option value="7-10">7-10 hours per week</option>
                  <option value="10+">More than 10 hours per week</option>
                </select>
              </div>

              <div>
                <Label htmlFor="motivation" className="text-slate-200">Why do you want to be a game tester?</Label>
                <Textarea
                  id="motivation"
                  value={profile.motivation}
                  onChange={(e) => setProfile(prev => ({ ...prev, motivation: e.target.value }))}
                  placeholder="Tell us about your motivation for testing games..."
                  rows={4}
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
                    checked={profile.agreeToTerms}
                    onCheckedChange={(checked) => setProfile(prev => ({ ...prev, agreeToTerms: !!checked }))}
                    className="mt-1"
                  />
                  <Label htmlFor="terms" className="text-slate-200 text-sm leading-relaxed">
                    I agree to the <span className="text-blue-400 hover:underline cursor-pointer">Terms of Service</span> and <span className="text-blue-400 hover:underline cursor-pointer">Privacy Policy</span>. I understand that I will receive testing assignments and commit to providing quality feedback.
                  </Label>
                </div>

                <Button
                  type="submit"
                  disabled={isSubmitting || !profile.agreeToTerms}
                  className="w-full bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white border-0"
                  size="lg"
                >
                  {isSubmitting ? 'Creating Profile...' : 'Complete Registration'}
                </Button>
              </div>
            </CardContent>
          </Card>
        </form>
      </div>
    </div>
  );
}
