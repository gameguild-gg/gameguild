/**
 * Stub for content report service.
 * This module is disabled in production.
 */

export interface ContentReport {
    id: string;
    contentType: string;
    contentId: string;
    contentTitle?: string;
    reportType?: string;
    reason: string;
    description?: string;
    status: 'pending' | 'reviewed' | 'resolved' | 'dismissed';
    createdAt: string;
    userId: string;
}

export async function reportContent(_data: Omit<ContentReport, 'id' | 'status' | 'createdAt'>) {
    return { success: true, message: 'Report submitted (stub)' };
}

export async function getContentReports(_contentType: string, _contentId: string) {
    return { data: [] as ContentReport[], error: null };
}

export async function updateReportStatus(_reportId: string, _status: ContentReport['status']) {
    return { success: false, error: 'Content report management is disabled' };
}

// Export as class-like object for compatibility
export const ContentReportService = {
    reportContent,
    getContentReports,
    updateReportStatus,
    // Alias methods
    report: reportContent,
    getReports: getContentReports,
    updateStatus: updateReportStatus,
    // Additional method for component compatibility
    createReport: async (_data: Omit<ContentReport, 'id' | 'status' | 'createdAt'>) => {
        return { success: true, message: 'Report submitted (stub)' };
    },
};

export const contentReportService = ContentReportService;

export default contentReportService;
