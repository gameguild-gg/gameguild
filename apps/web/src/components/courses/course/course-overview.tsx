'use client';

import MarkdownRenderer from '@/components/markdown-renderer/markdown-renderer';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Program, ProgramContent, ProgramContentType } from '@/lib/api/generated';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import { BookOpen, CheckCircle, Clock, Code, FileText, MessageSquare, Target } from 'lucide-react';
import { useState } from 'react';

interface CourseOverviewProps {
  readonly course: Program;
  readonly levelConfig?: {
    name: string;
    color: string;
    bgColor: string;
  };
}

const LEARNING_OBJECTIVES = ['Master the fundamentals', 'Build practical projects to reinforce learning', 'Apply industry best practices and workflows', 'Prepare for real-world development challenges'] as const;
function normalizeList(value: unknown): string[] {
  if (Array.isArray(value)) {
    return value.map((item) => String(item).trim()).filter(Boolean);
  }

  if (typeof value === 'string') {
    return value
      .split(/[,;\n]/)
      .map((item) => item.trim())
      .filter(Boolean);
  }

  return [];
}

export function CourseOverview({ course, levelConfig }: CourseOverviewProps) {
  const [selectedContent, setSelectedContent] = useState<ProgramContent | null>(
    course.programContents?.find((content: any) => !content.parentId) || null
  );

  const courseDifficulty = course.difficulty as string | number | null | undefined;
  const courseCategory = course.category as string | number | null | undefined;
  const config = levelConfig || getCourseLevelConfig(courseDifficulty);
  const categoryName = getCourseCategoryName(courseCategory);
  const publishedSkills = normalizeList(course['skillsProvided']);
  const requiredSkills = normalizeList(course['skillsRequired']);

  const topLevelContent = course.programContents?.filter((content: any) => !content.parentId) || [];
  const childContent = course.programContents?.filter((content: any) => content.parentId) || [];

  const getContentIcon = (type: number) => {
    switch (type) {
      case ProgramContentType.Page:
        return <FileText className="w-4 h-4" />;
      case ProgramContentType.Assignment:
      case ProgramContentType.Code:
      case ProgramContentType.Challenge:
        return <Code className="w-4 h-4" />;
      case ProgramContentType.Discussion:
      case ProgramContentType.Questionnaire:
        return <MessageSquare className="w-4 h-4" />;
      case ProgramContentType.Reflection:
      case ProgramContentType.Survey:
        return <FileText className="w-4 h-4" />;
      case ProgramContentType.Lesson:
      default: return <FileText className="w-4 h-4" />;
    }
  };

  const getContentTypeName = (type: number) => {
    switch (type) {
      case ProgramContentType.Page: return 'Page';
      case ProgramContentType.Assignment: return 'Assignment';
      case ProgramContentType.Questionnaire: return 'Questionnaire';
      case ProgramContentType.Discussion: return 'Discussion';
      case ProgramContentType.Code: return 'Code';
      case ProgramContentType.Challenge: return 'Challenge';
      case ProgramContentType.Reflection: return 'Reflection';
      case ProgramContentType.Survey: return 'Survey';
      case ProgramContentType.Lesson: return 'Lesson';
      default: return 'Content';
    }
  };

  return (
    <div className="space-y-8">
      {/* Course Information */}
      <section>
        <h2 className="text-2xl font-bold mb-6 flex items-center">
          <Target className="mr-3 h-6 w-6 text-blue-400" />
          Course Overview
        </h2>

        <div className="bg-gray-800/50 rounded-xl p-6 border border-gray-700">
          {/* Level and Category Badges */}
          <div className="flex flex-wrap gap-2 mb-6">
            <Badge className={`border ${config.bgColor}`}>{config.name}</Badge>
            <Badge variant="outline" className="border-gray-600 text-gray-300">
              {categoryName}
            </Badge>
            {course.estimatedHours && (
              <Badge variant="outline" className="border-gray-600 text-gray-300">
                <Clock className="w-3 h-3 mr-1" />
                {course.estimatedHours} hours
              </Badge>
            )}
          </div>

          <p className="text-lg text-gray-300 leading-relaxed mb-6">{course.description}</p>

          <div className="grid md:grid-cols-2 gap-6">
            <div>
              <h3 className="text-lg font-semibold mb-3 text-blue-400">Published Learning Outcomes</h3>
              {publishedSkills.length > 0 ? (
                <ul className="space-y-2">
                  {publishedSkills.map((skill) => (
                    <li key={skill} className="flex items-start">
                      <CheckCircle className="mr-2 h-5 w-5 text-green-400 mt-0.5 flex-shrink-0" />
                      <span className="text-gray-300">{skill}</span>
                    </li>
                  ))}
                </ul>
              ) : (
                <p className="text-gray-300">
                  No explicit learning outcomes were published for this {categoryName.toLowerCase()} course yet. Use the content outline below to inspect the live curriculum.
                </p>
              )}
            </div>

            <div>
              <h3 className="text-lg font-semibold mb-3 text-blue-400">Prerequisites</h3>
              {requiredSkills.length > 0 ? (
                <ul className="space-y-2 text-gray-300">
                  {requiredSkills.map((skill) => (
                    <li key={skill}>• {skill}</li>
                  ))}
                </ul>
              ) : (
                <p className="text-gray-300">
                  No explicit prerequisites were published for this course. {config.name === 'Beginner' ? 'The catalog metadata suggests it is suitable for beginners.' : `The catalog metadata marks it as ${config.name.toLowerCase()} level.`}
                </p>
              )}
            </div>
          </div>

          {course.programContents && (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 pt-6 border-t border-gray-700">
              <div className="text-center">
                <div className="text-2xl font-bold text-primary">
                  {course.programContents.length}
                </div>
                <div className="text-sm text-muted-foreground">Published Items</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-primary">
                  {course.programContents.filter((c: any) => c.isRequired).length}
                </div>
                <div className="text-sm text-muted-foreground">Required</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-primary">
                  {course.programContents.filter((c: any) => c.estimatedMinutes).reduce((acc: number, c: any) => acc + (c.estimatedMinutes || 0), 0)}
                </div>
                <div className="text-sm text-muted-foreground">Minutes</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-primary">
                  {course.currentEnrollments || 0}
                </div>
                <div className="text-sm text-muted-foreground">Enrolled</div>
              </div>
            </div>
          )}
        </div>
      </section>

      {/* Course Content */}
      {course.programContents && course.programContents.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <BookOpen className="w-5 h-5" />
              Course Content
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid lg:grid-cols-3 gap-6">
              {/* Content Navigation */}
              <div className="lg:col-span-1">
                <Accordion type="single" collapsible className="w-full">
                  {topLevelContent.map((content: any) => {
                    const children = childContent.filter((child: any) => child.parentId === content.id);

                    return (
                      <AccordionItem key={content.id} value={content.id!}>
                        <AccordionTrigger
                          className={`text-left hover:no-underline ${selectedContent?.id === content.id ? 'bg-accent' : ''
                            }`}
                          onClick={() => setSelectedContent(content)}
                        >
                          <div className="flex items-center gap-2 flex-1">
                            {getContentIcon(content.type || 0)}
                            <span className="flex-1">{content.title}</span>
                            {content.isRequired && (
                              <Badge variant="secondary" className="text-xs">
                                Required
                              </Badge>
                            )}
                          </div>
                        </AccordionTrigger>
                        {children.length > 0 && (
                          <AccordionContent>
                            <div className="space-y-2 pl-4">
                              {children.map((child: any) => (
                                <button
                                  key={child.id}
                                  className={`w-full text-left p-2 rounded-md hover:bg-accent transition-colors ${selectedContent?.id === child.id ? 'bg-accent' : ''
                                    }`}
                                  onClick={() => setSelectedContent(child)}
                                >
                                  <div className="flex items-center gap-2">
                                    {getContentIcon(child.type || 0)}
                                    <span className="flex-1 text-sm">{child.title}</span>
                                    {child.estimatedMinutes && (
                                      <span className="text-xs text-muted-foreground">
                                        {child.estimatedMinutes}m
                                      </span>
                                    )}
                                  </div>
                                </button>
                              ))}
                            </div>
                          </AccordionContent>
                        )}
                      </AccordionItem>
                    );
                  })}
                </Accordion>
              </div>

              {/* Content Display */}
              <div className="lg:col-span-2 min-w-0">
                {selectedContent ? (
                  <div className="space-y-4 min-w-0">
                    <div className="flex items-center justify-between">
                      <div>
                        <h3 className="text-xl font-semibold">{selectedContent.title}</h3>
                        <p className="text-sm text-muted-foreground">
                          {getContentTypeName(selectedContent.type || 0)}
                          {selectedContent.estimatedMinutes && ` • ${selectedContent.estimatedMinutes} minutes`}
                        </p>
                      </div>
                      <div className="flex items-center gap-2">
                        {selectedContent.isRequired && (
                          <Badge variant="secondary">Required</Badge>
                        )}
                        <Badge variant="outline">
                          {getContentTypeName(selectedContent.type || 0)}
                        </Badge>
                      </div>
                    </div>

                    {selectedContent.description && (
                      <p className="text-muted-foreground">{selectedContent.description}</p>
                    )}

                    {selectedContent.body && (
                      <div className="prose prose-stone dark:prose-invert max-w-none min-w-0 overflow-x-auto break-words">
                        <MarkdownRenderer content={selectedContent.body} />
                      </div>
                    )}

                    {selectedContent.children && selectedContent.children.length > 0 && (
                      <div className="space-y-2">
                        <h4 className="font-semibold">Sub-modules:</h4>
                        <div className="grid gap-2">
                          {selectedContent.children.map((child: any) => (
                            <button
                              key={child.id}
                              className="w-full text-left p-3 rounded-lg border hover:bg-accent transition-colors"
                              onClick={() => setSelectedContent(child)}
                            >
                              <div className="flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                  {getContentIcon(child.type || 0)}
                                  <span>{child.title}</span>
                                </div>
                                <div className="flex items-center gap-2">
                                  {child.estimatedMinutes && (
                                    <span className="text-sm text-muted-foreground">
                                      {child.estimatedMinutes}m
                                    </span>
                                  )}
                                  {child.isRequired && (
                                    <Badge variant="secondary" className="text-xs">
                                      Required
                                    </Badge>
                                  )}
                                </div>
                              </div>
                            </button>
                          ))}
                        </div>
                      </div>
                    )}
                  </div>
                ) : (
                  <div className="text-center py-12 text-muted-foreground">
                    <BookOpen className="w-12 h-12 mx-auto mb-4 opacity-50" />
                    <p>Select a module to view its content</p>
                  </div>
                )}
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
