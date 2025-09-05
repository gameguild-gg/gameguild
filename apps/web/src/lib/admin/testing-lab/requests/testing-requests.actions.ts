'use server';

import { getTestingRequests, getTestingRequestsSearch, getTestingSessionsByRequestByTestingRequestId } from '@/lib/api/generated/sdk.gen';
import { TestingRequest, TestingSession } from '@/lib/api/generated/types.gen';
import { configureAuthenticatedClient } from '@/lib/core/api/authenticated-client';

export interface TestingRequestActionResult<T = unknown> {
    success: boolean;
    data?: T;
    error?: string;
}

// Enhanced testing request with session and game information
export interface EnhancedTestingRequest extends TestingRequest {
    assignedSession?: TestingSession;
    gameName?: string;
    gameVersion?: string;
}

export async function getTestingRequestsAction(): Promise<TestingRequestActionResult<TestingRequest[]>> {
    try {
        console.log('Fetching testing requests from API...');

        await configureAuthenticatedClient();

        const response = await getTestingRequests({
            query: {
                skip: 0,
                take: 100
            }
        });

        if (response.data) {
            return {
                success: true,
                data: response.data,
            };
        }

        return {
            success: true,
            data: [],
        };
    } catch (error) {
        console.error('Failed to load testing requests:', error);
        return {
            success: false,
            error: 'Failed to load testing requests',
        };
    }
}

export async function searchTestingRequestsAction({ query }: { query: { searchTerm: string } }): Promise<TestingRequestActionResult<TestingRequest[]>> {
    try {
        console.log('Searching testing requests for:', query.searchTerm);

        await configureAuthenticatedClient();

        const response = await getTestingRequestsSearch({
            query: {
                searchTerm: query.searchTerm
            }
        });

        if (response.data) {
            return {
                success: true,
                data: response.data,
            };
        }

        return {
            success: true,
            data: [],
        };
    } catch (error) {
        console.error('Failed to search testing requests:', error);
        return {
            success: false,
            error: 'Failed to search testing requests',
        };
    }
}

// Action that fetches testing requests with session and game information
export async function getTestingRequestsWithDetailsAction(): Promise<TestingRequestActionResult<EnhancedTestingRequest[]>> {
    try {
        console.log('Fetching enhanced testing requests from API...');

        await configureAuthenticatedClient();

        const response = await getTestingRequests({
            query: {
                skip: 0,
                take: 100
            }
        });

        if (!response.data) {
            return {
                success: true,
                data: [],
            };
        }

        console.log(`Loaded ${response.data.length} testing requests, starting enhancement...`);

        // Log the raw data structure for debugging
        console.log('Raw testing requests data:', JSON.stringify(response.data.slice(0, 2), null, 2));

        // Enhance each testing request with session and game information
        const enhancedRequests: EnhancedTestingRequest[] = await Promise.all(
            response.data.map(async (request) => {
                const enhanced: EnhancedTestingRequest = {
                    ...request,
                    gameName: request.projectVersion?.project?.title || 'Unknown Game',
                    gameVersion: request.projectVersion?.versionNumber || 'Unknown Version',
                };

                // Log project info for debugging
                console.log(`Testing request ${request.id} project info:`, {
                    hasProjectVersion: !!request.projectVersion,
                    hasProject: !!request.projectVersion?.project,
                    hasTitle: !!request.projectVersion?.project?.title,
                    projectVersionData: request.projectVersion,
                    gameName: enhanced.gameName,
                    gameVersion: enhanced.gameVersion
                });

                // Try to fetch the assigned session for this request
                try {
                    if (request.id) {
                        const sessionResponse = await getTestingSessionsByRequestByTestingRequestId({
                            path: {
                                testingRequestId: request.id
                            }
                        });

                        if (sessionResponse.data && sessionResponse.data.length > 0) {
                            const session = sessionResponse.data[0];
                            enhanced.assignedSession = session;
                            console.log(`Found session for request ${request.id}: ${session?.sessionName || 'Unknown Session'}`);
                        } else {
                            console.log(`No session found for request ${request.id}`);
                        }
                    }
                } catch (sessionError) {
                    console.warn(`Failed to fetch session for request ${request.id}:`, sessionError);
                    // Continue without session info - not critical
                }

                return enhanced;
            })
        );

        const assignedCount = enhancedRequests.filter(req => req.assignedSession).length;
        console.log(`Enhanced ${enhancedRequests.length} testing requests (${assignedCount} assigned to sessions)`);

        return {
            success: true,
            data: enhancedRequests,
        };
    } catch (error) {
        console.error('Failed to load enhanced testing requests:', error);
        return {
            success: false,
            error: 'Failed to load enhanced testing requests',
        };
    }
}
