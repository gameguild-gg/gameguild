'use client';

import { Link } from '@/i18n/navigation';
import { flattenDashboardNavigationItems } from '@/components/console/dashboard-sidebar';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Search } from 'lucide-react';
import { useSearchParams } from 'next/navigation';

export default function DashboardSearchPage() {
  const searchParams = useSearchParams();
  const query = searchParams.get('q')?.trim() ?? '';
  const lowerQuery = query.toLowerCase();
  const items = flattenDashboardNavigationItems();
  const results = lowerQuery
    ? items.filter((item) => `${item.title} ${item.url}`.toLowerCase().includes(lowerQuery))
    : items.slice(0, 12);

  return (
    <div className="space-y-6">
      <section className="rounded-lg border bg-background p-5">
        <div className="flex items-start gap-4">
          <div className="flex size-11 shrink-0 items-center justify-center rounded-lg bg-primary text-primary-foreground">
            <Search className="size-5" />
          </div>
          <div className="space-y-2">
            <div className="text-xs font-semibold uppercase text-muted-foreground">Search</div>
            <h1 className="text-2xl font-semibold tracking-normal">
              {query ? `Results for "${query}"` : 'Dashboard search'}
            </h1>
            <p className="text-sm text-muted-foreground">
              Search dashboard routes, learning operations, community management, testing lab, and launch pad surfaces.
            </p>
          </div>
        </div>
      </section>

      <section className="grid gap-4">
        {results.length > 0 ? (
          results.map((item) => {
            const Icon = item.icon;

            return (
              <Card key={item.url}>
                <CardHeader className="pb-3">
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-start gap-3">
                      <div className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-md bg-muted">
                        <Icon className="size-4" />
                      </div>
                      <div>
                        <CardTitle className="text-base">{item.title}</CardTitle>
                        <CardDescription>{item.url}</CardDescription>
                      </div>
                    </div>
                    <Badge variant="secondary">Page</Badge>
                  </div>
                </CardHeader>
                <CardContent>
                  <Button asChild variant="outline" size="sm">
                    <Link href={item.url}>Open result</Link>
                  </Button>
                </CardContent>
              </Card>
            );
          })
        ) : (
          <Card>
            <CardContent className="flex flex-col items-center gap-3 py-12 text-center">
              <Search className="size-10 text-muted-foreground" />
              <div>
                <h2 className="text-lg font-semibold">No results found</h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  Try a shorter query or open the command palette with Ctrl+K.
                </p>
              </div>
              <Button asChild variant="outline">
                <Link href="/workspace/learning/courses">Open courses</Link>
              </Button>
            </CardContent>
          </Card>
        )}
      </section>
    </div>
  );
}
