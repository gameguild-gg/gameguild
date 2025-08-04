'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Program } from '../../../../../old/quick-fix/programs/programs.actions';
import { Clock, Eye, DollarSign, Calendar, MoreHorizontal } from 'lucide-react';
import Link from 'next/link';
import { formatDistanceToNow, parseISO } from 'date-fns';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';

interface ProgramTableProps {
  programs: Program[];
}

export function ProgramTable({ programs }: ProgramTableProps) {
  const formatPrice = (price?: number, currency?: string) => {
    if (!price) return 'Free';
    return `${currency || '$'}${price}`;
  };

  const formatDuration = (duration?: number) => {
    if (!duration) return '-';
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
    <Card className="border-slate-700/50 bg-slate-800/30 backdrop-blur-sm shadow-lg hover:shadow-xl transition-all duration-300">
      <CardContent className="p-0">
        <Table>
          <TableHeader>
            <TableRow className="border-slate-700/50 hover:bg-slate-800/50">
              <TableHead className="text-slate-200">Title</TableHead>
              <TableHead className="text-slate-200">Status</TableHead>
              <TableHead className="text-slate-200">Visibility</TableHead>
              <TableHead className="text-slate-200">Difficulty</TableHead>
              <TableHead className="text-slate-200">Duration</TableHead>
              <TableHead className="text-slate-200">Price</TableHead>
              <TableHead className="text-slate-200">Created</TableHead>
              <TableHead className="text-slate-200 w-[100px]">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {programs.map((program) => (
              <TableRow key={program.id} className="border-slate-700/50 hover:bg-slate-800/30">
                <TableCell className="py-2">
                  <div>
                    <div className="font-medium text-slate-200 line-clamp-1">{program.title}</div>
                    {program.shortDescription && <div className="text-sm text-slate-400 line-clamp-1 mt-0.5">{program.shortDescription}</div>}
                    {program.tags && program.tags.length > 0 && (
                      <div className="flex flex-wrap gap-1 mt-1">
                        {program.tags.slice(0, 2).map((tag) => (
                          <Badge key={tag} variant="secondary" className="text-xs">
                            {tag}
                          </Badge>
                        ))}
                        {program.tags.length > 2 && (
                          <Badge variant="secondary" className="text-xs">
                            +{program.tags.length - 2}
                          </Badge>
                        )}
                      </div>
                    )}
                  </div>
                </TableCell>
                <TableCell className="py-2">
                  <Badge className={getStatusColor(program.status)}>{program.status}</Badge>
                </TableCell>
                <TableCell className="py-2">
                  <Badge className={getVisibilityColor(program.visibility)}>{program.visibility}</Badge>
                </TableCell>
                <TableCell className="py-2">
                  <div className="flex items-center gap-1 text-slate-400">
                    {program.difficulty && (
                      <>
                        <Eye className="h-3 w-3" />
                        <span className="text-sm">{program.difficulty}</span>
                      </>
                    )}
                    {!program.difficulty && <span className="text-sm">-</span>}
                  </div>
                </TableCell>
                <TableCell className="py-2">
                  <div className="flex items-center gap-1 text-slate-400">
                    <Clock className="h-3 w-3" />
                    <span className="text-sm">{formatDuration(program.duration)}</span>
                  </div>
                </TableCell>
                <TableCell className="py-2">
                  <div className="flex items-center gap-1 text-slate-400">
                    <DollarSign className="h-3 w-3" />
                    <span className="text-sm">{formatPrice(program.price, program.currency)}</span>
                  </div>
                </TableCell>
                <TableCell className="py-2">
                  <div className="flex items-center gap-1 text-slate-400">
                    <Calendar className="h-3 w-3" />
                    <span className="text-sm">{formatDistanceToNow(parseISO(program.createdAt), { addSuffix: true })}</span>
                  </div>
                </TableCell>
                <TableCell className="py-2">
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem asChild>
                        <Link href={`/dashboard/courses/${program.slug || program.id}`}>View Details</Link>
                      </DropdownMenuItem>
                      <DropdownMenuItem asChild>
                        <Link href={`/dashboard/courses/${program.slug || program.id}/edit`}>Edit Program</Link>
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}
