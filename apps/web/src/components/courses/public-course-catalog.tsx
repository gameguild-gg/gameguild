'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import type { Program } from '@/lib/api/generated';
import { getCourseCategoryName, getCourseLevelConfig } from '@/lib/courses/services/course.service';
import { CourseCatalog } from '@game-guild/courses';
import { ArrowRight, BookOpen, Clock, Star, Users } from 'lucide-react';
import Image from 'next/image';
import { useSearchParams } from 'next/navigation';
import React from 'react';

interface PublicCourseCatalogProps {
    initialCourses: Program[];
}

function normalizeFilterValue(value: string): string {
    return value.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-');
}

function matchesCategory(categoryName: string, filterValue: string): boolean {
    const normalizedCategory = normalizeFilterValue(categoryName);

    if (normalizedCategory === filterValue) {
        return true;
    }

    const aliases: Record<string, string[]> = {
        art: ['art-design', 'design', 'creative-arts'],
        design: ['design', 'game-development', 'creative-arts'],
        programming: ['programming', 'web-development', 'mobile-development', 'ai'],
    };

    return (aliases[filterValue] ?? []).includes(normalizedCategory);
}
function getCatalogState(course: Program): { label: string; className: string; caption: string } {
    const visibility = typeof course.visibility === 'string' ? course.visibility.trim().toLowerCase() : '';
    const isEnrollmentOpen = typeof course.isEnrollmentOpen === 'boolean' ? course.isEnrollmentOpen : false;

    if (visibility !== 'public') {
        return {
            label: 'Preview',
            className: 'border-blue-500 bg-blue-500/10 text-blue-300',
            caption: 'Preview available',
        };
    }

    if (isEnrollmentOpen) {
        return {
            label: 'Enrollment Open',
            className: 'border-emerald-500 bg-emerald-500/10 text-emerald-300',
            caption: 'Open for enrollment',
        };
    }

    return {
        label: 'Enrollment Closed',
        className: 'border-amber-500 bg-amber-500/10 text-amber-300',
        caption: 'Enrollment closed',
    };
}

function PublicCourseGrid({ courses }: { courses: Program[] }) {
    if (!courses.length) {
        return <p className="rounded-xl border border-slate-700 bg-slate-900/70 p-6 text-slate-300">No published courses matched this filter yet.</p>;
    }

    return (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2 xl:grid-cols-3">
            {courses.map((course, index) => {
                const courseTitle = typeof course.title === 'string' && course.title.length > 0 ? course.title : 'Untitled course';
                const courseSlug = typeof course.slug === 'string' && course.slug.length > 0 ? course.slug : null;
                const courseDescription = typeof course.description === 'string' ? course.description : '';
                const courseImage = typeof course.thumbnail === 'string' && course.thumbnail.length > 0 ? course.thumbnail : null;
                const categoryName = getCourseCategoryName(course.category as string | number | null | undefined);
                const levelConfig = getCourseLevelConfig(course.difficulty as string | number | null | undefined);
                const currentEnrollments = typeof course.currentEnrollments === 'number' ? course.currentEnrollments : 0;
                const averageRating = typeof course.averageRating === 'number' ? course.averageRating : 0;
                const estimatedHours = typeof course.estimatedHours === 'number' ? course.estimatedHours : null;
                const catalogState = getCatalogState(course);

                return (
                    <article key={course.id ?? courseSlug ?? index} className="overflow-hidden rounded-2xl border border-slate-700 bg-slate-900/80 shadow-lg shadow-slate-950/40 transition-transform hover:-translate-y-1 hover:border-slate-500">
                        <div className="relative aspect-video overflow-hidden bg-slate-800">
                            {courseImage ? (
                                <Image src={courseImage} alt={courseTitle} fill className="object-cover" />
                            ) : (
                                <div className="flex h-full w-full items-center justify-center bg-[radial-gradient(circle_at_18%_18%,rgba(56,189,248,0.35),transparent_32%),linear-gradient(135deg,#0f172a,#1e1b4b_55%,#020617)] px-6 text-center">
                                    <span className="text-sm font-semibold uppercase tracking-wide text-white/75">{courseTitle}</span>
                                </div>
                            )}
                            <div className="absolute inset-0 bg-linear-to-t from-slate-950 via-slate-950/30 to-transparent" />
                            <div className="absolute left-4 top-4 flex flex-wrap gap-2">
                                <Badge className={`${levelConfig.bgColor} ${levelConfig.color} border`}>{levelConfig.name}</Badge>
                                <Badge variant="outline" className="border-slate-400/40 bg-slate-950/50 text-slate-100">{categoryName}</Badge>
                                <Badge variant="outline" className={catalogState.className}>{catalogState.label}</Badge>
                            </div>
                        </div>

                        <div className="flex flex-col gap-4 p-5 text-white">
                            <div>
                                <h3 className="text-xl font-semibold">{courseTitle}</h3>
                                <p className="mt-2 line-clamp-3 text-sm text-slate-300">{courseDescription || 'Published course details are now loading from the live catalog.'}</p>
                            </div>

                            <div className="flex flex-wrap gap-4 text-sm text-slate-300">
                                <span className="flex items-center gap-1.5">
                                    <Users className="h-4 w-4 text-blue-400" />
                                    {currentEnrollments} enrolled
                                </span>
                                <span className="flex items-center gap-1.5">
                                    <Star className="h-4 w-4 text-amber-400" />
                                    {averageRating.toFixed(1)}
                                </span>
                                {estimatedHours !== null && (
                                    <span className="flex items-center gap-1.5">
                                        <Clock className="h-4 w-4 text-emerald-400" />
                                        {estimatedHours}h
                                    </span>
                                )}
                            </div>

                            <div className="mt-auto flex items-center justify-between gap-3">
                                <div className="flex items-center gap-2 text-sm text-slate-400">
                                    <BookOpen className="h-4 w-4" />
                                    {catalogState.caption}
                                </div>
                                {courseSlug ? (
                                    <Button asChild className="bg-blue-600 text-white hover:bg-blue-500">
                                        <Link href={`/courses/${courseSlug}`}>
                                            View course
                                            <ArrowRight className="ml-2 h-4 w-4" />
                                        </Link>
                                    </Button>
                                ) : null}
                            </div>
                        </div>
                    </article>
                );
            })}
        </div>
    );
}

export function PublicCourseCatalog({ initialCourses }: PublicCourseCatalogProps) {
    const searchParams = useSearchParams();
    const categoryFilter = searchParams?.get('category');

    const visibleCourses = React.useMemo(() => {
        if (!categoryFilter) {
            return initialCourses;
        }

        const normalizedFilter = normalizeFilterValue(categoryFilter);
        return initialCourses.filter((course) => matchesCategory(getCourseCategoryName(course.category as string | number | null | undefined), normalizedFilter));
    }, [categoryFilter, initialCourses]);

    return (
        <CourseCatalog<Program>
            initialCourses={visibleCourses}
            Grid={PublicCourseGrid}
            title="Course Catalog"
            className="container mx-auto px-4 py-8"
        />
    );
}
