'use client';

import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Program } from '@/lib/programs/programs.actions';
import { Clock, Eye, DollarSign, Calendar, BookOpen } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { formatDistanceToNow, parseISO } from 'date-fns';

interface ProgramGridProps {
  programs: Program[];
}

export function ProgramGrid({ programs }: ProgramGridProps) {
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
    <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
      {programs.map((program) => (
        <Link key={program.id} href={`/dashboard/courses/${program.slug || program.id}`}>
          <Card className="group overflow-hidden border-slate-700/50 bg-slate-800/30 backdrop-blur-sm shadow-lg hover:shadow-xl transition-all duration-300 hover:border-slate-600/50 cursor-pointer">
            <CardHeader className="p-0">
              <div className="relative aspect-video w-full overflow-hidden bg-slate-700/50">
                {program.imageUrl || program.thumbnail ? (
                  <Image src={program.imageUrl || program.thumbnail || ''} alt={program.title} fill className="object-cover transition-transform duration-300 group-hover:scale-105" />
                ) : (
                  <div className="flex h-full w-full items-center justify-center">
                    <BookOpen className="h-10 w-10 text-slate-400" />
                  </div>
                )}
                <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent" />
                <div className="absolute bottom-1.5 left-1.5 right-1.5 flex justify-between">
                  <Badge className={getStatusColor(program.status)}>{program.status}</Badge>
                  <Badge className={getVisibilityColor(program.visibility)}>{program.visibility}</Badge>
                </div>
              </div>
            </CardHeader>

            <CardContent className="p-3">
              <CardTitle className="line-clamp-2 text-base font-semibold text-slate-200 group-hover:text-white transition-colors">{program.title}</CardTitle>

              {program.shortDescription && <p className="mt-1.5 line-clamp-2 text-sm text-slate-400">{program.shortDescription}</p>}

              <div className="mt-3 flex flex-wrap gap-1.5">
                {program.difficulty && (
                  <div className="flex items-center gap-1 text-xs text-slate-400">
                    <Eye className="h-3 w-3" />
                    {program.difficulty}
                  </div>
                )}

                {formatDuration(program.duration) && (
                  <div className="flex items-center gap-1 text-xs text-slate-400">
                    <Clock className="h-3 w-3" />
                    {formatDuration(program.duration)}
                  </div>
                )}

                <div className="flex items-center gap-1 text-xs text-slate-400">
                  <DollarSign className="h-3 w-3" />
                  {formatPrice(program.price, program.currency)}
                </div>
              </div>

              {program.tags && program.tags.length > 0 && (
                <div className="mt-2 flex flex-wrap gap-1">
                  {program.tags.slice(0, 3).map((tag) => (
                    <Badge key={tag} variant="secondary" className="text-xs">
                      {tag}
                    </Badge>
                  ))}
                  {program.tags.length > 3 && (
                    <Badge variant="secondary" className="text-xs">
                      +{program.tags.length - 3}
                    </Badge>
                  )}
                </div>
              )}
            </CardContent>

            <CardFooter className="px-3 pb-3 pt-0">
              <div className="flex items-center gap-1 text-xs text-slate-400">
                <Calendar className="h-3 w-3" />
                {formatDistanceToNow(parseISO(program.createdAt), { addSuffix: true })}
              </div>
            </CardFooter>
          </Card>
        </Link>
      ))}
    </div>
  );
}
