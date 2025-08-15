'use client';

import React from 'react';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { BookOpen, Clock, Eye, FileText, Star, Users, Edit, Settings, Archive } from 'lucide-react';
import type { Program } from '@/lib/api/generated/types.gen';
import Link from 'next/link';

interface CourseDetailsProps {
  course: Program;
}

export function CourseDetails({ course }: CourseDetailsProps) {
  const getStatusColor = (status: string | number | undefined) => {
    const statusStr = typeof status === 'number' ? status.toString() : status;
    switch (statusStr) {
      case 'published':
      case '2':
        return 'bg-green-500/20 text-green-400 border-green-500/30';
      case 'draft':
      case '0':
        return 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30';
      case 'archived':
      case '3':
        return 'bg-red-500/20 text-red-400 border-red-500/30';
      default:
        return 'bg-slate-500/20 text-slate-400 border-slate-500/30';
    }
  };

  const getStatusIcon = (status: string | number | undefined) => {
    const statusStr = typeof status === 'number' ? status.toString() : status;
    switch (statusStr) {
      case 'published':
      case '2':
        return <Eye className="h-3 w-3" />;
      case 'draft':
      case '0':
        return <FileText className="h-3 w-3" />;
      case 'archived':
      case '3':
        return <Archive className="h-3 w-3" />;
      default:
        return <FileText className="h-3 w-3" />;
    }
  };

  const getStatusLabel = (status: string | number | undefined) => {
    const statusStr = typeof status === 'number' ? status.toString() : status;
    switch (statusStr) {
      case 'published':
      case '2':
        return 'Published';
      case 'draft':
      case '0':
        return 'Draft';
      case 'archived':
      case '3':
        return 'Archived';
      default:
        return 'Draft';
    }
  };

  return (
    <div className="space-y-6">
      {/* Course Header */}
      <div className="flex items-start justify-between">
        <div className="flex-1 space-y-4">
          <div className="flex items-center gap-3">
            <Badge 
              variant="outline" 
              className={`flex items-center gap-1 ${getStatusColor(course.status)}`}
            >
              {getStatusIcon(course.status)}
              {getStatusLabel(course.status)}
            </Badge>
            {course.difficulty && (
              <Badge variant="secondary">
                Level {course.difficulty}
              </Badge>
            )}
          </div>
          
          <div className="prose prose-sm dark:prose-invert max-w-none">
            <p className="text-muted-foreground">{course.description}</p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Link href={`/dashboard/courses/${course.slug || course.id}/edit`}>
            <Button variant="outline" size="sm">
              <Edit className="h-4 w-4 mr-2" />
              Edit
            </Button>
          </Link>
          <Link href={`/dashboard/courses/${course.slug || course.id}/settings`}>
            <Button variant="outline" size="sm">
              <Settings className="h-4 w-4 mr-2" />
              Settings
            </Button>
          </Link>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Content Items</CardTitle>
            <BookOpen className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{(course as any).contentItems?.length || 0}</div>
            <p className="text-xs text-muted-foreground">Total modules</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Students</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{(course as any).enrollmentCount || 0}</div>
            <p className="text-xs text-muted-foreground">Enrolled students</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Rating</CardTitle>
            <Star className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{(course as any).averageRating || '0.0'}</div>
            <p className="text-xs text-muted-foreground">Average rating</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Duration</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{(course as any).estimatedDuration || 'N/A'}</div>
            <p className="text-xs text-muted-foreground">Total hours</p>
          </CardContent>
        </Card>
      </div>

      {/* Course Navigation */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        <Link href={`/dashboard/courses/${course.slug || course.id}/content`}>
          <Card className="hover:shadow-md transition-shadow cursor-pointer">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <BookOpen className="h-5 w-5" />
                Content Management
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">
                Manage course content, modules, and lessons
              </p>
            </CardContent>
          </Card>
        </Link>

        <Link href={`/dashboard/courses/${course.slug || course.id}/delivery`}>
          <Card className="hover:shadow-md transition-shadow cursor-pointer">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Users className="h-5 w-5" />
                Delivery & Enrollment
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">
                Manage student enrollment and course delivery
              </p>
            </CardContent>
          </Card>
        </Link>

        <Link href={`/dashboard/courses/${course.slug || course.id}/pricing`}>
          <Card className="hover:shadow-md transition-shadow cursor-pointer">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Star className="h-5 w-5" />
                Pricing & Monetization
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm text-muted-foreground">
                Configure pricing and monetization options
              </p>
            </CardContent>
          </Card>
        </Link>
      </div>
    </div>
  );
}
