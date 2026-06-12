'use client';

import { PeriodType } from '@/components/common/filters/filter-context';
import { CourseFilterControls } from '@/components/courses/common';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { ModulesContentsContentStatus, ModulesProgramsProgramDifficulty } from '@/lib/api/generated/stub-types';
import { ProgramCategory } from '@/lib/api/generated/types.gen';
import { Eye, FileText, Plus, Play } from 'lucide-react';
import React, { useMemo, useState } from 'react';
import { CourseCard, CourseCardCourse } from './course-card';

// Type aliases to maintain existing naming
type Course = CourseCardCourse;
type CourseStatus = ModulesContentsContentStatus;
type CourseArea = ProgramCategory;
type CourseLevel = ModulesProgramsProgramDifficulty;

interface CourseListProps {
  courses: Course[];
  onEdit?: (course: Course) => void;
  onView?: (course: Course) => void;
  onEnroll?: (course: Course) => void;
  onCreate?: () => void;
  initialViewMode?: 'cards' | 'row' | 'table';
  hideViewToggle?: boolean;
}

export const CourseList = ({ courses, onEdit, onView, onEnroll, onCreate, initialViewMode = 'cards', hideViewToggle = false }: CourseListProps): React.JSX.Element => {
  // Filter states
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedStatuses, setSelectedStatuses] = useState<CourseStatus[]>([]);
  const [selectedAreas, setSelectedAreas] = useState<CourseArea[]>([]);
  const [selectedLevels, setSelectedLevels] = useState<CourseLevel[]>([]);
  const [selectedPeriod, setSelectedPeriod] = useState<PeriodType>('week');
  const [viewMode, setViewMode] = useState<'cards' | 'row' | 'table'>(initialViewMode);

  const normalizeStatus = (status: Course['status']): CourseStatus | undefined => {
    if (status === undefined || status === null) return undefined;
    const value = typeof status === 'number' ? status : String(status).toLowerCase();
    if (value === 0 || value === '0' || value === 'draft') return ModulesContentsContentStatus.DRAFT;
    if (value === 1 || value === '1' || value === 'under-review' || value === 'review') return ModulesContentsContentStatus.UNDER_REVIEW;
    if (value === 2 || value === '2' || value === 'published') return ModulesContentsContentStatus.PUBLISHED;
    if (value === 3 || value === '3' || value === 'archived') return ModulesContentsContentStatus.ARCHIVED;
    return undefined;
  };

  const normalizeLevel = (level: Course['difficulty'] | Course['level']): CourseLevel | undefined => {
    if (level === undefined || level === null) return undefined;
    const value = typeof level === 'number' ? level : String(level).toLowerCase();
    if (value === 0 || value === '0' || value === 'beginner') return ModulesProgramsProgramDifficulty.BEGINNER;
    if (value === 1 || value === '1' || value === 'intermediate') return ModulesProgramsProgramDifficulty.INTERMEDIATE;
    if (value === 2 || value === '2' || value === 'advanced') return ModulesProgramsProgramDifficulty.ADVANCED;
    if (value === 3 || value === '3' || value === 'expert') return ModulesProgramsProgramDifficulty.EXPERT;
    return undefined;
  };

  const getStatusLabel = (course: Course): string => {
    const status = normalizeStatus(course.status);
    if (status === ModulesContentsContentStatus.DRAFT) return 'Draft';
    if (status === ModulesContentsContentStatus.UNDER_REVIEW) return 'Under review';
    if (status === ModulesContentsContentStatus.PUBLISHED) return 'Published';
    if (status === ModulesContentsContentStatus.ARCHIVED) return 'Archived';
    return 'Unknown';
  };

  const getLevelLabel = (course: Course): string => {
    const level = normalizeLevel(course.difficulty ?? course.level);
    if (level === ModulesProgramsProgramDifficulty.BEGINNER) return 'Beginner';
    if (level === ModulesProgramsProgramDifficulty.INTERMEDIATE) return 'Intermediate';
    if (level === ModulesProgramsProgramDifficulty.ADVANCED) return 'Advanced';
    if (level === ModulesProgramsProgramDifficulty.EXPERT) return 'Expert';
    return 'Unknown';
  };

  // Filter handlers
  const handleToggleStatus = (status: CourseStatus) => {
    setSelectedStatuses((prev) => (prev.includes(status) ? prev.filter((s) => s !== status) : [...prev, status]));
  };

  const handleToggleArea = (area: CourseArea) => {
    setSelectedAreas((prev) => (prev.includes(area) ? prev.filter((a) => a !== area) : [...prev, area]));
  };

  const handleToggleLevel = (level: CourseLevel) => {
    setSelectedLevels((prev) => (prev.includes(level) ? prev.filter((l) => l !== level) : [...prev, level]));
  };

  // Filtered courses
  const filteredCourses = useMemo(() => {
    return courses.filter((course) => {
      // Search filter
      if (searchTerm) {
        const searchLower = searchTerm.toLowerCase();
        const title = (course.title || '').toLowerCase();
        const description = (course.description || '').toLowerCase();
        if (!title.includes(searchLower) && !description.includes(searchLower)) {
          return false;
        }
      }

      // Status filter
      const normalizedStatus = normalizeStatus(course.status);
      if (selectedStatuses.length > 0 && normalizedStatus) {
        if (!selectedStatuses.includes(normalizedStatus)) {
          return false;
        }
      }

      // Area filter (category)
      if (selectedAreas.length > 0 && course.category) {
        if (!selectedAreas.includes(course.category as CourseArea)) {
          return false;
        }
      }

      // Level filter (difficulty)
      const normalizedLevel = normalizeLevel(course.difficulty ?? course.level);
      if (selectedLevels.length > 0 && normalizedLevel) {
        if (!selectedLevels.includes(normalizedLevel)) {
          return false;
        }
      }

      return true;
    });
  }, [courses, searchTerm, selectedStatuses, selectedAreas, selectedLevels]);

  const renderCourseGrid = () => {
    if (filteredCourses.length === 0) {
      return (
        <div className="col-span-full flex flex-col items-center justify-center py-12 text-center">
          <div className="text-slate-400 text-lg mb-2">No courses found</div>
          <div className="text-slate-500 text-sm mb-4">{searchTerm || selectedStatuses.length > 0 || selectedAreas.length > 0 || selectedLevels.length > 0 ? 'Try adjusting your filters' : 'No courses available at the moment'}</div>
          {onCreate && courses.length === 0 && (
            <Button onClick={onCreate} className="flex items-center gap-2">
              <Plus className="h-4 w-4" />
              Create Your First Course
            </Button>
          )}
        </div>
      );
    }

    switch (viewMode) {
      case 'cards':
        return (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
            {filteredCourses.map((course) => (
              <CourseCard key={course.id} course={course} onEdit={onEdit} onView={onView} onEnroll={onEnroll} />
            ))}
          </div>
        );

      case 'row':
        return (
          <div className="space-y-4">
            {filteredCourses.map((course) => (
              <div key={course.id} className="w-full">
                <CourseCard course={course} onEdit={onEdit} onView={onView} onEnroll={onEnroll} />
              </div>
            ))}
          </div>
        );

      case 'table':
        return (
          <div className="overflow-hidden rounded-xl border border-slate-700/60 bg-slate-950/40">
            <Table aria-label="Courses">
              <TableHeader>
                <TableRow className="border-slate-800">
                  <TableHead>Course</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Area</TableHead>
                  <TableHead>Level</TableHead>
                  <TableHead className="text-right">Hours</TableHead>
                  <TableHead className="text-right">Enrollments</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredCourses.map((course) => {
                  const title = course.title || 'Untitled course';
                  const status = getStatusLabel(course);
                  const area = ((course.area as string | undefined) ?? (course.category as string | undefined) ?? 'General').toString();
                  const level = getLevelLabel(course);
                  const hours = course.estimatedHours ?? 0;
                  const enrollments = course.analytics?.enrollments ?? (course.currentEnrollments as number | undefined) ?? 0;
                  const isPublished = normalizeStatus(course.status) === ModulesContentsContentStatus.PUBLISHED;

                  return (
                    <TableRow key={course.id ?? title} className="border-slate-800">
                      <TableCell>
                        <div className="min-w-0">
                          <div className="font-medium text-slate-100">{title}</div>
                          {course.description && <div className="max-w-md truncate text-xs text-slate-400">{course.description}</div>}
                        </div>
                      </TableCell>
                      <TableCell>
                        <span className="rounded-full border border-slate-700 bg-slate-900 px-2 py-1 text-xs text-slate-200">{status}</span>
                      </TableCell>
                      <TableCell className="capitalize text-slate-300">{area}</TableCell>
                      <TableCell className="text-slate-300">{level}</TableCell>
                      <TableCell className="text-right text-slate-300">{hours}h</TableCell>
                      <TableCell className="text-right text-slate-300">{enrollments}</TableCell>
                      <TableCell>
                        <div className="flex justify-end gap-2">
                          {onView && (
                            <Button size="sm" variant="outline" onClick={() => onView(course)}>
                              <Eye className="mr-1 h-4 w-4" />
                              View
                            </Button>
                          )}
                          {onEdit && (
                            <Button size="sm" variant="outline" onClick={() => onEdit(course)}>
                              <FileText className="mr-1 h-4 w-4" />
                              Edit
                            </Button>
                          )}
                          {onEnroll && isPublished && (
                            <Button size="sm" onClick={() => onEnroll(course)}>
                              <Play className="mr-1 h-4 w-4" />
                              Enroll
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className="space-y-6">
      {/* Filter Controls */}
      <CourseFilterControls
        searchTerm={searchTerm}
        onSearchChange={setSearchTerm}
        selectedStatuses={selectedStatuses}
        onToggleStatus={handleToggleStatus}
        selectedAreas={selectedAreas}
        onToggleArea={handleToggleArea}
        selectedLevels={selectedLevels}
        onToggleLevel={handleToggleLevel}
        selectedPeriod={selectedPeriod}
        onPeriodChange={setSelectedPeriod}
        viewMode={viewMode}
        onViewModeChange={setViewMode}
        hideViewToggle={hideViewToggle}
      />

      {/* Results Summary */}
      <div className="flex items-center justify-between text-sm text-slate-400">
        <span>
          Showing {filteredCourses.length} of {courses.length} courses
        </span>
        <div className="flex items-center gap-3">
          {(searchTerm || selectedStatuses.length > 0 || selectedAreas.length > 0 || selectedLevels.length > 0) && (
            <button
              onClick={() => {
                setSearchTerm('');
                setSelectedStatuses([]);
                setSelectedAreas([]);
                setSelectedLevels([]);
                setSelectedPeriod('month');
              }}
              className="text-blue-400 hover:text-blue-300 transition-colors"
            >
              Clear all filters
            </button>
          )}
          {onCreate && (
            <Button onClick={onCreate} size="sm" className="flex items-center gap-2">
              <Plus className="h-4 w-4" />
              Create Course
            </Button>
          )}
        </div>
      </div>

      {/* Course Grid */}
      {renderCourseGrid()}
    </div>
  );
};
