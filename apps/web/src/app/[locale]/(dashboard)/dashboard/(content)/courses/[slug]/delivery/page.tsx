'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import React from 'react';
import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard/common/ui/dashboard-page';
import { getProgramBySlugService } from '@/lib/content-management/programs/programs.service';
import { scheduleProgram } from '@/lib/content-management/programs/programs.actions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

interface PageProps {
  params: Promise<{ slug: string; locale: string }>;
}

export default async function CourseDeliveryPage({ params }: PageProps) {
  const { slug } = await params;
  const result = await getProgramBySlugService(slug);
  const program = result.success ? result.data : null;

  async function schedule(formData: FormData) {
    'use server';
    if (!program?.id) return;
    const publishAt = String(formData.get('publishAt') || '');
    if (!publishAt) return;
    await scheduleProgram(program.id, publishAt);
  }

  return (
    <DashboardPage>
      <DashboardPageHeader>
        <DashboardPageTitle>Delivery & Schedule</DashboardPageTitle>
        <DashboardPageDescription>Configure course availability and publication schedule.</DashboardPageDescription>
      </DashboardPageHeader>
      <DashboardPageContent>
        {program ? (
          <div className="space-y-6">
            <Card>
              <CardHeader>
                <CardTitle>Publication schedule</CardTitle>
              </CardHeader>
              <CardContent>
                <form action={schedule} className="grid grid-cols-1 md:grid-cols-[1fr_auto] gap-3 items-end">
                  <div className="space-y-2">
                    <Label htmlFor="publishAt">Publish at</Label>
                    <Input id="publishAt" name="publishAt" type="datetime-local" />
                  </div>
                  <Button type="submit">Schedule</Button>
                </form>
                <div className="text-sm text-muted-foreground mt-3">Current status: {program.status ?? '—'}</div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Enrollment</CardTitle>
              </CardHeader>
              <CardContent className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm text-muted-foreground">
                <div>
                  <div className="font-medium text-foreground">Status</div>
                  <div>{program.enrollmentStatus ?? '—'}</div>
                </div>
                <div>
                  <div className="font-medium text-foreground">Max enrollments</div>
                  <div>{program.maxEnrollments ?? '—'}</div>
                </div>
                <div>
                  <div className="font-medium text-foreground">Deadline</div>
                  <div>{program.enrollmentDeadline ?? '—'}</div>
                </div>
              </CardContent>
            </Card>
          </div>
        ) : (
          <div className="text-muted-foreground">Program not found.</div>
        )}
      </DashboardPageContent>
    </DashboardPage>
  );
}
                              <Button variant="ghost" size="sm" onClick={() => removeSession(session.id)} className="text-destructive hover:text-destructive">
                                <Trash2 className="h-3 w-3" />
                              </Button>
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    ))}
                  </div>
                ) : (
                  <div className="text-center py-8 text-muted-foreground">
                    <Calendar className="h-12 w-12 mx-auto mb-4 opacity-50" />
                    <p>No sessions scheduled yet.</p>
                    <p className="text-sm">Add your first session to get started.</p>
                  </div>
                )}
              </CardContent>
            </Card>
          )}

          {/* Preview Section */}
          <Card className="shadow-lg border-border bg-card/50 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="flex items-center gap-2">👁️ Student Preview</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="p-6 bg-muted/30 rounded-lg border-2 border-dashed">
                <h3 className="font-semibold mb-2">How students will see this course:</h3>
                <div className="space-y-2 text-sm">
                  <p>
                    <strong>Delivery:</strong> {selectedMode?.label}
                  </p>
                  <p>
                    <strong>Format:</strong> {selectedMode?.description}
                  </p>
                  {state.delivery.sessions.length > 0 && (
                    <p>
                      <strong>Sessions:</strong> {state.delivery.sessions.length} scheduled sessions
                    </p>
                  )}
                  {state.enrollment.enrollmentWindow && (
                    <p>
                      <strong>Enrollment:</strong> {state.enrollment.enrollmentWindow.opensAt ? `Opens ${format(state.enrollment.enrollmentWindow.opensAt, 'MMM d, yyyy')}` : 'Open enrollment'}
                    </p>
                  )}
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
