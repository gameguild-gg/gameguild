'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { 
  Settings, 
  Users, 
  Calendar, 
  Shield, 
  Archive, 
  Trash2, 
  AlertTriangle,
  Copy,
  ExternalLink,
  ChevronDown,
  ChevronRight
} from 'lucide-react';
import { useCourseEditor } from '@/lib/courses/course-editor.context';
import { useState } from 'react';

export default function CourseSettingsPage() {
  const { state, setEnrollmentStatus } = useCourseEditor();
  const [openSections, setOpenSections] = useState({
    status: true, // First section open by default
    enrollment: false,
    access: false,
    schedule: false,
    danger: false,
  });

  const toggleSection = (section: keyof typeof openSections) => {
    setOpenSections(prev => ({
      ...prev,
      [section]: !prev[section]
    }));
  };

  return (
    <div className="flex-1 flex flex-col min-h-0">
      {/* Sticky Header */}
      <div className="sticky top-0 z-10 border-b border-border bg-background/95 backdrop-blur-sm">
        <div className="p-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-bold text-foreground">Course Settings</h1>
              <p className="text-sm text-muted-foreground">Manage course visibility, enrollment settings, and advanced options</p>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 p-6 overflow-auto">
        <div className="max-w-4xl mx-auto space-y-6">
          
          {/* Course Status */}
          <Collapsible open={openSections.status} onOpenChange={() => toggleSection('status')}>
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CollapsibleTrigger asChild>
                <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                  <CardTitle className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Settings className="h-5 w-5" />
                      Course Status & Visibility
                    </div>
                    {openSections.status ? (
                      <ChevronDown className="h-4 w-4" />
                    ) : (
                      <ChevronRight className="h-4 w-4" />
                    )}
                  </CardTitle>
                </CardHeader>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <CardContent className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <Label className="text-base font-medium">Course Status</Label>
                      <p className="text-sm text-muted-foreground">Control whether your course is published or in draft mode</p>
                    </div>
                    <Badge variant={state.status === 'published' ? 'default' : 'secondary'}>
                      {state.status === 'published' ? 'Published' : 'Draft'}
                    </Badge>
                  </div>
                  
                  <div className="flex items-center justify-between">
                    <div>
                      <Label className="text-base font-medium">Course URL</Label>
                      <p className="text-sm text-muted-foreground">Public link to your course</p>
                    </div>
                    <div className="flex items-center gap-2">
                      <code className="px-2 py-1 bg-muted rounded text-sm">/courses/{state.slug}</code>
                      <Button variant="outline" size="sm">
                        <Copy className="h-3 w-3" />
                      </Button>
                      <Button variant="outline" size="sm">
                        <ExternalLink className="h-3 w-3" />
                      </Button>
                    </div>
                  </div>
                </CardContent>
              </CollapsibleContent>
            </Card>
          </Collapsible>

          {/* Enrollment Settings */}
          <Collapsible open={openSections.enrollment} onOpenChange={() => toggleSection('enrollment')}>
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CollapsibleTrigger asChild>
                <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                  <CardTitle className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Users className="h-5 w-5" />
                      Enrollment Settings
                    </div>
                    {openSections.enrollment ? (
                      <ChevronDown className="h-4 w-4" />
                    ) : (
                      <ChevronRight className="h-4 w-4" />
                    )}
                  </CardTitle>
                </CardHeader>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <CardContent className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <Label className="text-base font-medium">Open Enrollment</Label>
                      <p className="text-sm text-muted-foreground">Allow new students to enroll in the course</p>
                    </div>
                    <Switch checked={state.enrollment.isOpen} onCheckedChange={setEnrollmentStatus} />
                  </div>
                  
                  <Separator />
                  
                  <div className="space-y-3">
                    <div>
                      <Label htmlFor="max-enrollments">Maximum Enrollments</Label>
                      <Input id="max-enrollments" type="number" placeholder="Leave empty for unlimited" className="w-full" />
                      <p className="text-xs text-muted-foreground mt-1">Set a limit on how many students can enroll</p>
                    </div>
                    
                    <div>
                      <Label htmlFor="enrollment-deadline">Enrollment Deadline</Label>
                      <Input id="enrollment-deadline" type="date" className="w-full" />
                      <p className="text-xs text-muted-foreground mt-1">Last date students can enroll in the course</p>
                    </div>
                  </div>
                </CardContent>
              </CollapsibleContent>
            </Card>
          </Collapsible>

          {/* Course Access & Prerequisites */}
          <Collapsible open={openSections.access} onOpenChange={() => toggleSection('access')}>
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CollapsibleTrigger asChild>
                <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                  <CardTitle className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Shield className="h-5 w-5" />
                      Access Control
                    </div>
                    {openSections.access ? (
                      <ChevronDown className="h-4 w-4" />
                    ) : (
                      <ChevronRight className="h-4 w-4" />
                    )}
                  </CardTitle>
                </CardHeader>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <CardContent className="space-y-4">
                  <div className="space-y-3">
                    <div>
                      <Label htmlFor="prerequisites">Prerequisites</Label>
                      <Textarea id="prerequisites" placeholder="List any required knowledge or courses students should complete first" rows={3} />
                    </div>
                    
                    <div className="flex items-center justify-between">
                      <div>
                        <Label className="text-base font-medium">Require Approval</Label>
                        <p className="text-sm text-muted-foreground">Manually approve enrollment requests</p>
                      </div>
                      <Switch />
                    </div>
                    
                    <div className="flex items-center justify-between">
                      <div>
                        <Label className="text-base font-medium">Member Only</Label>
                        <p className="text-sm text-muted-foreground">Restrict access to guild members only</p>
                      </div>
                      <Switch />
                    </div>
                  </div>
                </CardContent>
              </CollapsibleContent>
            </Card>
          </Collapsible>

          {/* Course Scheduling */}
          <Collapsible open={openSections.schedule} onOpenChange={() => toggleSection('schedule')}>
            <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
              <CollapsibleTrigger asChild>
                <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                  <CardTitle className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Calendar className="h-5 w-5" />
                      Course Schedule
                    </div>
                    {openSections.schedule ? (
                      <ChevronDown className="h-4 w-4" />
                    ) : (
                      <ChevronRight className="h-4 w-4" />
                    )}
                  </CardTitle>
                </CardHeader>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <Label htmlFor="start-date">Course Start Date</Label>
                      <Input id="start-date" type="date" />
                    </div>
                    <div>
                      <Label htmlFor="end-date">Course End Date</Label>
                      <Input id="end-date" type="date" />
                    </div>
                  </div>
                  
                  <div className="flex items-center justify-between">
                    <div>
                      <Label className="text-base font-medium">Self-Paced</Label>
                      <p className="text-sm text-muted-foreground">Students can complete lessons at their own pace</p>
                    </div>
                    <Switch defaultChecked />
                  </div>
                </CardContent>
              </CollapsibleContent>
            </Card>
          </Collapsible>

          {/* Danger Zone */}
          <Collapsible open={openSections.danger} onOpenChange={() => toggleSection('danger')}>
            <Card className="shadow-lg border-destructive/20 bg-destructive/5 backdrop-blur-sm">
              <CollapsibleTrigger asChild>
                <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                  <CardTitle className="flex items-center justify-between text-destructive">
                    <div className="flex items-center gap-2">
                      <AlertTriangle className="h-5 w-5" />
                      Danger Zone
                    </div>
                    {openSections.danger ? (
                      <ChevronDown className="h-4 w-4" />
                    ) : (
                      <ChevronRight className="h-4 w-4" />
                    )}
                  </CardTitle>
                </CardHeader>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <CardContent className="space-y-4">
                  <div className="flex items-center justify-between">
                    <div>
                      <Label className="text-base font-medium">Archive Course</Label>
                      <p className="text-sm text-muted-foreground">Hide the course from public listings but keep it accessible to enrolled students</p>
                    </div>
                    <Button variant="outline" className="gap-2">
                      <Archive className="h-4 w-4" />
                      Archive
                    </Button>
                  </div>
                  
                  <Separator />
                  
                  <div className="flex items-center justify-between">
                    <div>
                      <Label className="text-base font-medium text-destructive">Delete Course</Label>
                      <p className="text-sm text-muted-foreground">Permanently delete this course and all its content. This action cannot be undone.</p>
                    </div>
                    <Button variant="destructive" className="gap-2">
                      <Trash2 className="h-4 w-4" />
                      Delete
                    </Button>
                  </div>
                </CardContent>
              </CollapsibleContent>
            </Card>
          </Collapsible>
        </div>
      </div>
    </div>
  );
}
