'use client';

import { useCourseEditor } from '@/lib/courses/course-editor.context';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Textarea } from '@/components/ui/textarea';
import { Separator } from '@/components/ui/separator';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { 
  Globe, 
  Eye, 
  Settings, 
  Clock, 
  Play,
  Pause,
  Monitor,
  Tablet,
  Smartphone,
  Sun,
  Moon,
  RotateCcw,
  Trash2,
  Plus,
  CheckCircle,
  AlertCircle,
  Calendar,
  Users,
  Lock,
  Unlock,
  GitBranch,
  History,
  Bell,
  ExternalLink,
  Download,
  Share2
} from 'lucide-react';
import { useState } from 'react';
import { cn } from '@/lib/utils';

export default function PublishPage() {
  const { 
    state: course, 
    setVisibility, 
    setAccessType, 
    toggleAutoPublish, 
    setPublishDate, 
    createVersion, 
    restoreVersion, 
    deleteVersion, 
    setPreviewMode, 
    setPreviewTheme, 
    toggleLivePreview, 
    publishCourse, 
    unpublishCourse,
    setPublishingStatus 
  } = useCourseEditor();

  const [newVersionChanges, setNewVersionChanges] = useState('');
  const [showCreateVersion, setShowCreateVersion] = useState(false);
  const [showVersionHistory, setShowVersionHistory] = useState(false);
  const [selectedVersion, setSelectedVersion] = useState<string | null>(null);

  const publishing = course.publishing;
  const currentUser = 'Current User'; // Would come from auth context

  const handlePublish = async () => {
    setPublishingStatus('publishing');
    
    // Simulate publishing process
    setTimeout(() => {
      publishCourse();
      setPublishingStatus('published');
    }, 2000);
  };

  const handleCreateVersion = () => {
    if (newVersionChanges.trim()) {
      createVersion(newVersionChanges.trim(), currentUser);
      setNewVersionChanges('');
      setShowCreateVersion(false);
    }
  };

  const getPublishingStatusColor = (status: typeof publishing.publishingStatus) => {
    switch (status) {
      case 'published': return 'text-green-600';
      case 'publishing': return 'text-blue-600';
      case 'failed': return 'text-red-600';
      default: return 'text-gray-600';
    }
  };

  const getVisibilityIcon = (visibility: typeof publishing.settings.visibility) => {
    switch (visibility) {
      case 'public': return Globe;
      case 'private': return Lock;
      case 'unlisted': return Eye;
      default: return Globe;
    }
  };

  const formatDate = (date: Date) => {
    return new Intl.DateTimeFormat('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold flex items-center gap-2">
            <Play className="h-6 w-6" />
            Preview & Publish
          </h1>
          <p className="text-muted-foreground">
            Manage course publishing, versions, and preview settings
          </p>
        </div>
        
        <div className="flex items-center gap-3">
          <Badge 
            variant={course.status === 'published' ? 'default' : 'secondary'}
            className={cn(
              'flex items-center gap-1',
              getPublishingStatusColor(publishing.publishingStatus)
            )}
          >
            {publishing.publishingStatus === 'publishing' ? (
              <div className="animate-spin h-3 w-3 border border-current border-t-transparent rounded-full" />
            ) : course.status === 'published' ? (
              <CheckCircle className="h-3 w-3" />
            ) : (
              <AlertCircle className="h-3 w-3" />
            )}
            {publishing.publishingStatus === 'publishing' ? 'Publishing...' : course.status.charAt(0).toUpperCase() + course.status.slice(1)}
          </Badge>
          
          {course.status === 'published' ? (
            <Button onClick={unpublishCourse} variant="outline">
              <Pause className="h-4 w-4 mr-2" />
              Unpublish
            </Button>
          ) : (
            <Button 
              onClick={handlePublish} 
              disabled={!course.isValid || publishing.publishingStatus === 'publishing'}
            >
              <Play className="h-4 w-4 mr-2" />
              {publishing.publishingStatus === 'publishing' ? 'Publishing...' : 'Publish'}
            </Button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Publishing Settings */}
        <div className="lg:col-span-2 space-y-6">
          {/* Publish Settings */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Settings className="h-5 w-5" />
                Publishing Settings
              </CardTitle>
              <CardDescription>
                Control how and when your course is published
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="visibility">Visibility</Label>
                  <Select value={publishing.settings.visibility} onValueChange={setVisibility}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="public">
                        <div className="flex items-center gap-2">
                          <Globe className="h-4 w-4" />
                          Public - Anyone can find and enroll
                        </div>
                      </SelectItem>
                      <SelectItem value="unlisted">
                        <div className="flex items-center gap-2">
                          <Eye className="h-4 w-4" />
                          Unlisted - Only with direct link
                        </div>
                      </SelectItem>
                      <SelectItem value="private">
                        <div className="flex items-center gap-2">
                          <Lock className="h-4 w-4" />
                          Private - Invitation only
                        </div>
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="accessType">Access Type</Label>
                  <Select value={publishing.settings.accessType} onValueChange={setAccessType}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="free">Free Access</SelectItem>
                      <SelectItem value="paid">Paid Course</SelectItem>
                      <SelectItem value="enrollment">Enrollment Required</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Auto-publish changes</Label>
                    <p className="text-sm text-muted-foreground">
                      Automatically publish when you save changes
                    </p>
                  </div>
                  <Switch 
                    checked={publishing.settings.autoPublish}
                    onCheckedChange={toggleAutoPublish}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="publishDate">Scheduled Publish Date</Label>
                  <Input
                    id="publishDate"
                    type="datetime-local"
                    value={publishing.settings.publishAt ? 
                      new Date(publishing.settings.publishAt.getTime() - publishing.settings.publishAt.getTimezoneOffset() * 60000)
                        .toISOString().slice(0, 16) : ''
                    }
                    onChange={(e) => setPublishDate(e.target.value ? new Date(e.target.value) : undefined)}
                  />
                  <p className="text-xs text-muted-foreground">
                    Leave empty to publish immediately
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Version History */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <GitBranch className="h-5 w-5" />
                Version History
                <Badge variant="outline">{publishing.currentVersion}</Badge>
              </CardTitle>
              <CardDescription>
                Track changes and manage course versions
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex gap-2">
                <Dialog open={showCreateVersion} onOpenChange={setShowCreateVersion}>
                  <DialogTrigger asChild>
                    <Button variant="outline" size="sm">
                      <Plus className="h-4 w-4 mr-2" />
                      Create Version
                    </Button>
                  </DialogTrigger>
                  <DialogContent>
                    <DialogHeader>
                      <DialogTitle>Create New Version</DialogTitle>
                      <DialogDescription>
                        Document the changes made in this version
                      </DialogDescription>
                    </DialogHeader>
                    <div className="space-y-4">
                      <Textarea
                        placeholder="Describe the changes made in this version..."
                        value={newVersionChanges}
                        onChange={(e) => setNewVersionChanges(e.target.value)}
                        rows={4}
                      />
                      <div className="flex justify-end gap-2">
                        <Button variant="outline" onClick={() => setShowCreateVersion(false)}>
                          Cancel
                        </Button>
                        <Button onClick={handleCreateVersion} disabled={!newVersionChanges.trim()}>
                          Create Version
                        </Button>
                      </div>
                    </div>
                  </DialogContent>
                </Dialog>

                <Button 
                  variant="outline" 
                  size="sm"
                  onClick={() => setShowVersionHistory(!showVersionHistory)}
                >
                  <History className="h-4 w-4 mr-2" />
                  {showVersionHistory ? 'Hide' : 'Show'} History
                </Button>
              </div>

              {showVersionHistory && (
                <div className="space-y-2 max-h-60 overflow-y-auto">
                  {publishing.versions.length === 0 ? (
                    <p className="text-sm text-muted-foreground py-4 text-center">
                      No versions created yet
                    </p>
                  ) : (
                    publishing.versions.map((version) => (
                      <div key={version.id} className="flex items-center justify-between p-3 border rounded">
                        <div className="flex-1">
                          <div className="flex items-center gap-2">
                            <Badge variant="outline">{version.version}</Badge>
                            <span className="text-sm font-medium">{version.changes}</span>
                            {version.version === publishing.currentVersion && (
                              <Badge variant="default" className="text-xs">Current</Badge>
                            )}
                          </div>
                          <p className="text-xs text-muted-foreground mt-1">
                            {formatDate(version.createdAt)} by {version.createdBy}
                          </p>
                        </div>
                        <div className="flex items-center gap-1">
                          {version.version !== publishing.currentVersion && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => restoreVersion(version.id)}
                            >
                              <RotateCcw className="h-4 w-4" />
                            </Button>
                          )}
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => deleteVersion(version.id)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </div>
                    ))
                  )}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Notifications */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Bell className="h-5 w-5" />
                Publishing Notifications
              </CardTitle>
              <CardDescription>
                Get notified when your course is published or updated
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Email notifications</Label>
                    <p className="text-sm text-muted-foreground">
                      Send email when course is published
                    </p>
                  </div>
                  <Switch 
                    checked={publishing.settings.notifications.email}
                    onCheckedChange={(checked) => {
                      // Would dispatch notification setting change
                    }}
                  />
                </div>

                <div className="flex items-center justify-between">
                  <div className="space-y-0.5">
                    <Label>Slack notifications</Label>
                    <p className="text-sm text-muted-foreground">
                      Post to Slack when course is published
                    </p>
                  </div>
                  <Switch 
                    checked={publishing.settings.notifications.slack}
                    onCheckedChange={(checked) => {
                      // Would dispatch notification setting change
                    }}
                  />
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Preview Panel */}
        <div className="space-y-6">
          {/* Live Preview */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Eye className="h-5 w-5" />
                Live Preview
              </CardTitle>
              <CardDescription>
                Preview how your course will look to students
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <Label>Live preview</Label>
                <Switch 
                  checked={publishing.preview.livePreview}
                  onCheckedChange={toggleLivePreview}
                />
              </div>

              <div className="space-y-2">
                <Label>Device preview</Label>
                <div className="flex gap-1 p-1 bg-muted rounded-md">
                  <Button
                    variant={publishing.preview.mode === 'desktop' ? 'default' : 'ghost'}
                    size="sm"
                    onClick={() => setPreviewMode('desktop')}
                    className="flex-1"
                  >
                    <Monitor className="h-4 w-4" />
                  </Button>
                  <Button
                    variant={publishing.preview.mode === 'tablet' ? 'default' : 'ghost'}
                    size="sm"
                    onClick={() => setPreviewMode('tablet')}
                    className="flex-1"
                  >
                    <Tablet className="h-4 w-4" />
                  </Button>
                  <Button
                    variant={publishing.preview.mode === 'mobile' ? 'default' : 'ghost'}
                    size="sm"
                    onClick={() => setPreviewMode('mobile')}
                    className="flex-1"
                  >
                    <Smartphone className="h-4 w-4" />
                  </Button>
                </div>
              </div>

              <div className="space-y-2">
                <Label>Theme</Label>
                <div className="flex gap-1 p-1 bg-muted rounded-md">
                  <Button
                    variant={publishing.preview.theme === 'light' ? 'default' : 'ghost'}
                    size="sm"
                    onClick={() => setPreviewTheme('light')}
                    className="flex-1"
                  >
                    <Sun className="h-4 w-4" />
                  </Button>
                  <Button
                    variant={publishing.preview.theme === 'dark' ? 'default' : 'ghost'}
                    size="sm"
                    onClick={() => setPreviewTheme('dark')}
                    className="flex-1"
                  >
                    <Moon className="h-4 w-4" />
                  </Button>
                </div>
              </div>

              <Separator />

              <div className="space-y-2">
                <Button variant="outline" className="w-full" asChild>
                  <a href={`/courses/${course.slug}/preview`} target="_blank" rel="noopener noreferrer">
                    <ExternalLink className="h-4 w-4 mr-2" />
                    Open Preview
                  </a>
                </Button>
                
                <Button variant="outline" className="w-full">
                  <Share2 className="h-4 w-4 mr-2" />
                  Share Preview Link
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Publishing Status */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <CheckCircle className="h-5 w-5" />
                Publishing Status
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-3 text-sm">
                <div className="flex items-center justify-between">
                  <span>Course validation</span>
                  <div className="flex items-center gap-1">
                    {course.isValid ? (
                      <CheckCircle className="h-4 w-4 text-green-600" />
                    ) : (
                      <AlertCircle className="h-4 w-4 text-red-600" />
                    )}
                    <span className={course.isValid ? 'text-green-600' : 'text-red-600'}>
                      {course.isValid ? 'Valid' : 'Issues found'}
                    </span>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <span>Content complete</span>
                  <div className="flex items-center gap-1">
                    <CheckCircle className="h-4 w-4 text-green-600" />
                    <span className="text-green-600">Complete</span>
                  </div>
                </div>

                <div className="flex items-center justify-between">
                  <span>SEO optimized</span>
                  <div className="flex items-center gap-1">
                    {(course.metadata.seo.metaTitle && course.metadata.seo.metaDescription) ? (
                      <CheckCircle className="h-4 w-4 text-green-600" />
                    ) : (
                      <AlertCircle className="h-4 w-4 text-yellow-600" />
                    )}
                    <span className={(course.metadata.seo.metaTitle && course.metadata.seo.metaDescription) ? 'text-green-600' : 'text-yellow-600'}>
                      {(course.metadata.seo.metaTitle && course.metadata.seo.metaDescription) ? 'Optimized' : 'Needs work'}
                    </span>
                  </div>
                </div>
              </div>

              {publishing.lastPublishedAt && (
                <div className="pt-3 border-t">
                  <div className="flex items-center gap-2 text-sm text-muted-foreground">
                    <Calendar className="h-4 w-4" />
                    <span>Published {formatDate(publishing.lastPublishedAt)}</span>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Quick Actions */}
          <Card>
            <CardHeader>
              <CardTitle>Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button variant="outline" className="w-full justify-start">
                <Download className="h-4 w-4 mr-2" />
                Export Course Data
              </Button>
              
              <Button variant="outline" className="w-full justify-start">
                <Users className="h-4 w-4 mr-2" />
                View Analytics
              </Button>
              
              <Button variant="outline" className="w-full justify-start">
                <Settings className="h-4 w-4 mr-2" />
                Advanced Settings
              </Button>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
