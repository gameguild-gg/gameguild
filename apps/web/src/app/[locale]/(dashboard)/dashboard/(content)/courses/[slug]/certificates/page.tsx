import React from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Award } from 'lucide-react';

interface PageProps {
  params: Promise<{ slug: string; locale: string }>;
}

export default async function Page({ params }: PageProps): Promise<React.JSX.Element> {
  const { slug } = await params;
  const result = await getProgramBySlugService(slug);
  const program = result.success ? (result.data as any) : null;

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Certificates & Skills</DashboardPageTitle>
        <DashboardPageDescription>Review certificates and skills metadata for this course.</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        {!program && <div className="text-muted-foreground">Program not found.</div>}
        {program && (
          <div className="space-y-6 max-w-4xl">
            <Card>
              <CardHeader>
                <CardTitle>Certificates</CardTitle>
              </CardHeader>
              <CardContent>
                {program?.certificates && program.certificates.length > 0 ? (
                  <div className="space-y-3">
                    {program.certificates.map((c: any) => (
                      <div key={c.id} className="p-3 border rounded-lg flex items-start justify-between">
                        <div>
                          <div className="flex items-center gap-2">
                            <Award className="h-4 w-4 text-yellow-600" />
                            <div className="font-medium">{c.name}</div>
                            {c.isActive ? <Badge>Active</Badge> : <Badge variant="secondary">Inactive</Badge>}
                          </div>
                          {c.description && <div className="text-sm text-muted-foreground mt-1">{c.description}</div>}
                          <div className="grid grid-cols-2 md:grid-cols-4 gap-3 text-xs text-muted-foreground mt-2">
                            <div>
                              <div className="font-medium text-foreground">Completion</div>
                              <div>{c.completionPercentage ?? '—'}%</div>
                            </div>
                            <div>
                              <div className="font-medium text-foreground">Min grade</div>
                              <div>{c.minimumGrade ?? '—'}</div>
                            </div>
                            <div>
                              <div className="font-medium text-foreground">Validity</div>
                              <div>{c.validityDays ? `${c.validityDays} days` : '—'}</div>
                            </div>
                            <div>
                              <div className="font-medium text-foreground">Verification</div>
                              <div>{c.verificationMethod ?? '—'}</div>
                            </div>
                          </div>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <div className="text-sm text-muted-foreground">No certificates configured for this program.</div>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Skills</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <div className="font-medium">Required</div>
                  {program?.skillsRequired && program.skillsRequired.length > 0 ? (
                    <div className="flex flex-wrap gap-2 mt-2">
                      {program.skillsRequired.map((t: any) => (
                        <Badge key={t.id} variant="outline">{t.tag?.name ?? t.name ?? t.id}</Badge>
                      ))}
                    </div>
                  ) : (
                    <div className="text-sm text-muted-foreground">No required skills.</div>
                  )}
                </div>
                <div>
                  <div className="font-medium">Provided</div>
                  {program?.skillsProvided && program.skillsProvided.length > 0 ? (
                    <div className="flex flex-wrap gap-2 mt-2">
                      {program.skillsProvided.map((t: any) => (
                        <Badge key={t.id} variant="secondary">{t.tag?.name ?? t.name ?? t.id}</Badge>
                      ))}
                    </div>
                  ) : (
                    <div className="text-sm text-muted-foreground">No provided skills.</div>
                  )}
                </div>
                <div className="text-xs text-muted-foreground">Editing of certificates/skills will be enabled once API endpoints are available.</div>
              </CardContent>
            </Card>
          </div>
        )}
      </DashboardPageContent>
    </DashboardPage>
  );
}
