import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { CertificateTemplateManager } from './certificate-template-manager';

const createCertificateTemplateMock = vi.fn();
const deleteCertificateTemplateMock = vi.fn();
const refreshMock = vi.fn();

vi.mock('@/lib/learning/actions', () => ({
  createCertificateTemplate: (...args: unknown[]) => createCertificateTemplateMock(...args),
  deleteCertificateTemplate: (...args: unknown[]) => deleteCertificateTemplateMock(...args),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock('next/navigation', () => ({
  useRouter: () => ({ refresh: refreshMock }),
}));

describe('CertificateTemplateManager', () => {
  beforeEach(() => {
    createCertificateTemplateMock.mockReset();
    deleteCertificateTemplateMock.mockReset();
    refreshMock.mockReset();
    createCertificateTemplateMock.mockResolvedValue({ success: true, data: { id: 'template-2' } });
    deleteCertificateTemplateMock.mockResolvedValue({ success: true, data: null });
  });

  it('creates a certificate template from the dashboard form', async () => {
    render(<CertificateTemplateManager courseId="course-1" templates={[]} />);

    fireEvent.change(screen.getByLabelText(/^name$/i), {
      target: { value: 'Completion certificate' },
    });
    fireEvent.click(screen.getByRole('button', { name: /create template/i }));

    await waitFor(() => {
      expect(createCertificateTemplateMock).toHaveBeenCalledWith(
        expect.objectContaining({
          courseId: 'course-1',
          name: 'Completion certificate',
          templateHtml: expect.stringContaining('{{recipientName}}'),
        }),
      );
    });
    expect(refreshMock).toHaveBeenCalled();
    expect(screen.getByText('Certificate template created.')).toBeInTheDocument();
  });

  it('links existing templates and deletes unused templates', async () => {
    render(
      <CertificateTemplateManager
        courseId="course-1"
        templates={[
          {
            id: 'template-1',
            courseId: 'course-1',
            name: 'Completion certificate',
            description: 'Default credential',
            status: 'active',
            issuedCount: 0,
            createdAt: '2026-01-01T00:00:00.000Z',
            updatedAt: '2026-01-02T00:00:00.000Z',
          },
        ]}
      />,
    );

    expect(screen.getByRole('link', { name: /completion certificate/i })).toHaveAttribute(
      'href',
      '/workspace/learning/courses/course-1/certificates/template-1',
    );

    fireEvent.click(screen.getByRole('button', { name: /delete completion certificate/i }));

    await waitFor(() => {
      expect(deleteCertificateTemplateMock).toHaveBeenCalledWith('course-1', 'template-1');
    });
    expect(refreshMock).toHaveBeenCalled();
  });
});
