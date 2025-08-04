'use client';

import { useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Clock, Users, Star, BookOpen, Play } from 'lucide-react';
import { CourseLocationSelector } from '@/components/courses/course-location-selector';
import { LocationBasedContent } from '@/components/courses/location-based-content';
import type { TestingLocation } from '@/lib/api/testing-types';
import type { Program } from '@/lib/api/generated/types.gen';

interface CourseDetailClientProps {
  course: Program;
  levelConfig: {
    name: string;
    color: string;
    bgColor: string;
    borderColor: string;
  };
}

export function CourseDetailClient({ course, levelConfig }: CourseDetailClientProps) {
  const [selectedLocation, setSelectedLocation] = useState<TestingLocation | null>(null);
  const [maxTests, setMaxTests] = useState(0);
  const [maxProjects, setMaxProjects] = useState(0);

  const handleLocationSelected = (location: TestingLocation, maxTestsCapacity: number, maxProjectsCapacity: number) => {
    setSelectedLocation(location);
    setMaxTests(maxTestsCapacity);
    setMaxProjects(maxProjectsCapacity);
  };

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      {/* Header */}
      <div className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border-b border-gray-800">
        <div className="container mx-auto px-4 py-12">
          <div className="max-w-4xl">
            <div className="flex items-center gap-2 mb-4">
              <Badge className={`${levelConfig.bgColor} ${levelConfig.color} ${levelConfig.borderColor}`}>
                Level {course.difficulty || 1} • {levelConfig.name}
              </Badge>
              <Badge variant="outline" className="capitalize">
                {course.category || 'General'}
              </Badge>
            </div>

            <h1 className="text-4xl font-bold mb-4">{course.title}</h1>

            {course.description && <p className="text-xl text-gray-300 mb-6">{course.description}</p>}

            <div className="flex items-center gap-6 text-sm text-gray-400">
              <div className="flex items-center gap-2">
                <BookOpen className="h-4 w-4" />
                <span>{course.programContents?.length || 0} lessons</span>
              </div>
              <div className="flex items-center gap-2">
                <Clock className="h-4 w-4" />
                <span>{course.estimatedHours || 0}h</span>
              </div>
              <div className="flex items-center gap-2">
                <Users className="h-4 w-4" />
                <span>{course.currentEnrollments || 0} students</span>
              </div>
              {course.averageRating && (
                <div className="flex items-center gap-2">
                  <Star className="h-4 w-4 text-yellow-400" />
                  <span>{course.averageRating.toFixed(1)}</span>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      <div className="container mx-auto px-4 py-8">
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-8">
            <Card className="bg-gray-900 border-gray-800">
              <CardHeader>
                <CardTitle>About This Program</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-gray-300">{course.description || 'No detailed description available.'}</p>
              </CardContent>
            </Card>

            {course.programContents && course.programContents.length > 0 && (
              <Card className="bg-gray-900 border-gray-800">
                <CardHeader>
                  <CardTitle>Course Content</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2">
                    {course.programContents.slice(0, 5).map((content, index) => (
                      <div key={content.id || index} className="flex items-center gap-3 p-3 rounded-lg bg-gray-800/50">
                        <Play className="h-4 w-4 text-blue-400" />
                        <span className="flex-1">{content.title || `Lesson ${index + 1}`}</span>
                        <span className="text-sm text-gray-400">10min</span>
                      </div>
                    ))}
                    {course.programContents.length > 5 && <p className="text-sm text-gray-400 text-center mt-4">+{course.programContents.length - 5} more lessons</p>}
                  </div>
                </CardContent>
              </Card>
            )}

            {/* Location-Based Testing Content */}
            <LocationBasedContent selectedLocation={selectedLocation} maxTests={maxTests} maxProjects={maxProjects} />
          </div>

          {/* Sidebar */}
          <div className="lg:col-span-1">
            <div className="sticky top-8 space-y-6">
              {/* Program Access Card */}
              <Card className="bg-gray-900 border-gray-800">
                <CardContent className="p-6">
                  <div className="space-y-4">
                    <div>
                      <h3 className="font-semibold mb-2">Program Access</h3>
                      <p className="text-sm text-gray-400 mb-4">{course.isEnrollmentOpen ? 'Enrollment is currently open' : 'Enrollment is closed'}</p>
                    </div>

                    {course.isEnrollmentOpen && (
                      <Button className="w-full" size="lg">
                        <Play className="h-4 w-4 mr-2" />
                        Enroll Now
                      </Button>
                    )}

                    <div className="pt-4 border-t border-gray-800">
                      <h4 className="font-medium mb-2">Program Details</h4>
                      <div className="space-y-2 text-sm text-gray-400">
                        <div className="flex justify-between">
                          <span>Difficulty:</span>
                          <span className={levelConfig.color}>{levelConfig.name}</span>
                        </div>
                        <div className="flex justify-between">
                          <span>Duration:</span>
                          <span>{course.estimatedHours || 0} hours</span>
                        </div>
                        <div className="flex justify-between">
                          <span>Students:</span>
                          <span>{course.currentEnrollments || 0}</span>
                        </div>
                        {course.averageRating && (
                          <div className="flex justify-between">
                            <span>Rating:</span>
                            <span className="flex items-center gap-1">
                              <Star className="h-3 w-3 text-yellow-400" />
                              {course.averageRating.toFixed(1)}
                            </span>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Location Selector */}
              <CourseLocationSelector onLocationSelected={handleLocationSelected} />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
