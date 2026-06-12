import { beforeEach, describe, expect, it, vi } from 'vitest';

import { ContentReportService } from './content-report.service';

describe('ContentReportService', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('submits content reports through the Next moderation route', async () => {
    const fetchMock = vi.spyOn(global, 'fetch').mockResolvedValue({
      ok: true,
      json: async () => ({
        id: 'report-1',
        status: 'pending',
        createdAt: '2026-06-12T00:00:00.000Z',
      }),
    } as Response);

    const result = await ContentReportService.createReport({
      contentType: 'lesson',
      contentId: 'asset-reference-id',
      contentTitle: 'Movement lesson',
      reportType: 'misinformation',
      reason: 'misinformation',
      description: 'The instructions are misleading.',
      userId: 'current-user',
    });

    expect(result).toEqual({
      success: true,
      message: 'Report submitted for moderation.',
      reportId: 'report-1',
    });
    expect(fetchMock).toHaveBeenCalledWith('/api/courses/content-reports', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        contentType: 'lesson',
        contentId: 'asset-reference-id',
        contentTitle: 'Movement lesson',
        reportType: 'misinformation',
        reason: 'misinformation',
        description: 'The instructions are misleading.',
      }),
    });
  });

  it('returns backend errors instead of reporting fake success', async () => {
    vi.spyOn(global, 'fetch').mockResolvedValue({
      ok: false,
      json: async () => ({ error: 'Content cannot be reported.' }),
    } as Response);

    const result = await ContentReportService.reportContent({
      contentType: 'lesson',
      contentId: 'course-content-id',
      reason: 'technical',
      userId: 'current-user',
    });

    expect(result).toEqual({
      success: false,
      error: 'Content cannot be reported.',
    });
  });
});
