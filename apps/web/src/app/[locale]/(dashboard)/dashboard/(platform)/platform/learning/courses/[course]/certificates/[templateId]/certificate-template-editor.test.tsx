import { fireEvent, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ updateCertificateTemplate: vi.fn() }));

vi.mock('@/lib/learning/actions', () => ({
  updateCertificateTemplate: mocks.updateCertificateTemplate,
}));

import { CertificateTemplateEditor } from './certificate-template-editor';

describe('CertificateTemplateEditor', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.updateCertificateTemplate.mockResolvedValue({ success: true, data: null });
  });

  it('edits template fields and persists default and active state', async () => {
    const user = userEvent.setup();
    render(
      <CertificateTemplateEditor
        template={{
          id: 'template-1',
          courseId: 'course-1',
          name: 'Original',
          description: null,
          status: 'active',
          isDefault: false,
          issuedCount: 0,
          createdAt: '2026-07-01T00:00:00.000Z',
          updatedAt: '2026-07-01T00:00:00.000Z',
          previewUrl: '/api/certificates/templates/template-1',
          templateHtml: '<main>Original</main>',
          templateStyles: null,
        }}
      />,
    );

    await user.clear(screen.getByLabelText('Template name'));
    await user.type(screen.getByLabelText('Template name'), 'Completion');
    await user.type(screen.getByLabelText('Description'), 'Course credential');
    fireEvent.change(screen.getByLabelText('Template HTML'), { target: { value: '<main>{{recipientName}}</main>' } });
    fireEvent.change(screen.getByLabelText('Template styles'), { target: { value: 'main { color: navy; }' } });
    await user.click(screen.getByRole('switch', { name: 'Default template' }));
    await user.click(screen.getByRole('button', { name: 'Save certificate template' }));

    expect(mocks.updateCertificateTemplate).toHaveBeenCalledWith({
      courseId: 'course-1',
      templateId: 'template-1',
      name: 'Completion',
      description: 'Course credential',
      templateHtml: '<main>{{recipientName}}</main>',
      templateStyles: 'main { color: navy; }',
      isDefault: true,
      isActive: true,
    });
    expect(await screen.findByRole('status')).toHaveTextContent('Certificate template saved.');
    expect(screen.getByTitle('Certificate preview')).toHaveAttribute('sandbox', '');
  }, 15_000);
});
