import { z } from 'zod';

// Testing Session Creation Schema - No testing requests required during creation
export const testingSessionCreateSchema = z.object({
    sessionName: z.string().min(1, 'Session name is required').max(255, 'Session name must be less than 255 characters'),
    sessionDate: z.string().min(1, 'Session date is required'),
    startTime: z.string().min(1, 'Start time is required'),
    endTime: z.string().min(1, 'End time is required'),
    maxTesters: z.number().min(1, 'Must have at least 1 tester slot').max(500, 'Cannot exceed 500 testers'),
    maxProjects: z.number().min(1, 'Must have at least 1 project slot').max(50, 'Cannot exceed 50 projects'),
    locationId: z.string().min(1, 'Location is required'),
    managerUserId: z.string().optional(),
    status: z.number().min(0).max(3), // Required field
}).superRefine((data, ctx) => {
    // Ensure start time is before end time
    if (data.startTime >= data.endTime) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: 'Start time must be before end time',
            path: ['endTime'],
        });
    }

    // Ensure session date is not in the past (for new sessions)
    const sessionDate = new Date(data.sessionDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (sessionDate < today) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: 'Session date cannot be in the past',
            path: ['sessionDate'],
        });
    }
});

// Testing Request Enrollment Schema - For developers requesting to join sessions
export const testingRequestEnrollmentSchema = z.object({
    sessionId: z.string().min(1, 'Session ID is required'),
    testingRequestId: z.string().min(1, 'Testing request ID is required'),
    requestedTesters: z.number().min(1, 'Must request at least 1 tester').max(50, 'Cannot exceed 50 testers'),
    message: z.string().optional(),
});

// Admin Enrollment Decision Schema - For approving/rejecting enrollment requests
export const enrollmentDecisionSchema = z.object({
    enrollmentId: z.string().min(1, 'Enrollment ID is required'),
    decision: z.enum(['approved', 'rejected']),
    adminMessage: z.string().optional(),
    approvedTesters: z.number().optional(), // Admin can approve fewer testers than requested
});

// Session Capacity Validation Schema - Updated for post-creation management
export const sessionCapacityValidationSchema = z.object({
    sessionMaxTesters: z.number(),
    sessionMaxProjects: z.number(),
    locationMaxTesters: z.number(),
    locationMaxProjects: z.number(),
    currentEnrollments: z.array(z.object({
        id: z.string(),
        testingRequestId: z.string(),
        title: z.string(),
        approvedTesters: z.number(),
        status: z.enum(['pending', 'approved', 'rejected']),
    })),
}).superRefine((data, ctx) => {
    // Validate that session capacity doesn't exceed location capacity
    if (data.sessionMaxTesters > data.locationMaxTesters) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: `Session tester capacity (${data.sessionMaxTesters}) cannot exceed location capacity (${data.locationMaxTesters})`,
            path: ['sessionMaxTesters'],
        });
    }

    if (data.sessionMaxProjects > data.locationMaxProjects) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: `Session project capacity (${data.sessionMaxProjects}) cannot exceed location capacity (${data.locationMaxProjects})`,
            path: ['sessionMaxProjects'],
        });
    }

    // Validate current approved enrollments don't exceed capacity
    const approvedEnrollments = data.currentEnrollments.filter(e => e.status === 'approved');
    const totalApprovedTesters = approvedEnrollments.reduce((sum, e) => sum + e.approvedTesters, 0);

    if (totalApprovedTesters > data.sessionMaxTesters) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: `Total approved testers (${totalApprovedTesters}) exceed session capacity (${data.sessionMaxTesters})`,
            path: ['currentEnrollments'],
        });
    }

    if (approvedEnrollments.length > data.sessionMaxProjects) {
        ctx.addIssue({
            code: z.ZodIssueCode.custom,
            message: `Number of approved projects (${approvedEnrollments.length}) exceeds session project capacity (${data.sessionMaxProjects})`,
            path: ['currentEnrollments'],
        });
    }
});

export type TestingSessionCreateData = z.infer<typeof testingSessionCreateSchema>;
export type TestingRequestEnrollmentData = z.infer<typeof testingRequestEnrollmentSchema>;
export type EnrollmentDecisionData = z.infer<typeof enrollmentDecisionSchema>;
export type SessionCapacityValidationData = z.infer<typeof sessionCapacityValidationSchema>;
