import { DashboardPage, DashboardPageContent, DashboardPageHeader } from '@/components/dashboard';
import { Skeleton } from '@/components/ui/skeleton';
import { ArrowLeft, BarChart3, Building, Globe, Settings, Users } from 'lucide-react';
import Link from 'next/link';

export default function Loading() {
    return (
        <DashboardPage>
            <DashboardPageHeader>
                <div className="flex items-center gap-4">
                    <Link
                        href="/dashboard/tenant"
                        className="flex items-center justify-center w-8 h-8 rounded-lg border border-border hover:bg-accent"
                    >
                        <ArrowLeft className="h-4 w-4" />
                    </Link>
                    <div className="space-y-2">
                        <Skeleton className="h-8 w-48" />
                        <Skeleton className="h-4 w-72" />
                    </div>
                </div>
            </DashboardPageHeader>
            <DashboardPageContent>
                <div className="space-y-6">
                    {/* Tabs skeleton */}
                    <div className="flex space-x-1 rounded-lg bg-muted p-1">
                        {[
                            { icon: Building, label: 'Overview' },
                            { icon: Users, label: 'Users' },
                            { icon: Globe, label: 'Domains' },
                            { icon: BarChart3, label: 'Analytics' },
                            { icon: Settings, label: 'Settings' },
                        ].map(({ icon: Icon, label }, index) => (
                            <div key={label} className="flex items-center gap-2 px-3 py-2">
                                <Icon className="h-4 w-4 text-muted-foreground" />
                                <Skeleton className="h-4 w-16" />
                            </div>
                        ))}
                    </div>

                    {/* Content skeleton */}
                    <div className="space-y-6">
                        <div className="grid gap-6 md:grid-cols-2">
                            <div className="space-y-4 rounded-lg border p-6">
                                <Skeleton className="h-6 w-32" />
                                <div className="space-y-3">
                                    {[1, 2, 3, 4].map((i) => (
                                        <div key={i} className="space-y-2">
                                            <Skeleton className="h-4 w-24" />
                                            <Skeleton className="h-4 w-full" />
                                        </div>
                                    ))}
                                </div>
                            </div>

                            <div className="space-y-4 rounded-lg border p-6">
                                <Skeleton className="h-6 w-32" />
                                <div className="space-y-3">
                                    {[1, 2, 3].map((i) => (
                                        <div key={i} className="space-y-2">
                                            <Skeleton className="h-4 w-24" />
                                            <Skeleton className="h-4 w-full" />
                                        </div>
                                    ))}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </DashboardPageContent>
        </DashboardPage>
    );
}
