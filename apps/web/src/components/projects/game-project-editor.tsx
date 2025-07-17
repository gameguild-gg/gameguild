'use client';

import { useState } from 'react';
import { Eye, Save, MoreHorizontal, Upload, Info } from 'lucide-react';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Badge } from '@game-guild/ui/components/badge';

interface GameProject {
  title: string;
  projectUrl: string;
  shortDescription: string;
  classification: string;
  projectType: string;
  releaseStatus: string;
  screenshots: string[];
  gameplayVideo?: string;
}

interface ProjectEditorProps {
  project?: Partial<GameProject>;
  onSave?: (project: GameProject) => void;
  onPreview?: () => void;
}

export function GameProjectEditor({ project, onSave, onPreview }: ProjectEditorProps) {
  const [formData, setFormData] = useState<GameProject>({
    title: project?.title || 'Nik The Dark Cat',
    projectUrl: project?.projectUrl || 'https://pawkumaster.itch.io/nik-the-dark-cat',
    shortDescription: project?.shortDescription || 'Help the black cat Nik to go home by passing four parkour levels',
    classification: project?.classification || 'Games',
    projectType: project?.projectType || 'HTML',
    releaseStatus: project?.releaseStatus || 'Released',
    screenshots: project?.screenshots || [],
    gameplayVideo: project?.gameplayVideo || '',
  });

  const handleInputChange = (field: keyof GameProject, value: string) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleSave = () => {
    onSave?.(formData);
  };

  return (
    <div className="bg-background text-foreground">
      {/* Header */}
      <div className="bg-card border-b border-border">
        <div className="max-w-7xl mx-auto px-6 py-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <span>Dashboard</span>
                <span>·</span>
                <span className="text-foreground">{formData.title}</span>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={onPreview}>
                <Eye className="w-4 h-4 mr-2" />
                View page
              </Button>
              <Button size="sm" onClick={handleSave} className="bg-red-600 hover:bg-red-700 text-white">
                <Save className="w-4 h-4 mr-2" />
                Save
              </Button>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto px-6 py-8">
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Left Column - Form */}
          <div className="lg:col-span-2 space-y-6">
            <div className="flex items-center gap-4 mb-6">
              <h1 className="text-2xl font-bold">Edit game</h1>
              <div className="flex items-center gap-2">
                <Badge variant="secondary">Devlog</Badge>
                <Badge variant="secondary">Metadata</Badge>
                <Badge variant="secondary">Analytics</Badge>
                <Badge variant="secondary">Distribute</Badge>
                <Badge variant="secondary">Interact</Badge>
                <Button variant="ghost" size="sm">
                  <MoreHorizontal className="w-4 h-4" />
                </Button>
              </div>
            </div>

            {/* Title */}
            <div className="space-y-2">
              <Label htmlFor="title" className="text-sm font-medium">
                Title
              </Label>
              <Input id="title" value={formData.title} onChange={(e) => handleInputChange('title', e.target.value)} className="bg-background border-border" />
            </div>

            {/* Project URL */}
            <div className="space-y-2">
              <Label htmlFor="projectUrl" className="text-sm font-medium">
                Project URL
              </Label>
              <Input
                id="projectUrl"
                value={formData.projectUrl}
                onChange={(e) => handleInputChange('projectUrl', e.target.value)}
                className="bg-background border-border"
              />
            </div>

            {/* Short Description */}
            <div className="space-y-2">
              <Label htmlFor="description" className="text-sm font-medium">
                Short description or tagline
              </Label>
              <div className="text-xs text-muted-foreground mb-2">Shown when someone shares a link to your project's page</div>
              <Textarea
                id="description"
                value={formData.shortDescription}
                onChange={(e) => handleInputChange('shortDescription', e.target.value)}
                rows={3}
                className="bg-background border-border resize-none"
              />
            </div>

            {/* Classification */}
            <div className="space-y-2">
              <Label className="text-sm font-medium">Classification</Label>
              <div className="text-xs text-muted-foreground mb-2">What are you uploading?</div>
              <Select value={formData.classification} onValueChange={(value) => handleInputChange('classification', value)}>
                <SelectTrigger className="bg-background border-border">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Games">Games — A piece of software you can play</SelectItem>
                  <SelectItem value="Tools">Tools — Software development tools</SelectItem>
                  <SelectItem value="Assets">Assets — Game assets and resources</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {/* Kind of Project */}
            <div className="space-y-2">
              <Label className="text-sm font-medium">Kind of project</Label>
              <Select value={formData.projectType} onValueChange={(value) => handleInputChange('projectType', value)}>
                <SelectTrigger className="bg-background border-border">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="HTML">HTML — You have a ZIP or HTML file that will be played in the browser</SelectItem>
                  <SelectItem value="Executable">Executable — You have an executable file</SelectItem>
                  <SelectItem value="Flash">Flash — You have a Flash file</SelectItem>
                </SelectContent>
              </Select>
              <div className="flex items-center gap-2 text-xs text-muted-foreground mt-2">
                <Info className="w-3 h-3" />
                <span>You can add additional downloadable files for any of the types above</span>
              </div>
            </div>

            {/* Release Status */}
            <div className="space-y-2">
              <Label className="text-sm font-medium">Release status</Label>
              <Select value={formData.releaseStatus} onValueChange={(value) => handleInputChange('releaseStatus', value)}>
                <SelectTrigger className="bg-background border-border">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Released">Released — A released project is a final product</SelectItem>
                  <SelectItem value="In Development">In Development — Project is still being worked on</SelectItem>
                  <SelectItem value="Prototype">Prototype — Early stage project</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Right Column - Preview */}
          <div className="space-y-6">
            {/* Game Preview Card */}
            <Card className="bg-card border-border overflow-hidden">
              <div className="relative">
                {/* Game Cover Image */}
                <div className="aspect-square bg-gradient-to-br from-gray-800 to-gray-900 flex items-center justify-center relative">
                  <div className="text-center">
                    {/* Pixel Art Cat */}
                    <div className="w-24 h-24 mx-auto mb-4 relative">
                      <div className="absolute inset-0 bg-black rounded-lg"></div>
                      {/* Cat Eyes */}
                      <div className="absolute top-6 left-4 w-3 h-3 bg-yellow-400 rounded-full"></div>
                      <div className="absolute top-6 right-4 w-3 h-3 bg-yellow-400 rounded-full"></div>
                      {/* Cat Body */}
                      <div className="absolute bottom-4 inset-x-2 h-8 bg-gray-800 rounded"></div>
                    </div>
                    <h3 className="text-white font-bold text-lg mb-1">NIK</h3>
                    <h4 className="text-white font-bold">THE DARK CAT</h4>
                    <p className="text-gray-300 text-sm mt-2">BY ISAAC REIS</p>
                  </div>
                </div>

                <CardContent className="p-4">
                  <div className="text-sm text-muted-foreground mb-4">
                    The cover image is used whenever Nik wants to link to your project. Recommended dimensions: 630x500
                  </div>

                  {/* Gameplay Video */}
                  <div className="space-y-2">
                    <Label className="text-sm font-medium">Gameplay video or trailer</Label>
                    <div className="text-xs text-muted-foreground">Provide a link to YouTube or Vimeo</div>
                    <Input
                      placeholder="eg: https://www.youtube.com/watch?v=s6Aa5pP"
                      value={formData.gameplayVideo}
                      onChange={(e) => handleInputChange('gameplayVideo', e.target.value)}
                      className="bg-background border-border text-xs"
                    />
                  </div>

                  {/* Screenshots */}
                  <div className="space-y-2 mt-4">
                    <Label className="text-sm font-medium">Screenshots</Label>
                    <div className="text-xs text-muted-foreground">Gameplay not highly recommended. Upload 3 to 5 for best results.</div>

                    <div className="border-2 border-dashed border-border rounded-lg p-6 text-center">
                      <Upload className="w-8 h-8 mx-auto mb-2 text-muted-foreground" />
                      <div className="text-sm text-muted-foreground">Drop files here or click to browse</div>
                    </div>

                    {/* Sample Screenshots */}
                    <div className="grid grid-cols-2 gap-2 mt-4">
                      <div className="aspect-video bg-gradient-to-br from-green-600 to-green-800 rounded border border-border flex items-center justify-center">
                        <div className="w-8 h-8 bg-black rounded"></div>
                      </div>
                      <div className="aspect-video bg-gradient-to-br from-blue-600 to-blue-800 rounded border border-border flex items-center justify-center">
                        <div className="w-8 h-8 bg-black rounded"></div>
                      </div>
                    </div>
                  </div>
                </CardContent>
              </div>
            </Card>
          </div>
        </div>
      </div>
    </div>
  );
}
