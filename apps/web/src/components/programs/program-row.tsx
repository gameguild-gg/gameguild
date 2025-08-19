'use client';

import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { Program } from '@/lib/api/generated/types.gen';
import { Clock, Eye, DollarSign, Calendar, BookOpen } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { formatDistanceToNow, parseISO } from 'date-fns';

interface ProgramRowProps {
  program: Program;
}

export function ProgramRow({ program }: ProgramRowProps) {
  const formatPrice = (price?: number, currency?: string) => {
    if (!price) return 'Free';
    return `${currency || '$'}${price}`;
  };

  const formatDuration = (duration?: number) => {
    if (!duration) return null;
    const hours = Math.floor(duration / 60);
    const minutes = duration % 60;

    if (hours > 0) {
      return `${hours}h${minutes > 0 ? ` ${minutes}m` : ''}`;
    }
    return `${minutes}m`;
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Published':
        return 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300';
      case 'Draft':
        return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300';
      case 'Archived':
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-300';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-300';
    }
  };

  const getVisibilityColor = (visibility: string) => {
    switch (visibility) {
      case 'Public':
        return 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300';
      case 'Private':
        return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300';
      case 'Restricted':
        return 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-300';
      default:
        return 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-300';
    }
  };

  return (
    <Link href={`/courses/${program.slug || program.id}/content`}>
      <Card className="group overflow-hidden border-slate-700/50 bg-slate-800/30 backdrop-blur-sm shadow-lg hover:shadow-xl transition-all duration-300 hover:border-slate-600/50 cursor-pointer">
        <CardContent className="p-3">
          <div className="flex items-start gap-3">
            {/* Thumbnail */}
            <div className="relative h-14 w-20 flex-shrink-0 overflow-hidden rounded-lg bg-slate-700/50">
              {program.thumbnail ? (
                <Image src={program.thumbnail || ''} alt={program.title} fill className="object-cover transition-transform duration-300 group-hover:scale-105" />
              ) : (
                <div className="flex h-full w-full items-center justify-center">
                  <BookOpen className="h-5 w-5 text-slate-400" />
                </div>
              )}
            </div>

            {/* Content */}
            <div className="flex-1 space-y-1.5">
              <div>
                <h3 className="text-base font-semibold text-slate-200 group-hover:text-white transition-colors line-clamp-1">{program.title}</h3>
                {program.description && <p className="text-sm text-slate-400 line-clamp-2 mt-0.5">{program.description}</p>}
              </div>

              {/* Badges */}
              <div className="flex gap-1.5">
                <Badge className={getStatusColor(program.status)}>{program.status}</Badge>
                <Badge className={getVisibilityColor(program.visibility)}>{program.visibility}</Badge>
              </div>

              {/* Metadata */}
              <div className="flex flex-wrap gap-3 text-xs text-slate-400">
                {(program.difficulty || 'Unknown') && (
                  <div className="flex items-center gap-1">
                    <Eye className="h-3 w-3" />
                    {program.difficulty || 'Unknown'}
                  </div>
                )}

                {formatDuration(program.estimatedHours ? program.estimatedHours * 60 : undefined) && (
                  <div className="flex items-center gap-1">
                    <Clock className="h-3 w-3" />
                    {formatDuration(program.estimatedHours ? program.estimatedHours * 60 : undefined)}
                  </div>
                )}

                <div className="flex items-center gap-1">
                  <DollarSign className="h-3 w-3" />
                  {program.productPrograms && program.productPrograms.length > 0 ? 'Paid Program' : 'Free'}
                </div>

                <div className="flex items-center gap-1">
                  <Calendar className="h-3 w-3" />
                  {formatDistanceToNow(parseISO(program.createdAt), { addSuffix: true })}
                </div>
              </div>

              {/* Skills */}
              {program.skillsProvided && program.skillsProvided.length > 0 && (
                <div className="flex flex-wrap gap-1">
                  {program.skillsProvided.slice(0, 4).map((skill) => (
                    <Badge key={skill.id} variant="secondary" className="text-xs">
                      {skill.name}
                    </Badge>
                  ))}
                  {program.skillsProvided.length > 4 && (
                    <Badge variant="secondary" className="text-xs">
                      +{program.skillsProvided.length - 4}
                    </Badge>
                  )}
                </div>
              )}
            </div>
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
