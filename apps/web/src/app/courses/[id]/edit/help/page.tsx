'use client';

import { useCourseEditor } from '@/lib/courses/course-editor.context';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { 
  HelpCircle, 
  BookOpen, 
  CheckCircle, 
  AlertCircle,
  ChevronDown,
  ChevronRight,
  Lightbulb,
  Video,
  FileText,
  MessageCircle,
  ExternalLink,
  Star,
  Clock,
  Target,
  Zap,
  Users,
  TrendingUp,
  Award,
  Search,
  Play,
  Settings,
  DollarSign
} from 'lucide-react';
import { useState } from 'react';
import { cn } from '@/lib/utils';

interface HelpItem {
  id: string;
  title: string;
  description: string;
  completed: boolean;
  priority: 'high' | 'medium' | 'low';
  category: string;
  estimatedTime: string;
}

interface GuideSection {
  id: string;
  title: string;
  description: string;
  icon: React.ComponentType<any>;
  items: HelpItem[];
}

export default function HelpPage() {
  const { state: course } = useCourseEditor();
  const [expandedSections, setExpandedSections] = useState<Set<string>>(new Set(['getting-started']));
  const [showCompleted, setShowCompleted] = useState(false);

  const toggleSection = (sectionId: string) => {
    const newExpanded = new Set(expandedSections);
    if (newExpanded.has(sectionId)) {
      newExpanded.delete(sectionId);
    } else {
      newExpanded.add(sectionId);
    }
    setExpandedSections(newExpanded);
  };

  const guideSections: GuideSection[] = [
    {
      id: 'getting-started',
      title: 'Getting Started',
      description: 'Essential steps to set up your course',
      icon: Star,
      items: [
        {
          id: 'course-title',
          title: 'Set course title and description',
          description: 'Choose a clear, descriptive title that tells students what they\'ll learn',
          completed: !!course.title && !!course.description,
          priority: 'high',
          category: 'content',
          estimatedTime: '5 min'
        },
        {
          id: 'course-category',
          title: 'Select course category and difficulty',
          description: 'Help students find your course by setting the right category and difficulty level',
          completed: !!course.category && !!course.difficulty,
          priority: 'high',
          category: 'setup',
          estimatedTime: '2 min'
        },
        {
          id: 'course-media',
          title: 'Add course thumbnail and preview video',
          description: 'Visual elements that will attract students to your course',
          completed: !!course.media.thumbnail || !!course.media.showcaseVideo,
          priority: 'medium',
          category: 'content',
          estimatedTime: '10 min'
        },
        {
          id: 'course-summary',
          title: 'Write compelling course summary',
          description: 'A brief overview that explains the value students will get',
          completed: !!course.summary,
          priority: 'high',
          category: 'content',
          estimatedTime: '15 min'
        }
      ]
    },
    {
      id: 'content-creation',
      title: 'Content & Structure',
      description: 'Build engaging course content',
      icon: BookOpen,
      items: [
        {
          id: 'learning-objectives',
          title: 'Define learning objectives',
          description: 'Clear goals help students understand what they\'ll achieve',
          completed: course.certificates.skillsProvided.length > 0,
          priority: 'high',
          category: 'content',
          estimatedTime: '20 min'
        },
        {
          id: 'course-structure',
          title: 'Organize course modules and lessons',
          description: 'Structure your content for optimal learning progression',
          completed: false, // Would check actual course content
          priority: 'high',
          category: 'content',
          estimatedTime: '60 min'
        },
        {
          id: 'interactive-elements',
          title: 'Add quizzes and assignments',
          description: 'Interactive content improves engagement and learning outcomes',
          completed: false,
          priority: 'medium',
          category: 'engagement',
          estimatedTime: '45 min'
        },
        {
          id: 'supplementary-materials',
          title: 'Upload resources and materials',
          description: 'Additional files, readings, and resources for students',
          completed: false,
          priority: 'medium',
          category: 'content',
          estimatedTime: '30 min'
        }
      ]
    },
    {
      id: 'marketing-seo',
      title: 'Marketing & Discovery',
      description: 'Make your course discoverable',
      icon: TrendingUp,
      items: [
        {
          id: 'seo-basics',
          title: 'Optimize for search engines',
          description: 'Set meta titles, descriptions, and keywords for better discoverability',
          completed: !!(course.metadata.seo.metaTitle && course.metadata.seo.metaDescription && course.metadata.seo.keywords.length > 0),
          priority: 'high',
          category: 'marketing',
          estimatedTime: '25 min'
        },
        {
          id: 'social-media',
          title: 'Configure social media sharing',
          description: 'Optimize how your course appears when shared on social platforms',
          completed: !!(course.metadata.seo.ogTitle && course.metadata.seo.ogImage),
          priority: 'medium',
          category: 'marketing',
          estimatedTime: '15 min'
        },
        {
          id: 'course-tags',
          title: 'Add relevant tags',
          description: 'Tags help students find your course through search and recommendations',
          completed: course.tags.length >= 3,
          priority: 'medium',
          category: 'marketing',
          estimatedTime: '10 min'
        },
        {
          id: 'pricing-strategy',
          title: 'Set up pricing and enrollment',
          description: 'Choose the right pricing model and enrollment settings',
          completed: course.products.length > 0,
          priority: 'high',
          category: 'business',
          estimatedTime: '20 min'
        }
      ]
    },
    {
      id: 'advanced-features',
      title: 'Advanced Features',
      description: 'Enhance your course with advanced capabilities',
      icon: Zap,
      items: [
        {
          id: 'certificates',
          title: 'Set up certificates and skills',
          description: 'Define what skills students will gain and certificate requirements',
          completed: course.certificates.certificates.length > 0,
          priority: 'medium',
          category: 'credentials',
          estimatedTime: '30 min'
        },
        {
          id: 'delivery-options',
          title: 'Configure delivery and schedule',
          description: 'Set up live sessions, cohorts, or self-paced learning',
          completed: course.delivery.mode !== 'online' ? course.delivery.sessions.length > 0 : true,
          priority: 'medium',
          category: 'delivery',
          estimatedTime: '25 min'
        },
        {
          id: 'analytics-tracking',
          title: 'Enable analytics and tracking',
          description: 'Track student progress and course performance',
          completed: !!course.metadata.analytics?.trackingId,
          priority: 'low',
          category: 'analytics',
          estimatedTime: '15 min'
        },
        {
          id: 'community-features',
          title: 'Set up community and discussions',
          description: 'Enable student discussions and community features',
          completed: false,
          priority: 'low',
          category: 'community',
          estimatedTime: '20 min'
        }
      ]
    }
  ];

  const allItems = guideSections.flatMap(section => section.items);
  const completedItems = allItems.filter(item => item.completed);
  const highPriorityIncomplete = allItems.filter(item => !item.completed && item.priority === 'high');
  const completionPercentage = Math.round((completedItems.length / allItems.length) * 100);

  const getPriorityColor = (priority: string) => {
    switch (priority) {
      case 'high': return 'bg-red-100 text-red-800';
      case 'medium': return 'bg-yellow-100 text-yellow-800';
      case 'low': return 'bg-green-100 text-green-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getSectionIcon = (IconComponent: React.ComponentType<any>) => {
    return <IconComponent className="h-5 w-5" />;
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold flex items-center gap-2">
            <HelpCircle className="h-6 w-6" />
            Help & Guidance
          </h1>
          <p className="text-muted-foreground">
            Complete course setup guide and helpful resources
          </p>
        </div>
        
        <div className="flex items-center gap-4">
          <div className="text-right">
            <div className="text-2xl font-bold text-green-600">{completionPercentage}%</div>
            <div className="text-sm text-muted-foreground">Complete</div>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Progress & Quick Actions */}
        <div className="lg:col-span-2 space-y-6">
          {/* Progress Overview */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Target className="h-5 w-5" />
                Course Setup Progress
              </CardTitle>
              <CardDescription>
                Track your progress through the course creation process
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <span className="text-sm font-medium">Overall Progress</span>
                  <span className="text-sm text-muted-foreground">{completedItems.length} of {allItems.length} completed</span>
                </div>
                <div className="w-full bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-green-600 h-2 rounded-full transition-all duration-300" 
                    style={{ width: `${completionPercentage}%` }}
                  />
                </div>
                
                {highPriorityIncomplete.length > 0 && (
                  <div className="mt-4 p-3 bg-red-50 rounded-md">
                    <div className="flex items-center gap-2 text-red-800 mb-2">
                      <AlertCircle className="h-4 w-4" />
                      <span className="font-medium">High Priority Items ({highPriorityIncomplete.length})</span>
                    </div>
                    <ul className="text-sm text-red-700 space-y-1">
                      {highPriorityIncomplete.slice(0, 3).map(item => (
                        <li key={item.id}>• {item.title}</li>
                      ))}
                      {highPriorityIncomplete.length > 3 && (
                        <li>• And {highPriorityIncomplete.length - 3} more...</li>
                      )}
                    </ul>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Course Setup Guide */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <BookOpen className="h-5 w-5" />
                  Setup Guide
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowCompleted(!showCompleted)}
                >
                  {showCompleted ? 'Hide' : 'Show'} Completed
                </Button>
              </CardTitle>
              <CardDescription>
                Step-by-step guidance to create a successful course
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {guideSections.map((section) => {
                const sectionItems = showCompleted ? section.items : section.items.filter(item => !item.completed);
                const completedCount = section.items.filter(item => item.completed).length;
                
                if (!showCompleted && sectionItems.length === 0) return null;

                return (
                  <Collapsible
                    key={section.id}
                    open={expandedSections.has(section.id)}
                    onOpenChange={() => toggleSection(section.id)}
                  >
                    <CollapsibleTrigger asChild>
                      <Button variant="ghost" className="w-full justify-between p-4 h-auto">
                        <div className="flex items-center gap-3 text-left">
                          {getSectionIcon(section.icon)}
                          <div>
                            <div className="font-medium">{section.title}</div>
                            <div className="text-sm text-muted-foreground">{section.description}</div>
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <Badge variant="outline">
                            {completedCount}/{section.items.length}
                          </Badge>
                          {expandedSections.has(section.id) ? (
                            <ChevronDown className="h-4 w-4" />
                          ) : (
                            <ChevronRight className="h-4 w-4" />
                          )}
                        </div>
                      </Button>
                    </CollapsibleTrigger>
                    <CollapsibleContent className="pt-2">
                      <div className="pl-8 space-y-3">
                        {sectionItems.map((item) => (
                          <div
                            key={item.id}
                            className={cn(
                              "flex items-start gap-3 p-3 rounded-md border",
                              item.completed ? "bg-green-50 border-green-200" : "bg-white"
                            )}
                          >
                            <div className="mt-0.5">
                              {item.completed ? (
                                <CheckCircle className="h-4 w-4 text-green-600" />
                              ) : (
                                <div className="h-4 w-4 rounded-full border-2 border-gray-300" />
                              )}
                            </div>
                            <div className="flex-1 min-w-0">
                              <div className="flex items-center gap-2 flex-wrap">
                                <h4 className="font-medium text-sm">{item.title}</h4>
                                <Badge variant="outline" className={getPriorityColor(item.priority)}>
                                  {item.priority}
                                </Badge>
                                <div className="flex items-center gap-1 text-xs text-muted-foreground">
                                  <Clock className="h-3 w-3" />
                                  {item.estimatedTime}
                                </div>
                              </div>
                              <p className="text-sm text-muted-foreground mt-1">
                                {item.description}
                              </p>
                            </div>
                          </div>
                        ))}
                      </div>
                    </CollapsibleContent>
                  </Collapsible>
                );
              })}
            </CardContent>
          </Card>

          {/* Quick Tips */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Lightbulb className="h-5 w-5" />
                Pro Tips
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-start gap-3 p-3 bg-blue-50 rounded-md">
                <Lightbulb className="h-4 w-4 text-blue-600 mt-0.5" />
                <div>
                  <h4 className="font-medium text-sm text-blue-900">Start with learning outcomes</h4>
                  <p className="text-sm text-blue-700">Define what students will achieve before creating content. This helps focus your course and attracts the right audience.</p>
                </div>
              </div>
              
              <div className="flex items-start gap-3 p-3 bg-green-50 rounded-md">
                <Users className="h-4 w-4 text-green-600 mt-0.5" />
                <div>
                  <h4 className="font-medium text-sm text-green-900">Know your audience</h4>
                  <p className="text-sm text-green-700">Understanding your target students helps you create relevant content and choose appropriate difficulty levels.</p>
                </div>
              </div>
              
              <div className="flex items-start gap-3 p-3 bg-purple-50 rounded-md">
                <TrendingUp className="h-4 w-4 text-purple-600 mt-0.5" />
                <div>
                  <h4 className="font-medium text-sm text-purple-900">Optimize for discovery</h4>
                  <p className="text-sm text-purple-700">Use clear titles, detailed descriptions, and relevant tags to help students find your course through search.</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Resources & Support */}
        <div className="space-y-6">
          {/* Quick Navigation */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Settings className="h-5 w-5" />
                Quick Navigation
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              <Button variant="ghost" className="w-full justify-start" asChild>
                <a href="../details">
                  <FileText className="h-4 w-4 mr-2" />
                  Course Details
                </a>
              </Button>
              
              <Button variant="ghost" className="w-full justify-start" asChild>
                <a href="../certificates">
                  <Award className="h-4 w-4 mr-2" />
                  Certificates & Skills
                </a>
              </Button>
              
              <Button variant="ghost" className="w-full justify-start" asChild>
                <a href="../seo">
                  <Search className="h-4 w-4 mr-2" />
                  SEO & Metadata
                </a>
              </Button>
              
              <Button variant="ghost" className="w-full justify-start" asChild>
                <a href="../publish">
                  <Play className="h-4 w-4 mr-2" />
                  Preview & Publish
                </a>
              </Button>
              
              <Button variant="ghost" className="w-full justify-start" asChild>
                <a href="../pricing">
                  <DollarSign className="h-4 w-4 mr-2" />
                  Pricing & Sales
                </a>
              </Button>
            </CardContent>
          </Card>

          {/* Help Resources */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <HelpCircle className="h-5 w-5" />
                Help Resources
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <Button variant="outline" className="w-full justify-start" asChild>
                <a href="/docs/course-creation" target="_blank" rel="noopener noreferrer">
                  <FileText className="h-4 w-4 mr-2" />
                  Course Creation Guide
                  <ExternalLink className="h-3 w-3 ml-auto" />
                </a>
              </Button>
              
              <Button variant="outline" className="w-full justify-start" asChild>
                <a href="/docs/video-tutorials" target="_blank" rel="noopener noreferrer">
                  <Video className="h-4 w-4 mr-2" />
                  Video Tutorials
                  <ExternalLink className="h-3 w-3 ml-auto" />
                </a>
              </Button>
              
              <Button variant="outline" className="w-full justify-start" asChild>
                <a href="/support" target="_blank" rel="noopener noreferrer">
                  <MessageCircle className="h-4 w-4 mr-2" />
                  Contact Support
                  <ExternalLink className="h-3 w-3 ml-auto" />
                </a>
              </Button>
              
              <Separator />
              
              <Button variant="outline" className="w-full justify-start" asChild>
                <a href="/community" target="_blank" rel="noopener noreferrer">
                  <Users className="h-4 w-4 mr-2" />
                  Creator Community
                  <ExternalLink className="h-3 w-3 ml-auto" />
                </a>
              </Button>
            </CardContent>
          </Card>

          {/* Best Practices */}
          <Card>
            <CardHeader>
              <CardTitle>Best Practices</CardTitle>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div>
                <h4 className="font-medium mb-1">Course Title</h4>
                <p className="text-muted-foreground">Keep it under 60 characters and include key benefits or outcomes.</p>
              </div>
              
              <div>
                <h4 className="font-medium mb-1">Description</h4>
                <p className="text-muted-foreground">Use bullet points to highlight key learning outcomes and course benefits.</p>
              </div>
              
              <div>
                <h4 className="font-medium mb-1">Pricing</h4>
                <p className="text-muted-foreground">Research competitor pricing and consider your target audience's budget.</p>
              </div>
              
              <div>
                <h4 className="font-medium mb-1">Preview Video</h4>
                <p className="text-muted-foreground">Keep it under 2 minutes and show course content, not just talking heads.</p>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
