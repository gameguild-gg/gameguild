'use client';

import React, { useMemo, useState } from 'react';
import { CourseFilterControls } from '@/components/courses/common';
import { CourseCard } from './course-card';
import { Button } from '@/components/ui/button';
import { Plus } from 'lucide-react';
import { Program, ContentStatus, ProgramCategory, ProgramDifficulty } from '@/lib/api/generated/types.gen';

// Type aliases to maintain existing naming
type Course = Program;
type CourseStatus = ContentStatus;
type CourseArea = ProgramCategory;
type CourseLevel = ProgramDifficulty;

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
  const [selectedPeriod, setSelectedPeriod] = useState('all');
  const [viewMode, setViewMode] = useState<'cards' | 'row' | 'table'>(initialViewMode);

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
        if (!course.title.toLowerCase().includes(searchLower) && !(course.description && course.description.toLowerCase().includes(searchLower))) {
          return false;
        }
      }

      // Status filter
      if (selectedStatuses.length > 0 && !selectedStatuses.includes(course.status || 0)) {
        return false;
      }

      // Area filter (category)
      if (selectedAreas.length > 0 && course.category && !selectedAreas.includes(course.category)) {
        return false;
      }

      // Level filter (difficulty)
      if (selectedLevels.length > 0 && course.difficulty && !selectedLevels.includes(course.difficulty)) {
        return false;
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
        // For now, use card view for table mode too
        // TODO: Implement actual table view
        return (
          <div className="space-y-3">
            {filteredCourses.map((course) => (
              <div key={course.id} className="w-full">
                <CourseCard course={course} onEdit={onEdit} onView={onView} onEnroll={onEnroll} />
              </div>
            ))}
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
                setSelectedPeriod('all');
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
