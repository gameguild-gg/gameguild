import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { getProgramBySlugService, getProgramLevelConfig } from '@/lib/content-management/programs/programs.service';
import { Archive, BookOpen, Clock, Eye, FileText, Star, Users } from 'lucide-react';
import { notFound } from 'next/navigation';
import React from 'react';

interface PageProps {
  params: Promise<{ slug: string }>;
}

export default async function Page({ params }: PageProps): Promise<React.JSX.Element> {
  const { slug } = await params;

  const result = await getProgramBySlugService(slug);

  if (!result.success || !result.data) {
    console.error('Error fetching program:', result.error);
    notFound();
  }

  const program = result.data;
  const levelConfig = getProgramLevelConfig(program.difficulty || 1);

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

  const getStatusName = (status: string | number | undefined) => {
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
        return 'Unknown';
    }
  };

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>{program.title}</DashboardPageTitle>
        <DashboardPageDescription>Program overview and details</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        <div className="space-y-6">
          {/* Status and Basic Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center justify-between">
                <span>Program Status</span>
                <Badge className={`${getStatusColor(program.status)} flex items-center gap-1`}>
                  {getStatusIcon(program.status)}
                  {getStatusName(program.status)}
                </Badge>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                <div className="flex items-center gap-2">
                  <BookOpen className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm">{program.programContents?.length || 0} lessons</span>
                </div>
                <div className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm">{program.estimatedHours || 0}h estimated</span>
                </div>
                <div className="flex items-center gap-2">
                  <Users className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm">{program.currentEnrollments || 0} enrolled</span>
                </div>
                {program.averageRating && (
                  <div className="flex items-center gap-2">
                    <Star className="h-4 w-4 text-yellow-400" />
                    <span className="text-sm">
                      {program.averageRating.toFixed(1)} ({program.totalRatings || 0} reviews)
                    </span>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Program Details */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle>Program Information</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Description</label>
                  <p className="text-sm mt-1">{program.description || 'No description provided'}</p>
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Category</label>
                  <p className="text-sm mt-1 capitalize">{program.category || 'Not specified'}</p>
                </div>
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Difficulty Level</label>
                  <div className="flex items-center gap-2 mt-1">
                    <Badge className={`${(await levelConfig).bgColor} ${(await levelConfig).color} ${(await levelConfig).borderColor}`}>
                      Level {program.difficulty || 1} • {(await levelConfig).name}
                    </Badge>
                  </div>
                  <p className="text-xs text-muted-foreground mt-1">{(await levelConfig).description}</p>
                </div>
                {program.slug && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Slug</label>
                    <p className="text-sm mt-1 font-mono bg-muted px-2 py-1 rounded">{program.slug}</p>
                  </div>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Enrollment & Access</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Enrollment Status</label>
                  <p className="text-sm mt-1 capitalize">{program.isEnrollmentOpen ? 'Open' : 'Closed'}</p>
                </div>
                {program.maxEnrollments && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Max Enrollments</label>
                    <p className="text-sm mt-1">{program.maxEnrollments}</p>
                  </div>
                )}
                {program.enrollmentDeadline && (
                  <div>
                    <label className="text-sm font-medium text-muted-foreground">Enrollment Deadline</label>
                    <p className="text-sm mt-1">{new Date(program.enrollmentDeadline).toLocaleDateString()}</p>
                  </div>
                )}
                <div>
                  <label className="text-sm font-medium text-muted-foreground">Visibility</label>
                  <p className="text-sm mt-1 capitalize">{program.visibility || 'Private'}</p>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Timestamps */}
          <Card>
            <CardHeader>
              <CardTitle>Timeline</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
                <div>
                  <label className="font-medium text-muted-foreground">Created</label>
                  <p className="mt-1">{program.createdAt ? new Date(program.createdAt).toLocaleDateString() : 'Unknown'}</p>
                </div>
                <div>
                  <label className="font-medium text-muted-foreground">Last Updated</label>
                  <p className="mt-1">{program.updatedAt ? new Date(program.updatedAt).toLocaleDateString() : 'Unknown'}</p>
                </div>
                {program.deletedAt && (
                  <div>
                    <label className="font-medium text-muted-foreground">Deleted</label>
                    <p className="mt-1">{new Date(program.deletedAt).toLocaleDateString()}</p>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>
      </DashboardPageContent>
    </DashboardPage>
  );
}
