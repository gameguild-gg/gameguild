import React from 'react';
import Link from 'next/link';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import type { ProjectVersion } from '@/lib/api/generated/types.gen';
import { getProjectBySlug } from '@/lib/content-management/projects/projects.actions';

export default async function VersionsPage({ params }: { params: { slug: string } }): Promise<React.JSX.Element> {
  const slug = params.slug;

  const projectResp = await getProjectBySlug({ url: '/api/projects/slug/{slug}', path: { slug }, query: { includeCollaborators: false, includeReleases: true, includeTeam: false } });
  const project = (projectResp as any)?.data as { title?: string; versions?: ProjectVersion[] } | undefined;
  const versions: ProjectVersion[] = project?.versions ?? [];

  return (
    <Card className="dark-card">
      <CardHeader>
        <CardTitle>Versions & Builds</CardTitle>
        <CardDescription>
          {project?.title ? (
            <span>
              Manage and upload builds for <span className="font-medium text-foreground">{project.title}</span>.
            </span>
          ) : (
            'Manage and upload new builds for your players.'
          )}
        </CardDescription>
      </CardHeader>
      <CardContent>
        {versions.length === 0 ? (
          <div className="flex flex-col items-center justify-center text-center py-12">
            <p className="text-sm text-muted-foreground mb-4">No versions uploaded yet.</p>
            <Button variant="secondary" disabled>
              Upload build
            </Button>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">{versions.length} version{versions.length > 1 ? 's' : ''} found</p>
              <Button variant="secondary" size="sm" disabled>
                Upload build
              </Button>
            </div>
            <div className="rounded-md border border-border/50 overflow-hidden">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="w-[140px]">Version</TableHead>
                    <TableHead>Status</TableHead>
                    <TableHead className="hidden lg:table-cell">Release notes</TableHead>
                    <TableHead className="w-[120px] text-right">Downloads</TableHead>
                    <TableHead className="w-[160px] text-right">Created</TableHead>
                    <TableHead className="w-[160px] text-right">Actions</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {versions.map((v) => (
                    <TableRow key={v.id} className="hover:bg-muted/30">
                      <TableCell className="font-medium">v{v.versionNumber}</TableCell>
                      <TableCell>
                        <Badge variant="outline" className="capitalize">
                          {String(v.status ?? 'unknown')}
                        </Badge>
                      </TableCell>
                      <TableCell className="hidden lg:table-cell text-muted-foreground">
                        {v.releaseNotes ? (
                          <span className="line-clamp-1" title={v.releaseNotes || undefined}>
                            {v.releaseNotes}
                          </span>
                        ) : (
                          <span className="text-muted-foreground/70">—</span>
                        )}
                      </TableCell>
                      <TableCell className="text-right">{typeof v.downloadCount === 'number' ? v.downloadCount : 0}</TableCell>
                      <TableCell className="text-right">
                        {v.createdAt ? new Date(v.createdAt).toLocaleDateString() : ''}
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          <Link href={`../../../testing-lab/requests?projectVersionId=${v.id}`} className="text-sm">
                            <Button variant="ghost" size="sm">View requests</Button>
                          </Link>
                          <Button variant="ghost" size="sm" disabled>
                            Download
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
