import { DashboardPage, DashboardPageContent, DashboardPageDescription, DashboardPageHeader, DashboardPageTitle } from '@/components/dashboard';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { TestingRequestActions } from '@/components/testing-lab/requests/testing-request-actions';
import { getTestingRequestsWithDetailsAction } from '@/lib/admin/testing-lab/requests/testing-requests.actions';
import { Calendar, Clock, ExternalLink, Gamepad2, User } from 'lucide-react';
import Link from 'next/link';
import { notFound } from 'next/navigation';
import React from 'react';

interface PageProps {
    params: Promise<{ id: string; locale: string }>;
}

export default async function Page({ params }: PageProps): Promise<React.JSX.Element> {
    const { id } = await params;

    // Fetch all testing requests and find the one with matching ID
    const result = await getTestingRequestsWithDetailsAction();
    const testingRequest = result.data?.find(request => request.id === id);

    if (!testingRequest) {
        notFound();
    }

    const getStatusBadge = (status: number) => {
        switch (status) {
            case 0: // Draft
                return <Badge variant="outline">Waiting</Badge>;
            case 1: // Open
                return <Badge variant="secondary">Under Review</Badge>;
            case 2: // InProgress
                return <Badge className="bg-green-500">Approved</Badge>;
            case 3: // Completed
                return <Badge className="bg-green-600">Approved</Badge>;
            case 4: // Cancelled
                return <Badge variant="destructive">Rejected</Badge>;
            default:
                return <Badge variant="outline">Unknown</Badge>;
        }
    };

    const formatDate = (dateString?: string) => {
        if (!dateString) return 'N/A';
        return new Date(dateString).toLocaleDateString('pt-BR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    };

    return (
        <DashboardPage>
            <DashboardPageHeader>
                <div className="flex items-center justify-between">
                    <div>
                        <DashboardPageTitle>{testingRequest.title}</DashboardPageTitle>
                        <DashboardPageDescription>Testing request details and management</DashboardPageDescription>
                    </div>
                    <div className="flex items-center gap-2">
                        <Button variant="outline" asChild>
                            <Link href="/dashboard/testing-lab/requests">
                                Back to Requests
                            </Link>
                        </Button>
                        <TestingRequestActions requestId={testingRequest.id || ''} status={testingRequest.status} />
                    </div>
                </div>
            </DashboardPageHeader>

            <DashboardPageContent>
                <div className="space-y-6">
                    {/* Status and Basic Info */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center justify-between">
                                Request Overview
                                {getStatusBadge(testingRequest.status)}
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <div className="flex items-center gap-2 text-sm font-medium">
                                        <Gamepad2 className="h-4 w-4" />
                                        Game Information
                                    </div>
                                    <div className="text-sm text-muted-foreground ml-6">
                                        <div className="font-medium">{testingRequest.gameName}</div>
                                        <div>Version: {testingRequest.gameVersion}</div>
                                    </div>
                                </div>

                                <div className="space-y-2">
                                    <div className="flex items-center gap-2 text-sm font-medium">
                                        <Clock className="h-4 w-4" />
                                        Submitted
                                    </div>
                                    <div className="text-sm text-muted-foreground ml-6">
                                        {formatDate(testingRequest.createdAt)}
                                    </div>
                                </div>

                                <div className="space-y-2">
                                    <div className="flex items-center gap-2 text-sm font-medium">
                                        <Calendar className="h-4 w-4" />
                                        Testing Period
                                    </div>
                                    <div className="text-sm text-muted-foreground ml-6">
                                        <div>Start: {formatDate(testingRequest.startDate)}</div>
                                        <div>End: {formatDate(testingRequest.endDate)}</div>
                                    </div>
                                </div>

                                <div className="space-y-2">
                                    <div className="flex items-center gap-2 text-sm font-medium">
                                        <User className="h-4 w-4" />
                                        Testers
                                    </div>
                                    <div className="text-sm text-muted-foreground ml-6">
                                        {testingRequest.currentTesterCount || 0} / {testingRequest.maxTesters || 'Unlimited'}
                                    </div>
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Assigned Session */}
                    {testingRequest.assignedSession && (
                        <Card>
                            <CardHeader>
                                <CardTitle>Assigned Testing Session</CardTitle>
                            </CardHeader>
                            <CardContent>
                                <div className="flex items-center justify-between">
                                    <div>
                                        <div className="font-medium">{testingRequest.assignedSession.sessionName}</div>
                                        <div className="text-sm text-muted-foreground">
                                            Session Date: {formatDate(testingRequest.assignedSession.sessionDate)}
                                        </div>
                                    </div>
                                    <Button variant="outline" asChild>
                                        <Link
                                            href={`/dashboard/testing-lab/sessions/${testingRequest.assignedSession.id}`}
                                            className="flex items-center gap-2"
                                        >
                                            View Session
                                            <ExternalLink className="h-4 w-4" />
                                        </Link>
                                    </Button>
                                </div>
                            </CardContent>
                        </Card>
                    )}

                    {/* Description */}
                    {testingRequest.description && (
                        <Card>
                            <CardHeader>
                                <CardTitle>Description</CardTitle>
                            </CardHeader>
                            <CardContent>
                                <div className="prose prose-sm max-w-none">
                                    <p className="whitespace-pre-wrap">{testingRequest.description}</p>
                                </div>
                            </CardContent>
                        </Card>
                    )}

                    {/* Download and Instructions */}
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        {testingRequest.downloadUrl && (
                            <Card>
                                <CardHeader>
                                    <CardTitle>Game Download</CardTitle>
                                </CardHeader>
                                <CardContent>
                                    <Button asChild className="w-full">
                                        <a href={testingRequest.downloadUrl} target="_blank" rel="noopener noreferrer">
                                            Download Game
                                            <ExternalLink className="h-4 w-4 ml-2" />
                                        </a>
                                    </Button>
                                </CardContent>
                            </Card>
                        )}

                        {testingRequest.instructionsContent && (
                            <Card>
                                <CardHeader>
                                    <CardTitle>Testing Instructions</CardTitle>
                                </CardHeader>
                                <CardContent>
                                    <div className="prose prose-sm max-w-none">
                                        <p className="whitespace-pre-wrap">{testingRequest.instructionsContent}</p>
                                    </div>
                                </CardContent>
                            </Card>
                        )}
                    </div>

                    {/* Feedback Form */}
                    {testingRequest.feedbackFormContent && (
                        <Card>
                            <CardHeader>
                                <CardTitle>Feedback Form</CardTitle>
                            </CardHeader>
                            <CardContent>
                                <div className="prose prose-sm max-w-none">
                                    <p className="whitespace-pre-wrap">{testingRequest.feedbackFormContent}</p>
                                </div>
                            </CardContent>
                        </Card>
                    )}
                </div>
            </DashboardPageContent>
        </DashboardPage>
    );
}
