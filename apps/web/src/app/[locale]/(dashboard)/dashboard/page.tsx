import { getDashboardContexts } from '@/lib/dashboard-contexts';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ShieldCheck } from 'lucide-react';

export default async function ManagementDashboardPage() {
  const dashboard = await getDashboardContexts();

  return <div className="space-y-6">
    <header><Badge variant="outline">Operations</Badge><h1 className="mt-2 text-3xl font-bold tracking-tight">Management dashboard</h1><p className="text-muted-foreground">Administrative capabilities for the selected tenant. Your own Teams and Projects remain in My Workspace.</p></header>
    <Card><CardHeader><CardTitle className="flex items-center gap-2"><ShieldCheck className="size-5" />Granted capabilities</CardTitle><CardDescription>Navigation is built from these capabilities; event participation does not create any of them.</CardDescription></CardHeader><CardContent><div className="flex flex-wrap gap-2">{dashboard.capabilities.map((capability) => <Badge key={capability} variant="secondary">{capability}</Badge>)}</div></CardContent></Card>
  </div>;
}
