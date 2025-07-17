export interface ContentReport {
  id: string;
  contentId: string;
  contentTitle: string;
  reporterId: string;
  reportType: string;
  description: string;
  status: 'pending' | 'under-review' | 'resolved' | 'dismissed';
  reportedAt: Date;
}

export interface CreateReportRequest {
  contentId: string;
  contentTitle: string;
  reportType: string;
  description: string;
}

export interface CreateReportResponse {
  success: boolean;
  reportId?: string;
  message: string;
}

// Mock service for content reporting - replace with actual API integration
export class ContentReportService {
  private static reports: ContentReport[] = [];

  static async createReport(request: CreateReportRequest): Promise<CreateReportResponse> {
    try {
      // Simulate API delay
      await new Promise(resolve => setTimeout(resolve, 1000));

      const report: ContentReport = {
        id: `report-${Date.now()}`,
        contentId: request.contentId,
        contentTitle: request.contentTitle,
        reporterId: 'current-user-id', // In real app, get from auth context
        reportType: request.reportType,
        description: request.description,
        status: 'pending',
        reportedAt: new Date(),
      };

      this.reports.push(report);

      console.log('Content report submitted:', report);

      return {
        success: true,
        reportId: report.id,
        message: 'Report submitted successfully. Our moderators will review it shortly.',
      };
    } catch (error) {
      console.error('Error creating report:', error);
      return {
        success: false,
        message: 'Failed to submit report. Please try again.',
      };
    }
  }

  static async getReports(): Promise<ContentReport[]> {
    return this.reports;
  }

  static async updateReportStatus(
    reportId: string, 
    status: ContentReport['status']
  ): Promise<boolean> {
    const report = this.reports.find(r => r.id === reportId);
    if (report) {
      report.status = status;
      return true;
    }
    return false;
  }
}
