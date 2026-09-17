import "@testing-library/jest-dom/vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { CertificateTemplateManager } from "./certificate-template-manager";

const createCertificateTemplateMock = vi.fn();
const deleteCertificateTemplateMock = vi.fn();
const refreshMock = vi.fn();

vi.mock("@/lib/learning/actions", () => ({
  createCertificateTemplate: (...args: unknown[]) =>
    createCertificateTemplateMock(...args),
  deleteCertificateTemplate: (...args: unknown[]) =>
    deleteCertificateTemplateMock(...args),
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({
    href,
    children,
    ...props
  }: {
    href: string;
    children: ReactNode;
  }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

vi.mock("next/navigation", () => ({
  usePathname: () => "/workspace/learning",
  useRouter: () => ({ refresh: refreshMock }),
}));

describe("CertificateTemplateManager", () => {
  beforeEach(() => {
    createCertificateTemplateMock.mockReset();
    deleteCertificateTemplateMock.mockReset();
    refreshMock.mockReset();
    createCertificateTemplateMock.mockResolvedValue({
      success: true,
      data: { id: "template-2" },
    });
    deleteCertificateTemplateMock.mockResolvedValue({
      success: true,
      data: null,
    });
  });

  it("creates a certificate template from the dashboard form", async () => {
    render(<CertificateTemplateManager courseId="course-1" templates={[]} />);

    fireEvent.change(screen.getByLabelText(/^name$/i), {
      target: { value: "Completion certificate" },
    });
    fireEvent.click(screen.getByRole("button", { name: /create template/i }));

    await waitFor(() => {
      expect(createCertificateTemplateMock).toHaveBeenCalledWith(
        expect.objectContaining({
          courseId: "course-1",
          name: "Completion certificate",
          templateHtml: expect.stringContaining("{{recipientName}}"),
        }),
      );
    });
    expect(refreshMock).toHaveBeenCalled();
    expect(
      screen.getByText("Certificate template created."),
    ).toBeInTheDocument();
  });

  it("links existing templates and deletes unused templates", async () => {
    render(
      <CertificateTemplateManager
        courseId="course-1"
        templates={[
          {
            id: "template-1",
            courseId: "course-1",
            name: "Completion certificate",
            description: "Default credential",
            status: "active",
            issuedCount: 0,
            createdAt: "2026-01-01T00:00:00.000Z",
            updatedAt: "2026-01-02T00:00:00.000Z",
          },
          {
            id: "template-keep",
            courseId: "course-1",
            name: "Keep this certificate",
            description: null,
            status: "inactive",
            issuedCount: 2,
            createdAt: "2026-01-01T00:00:00.000Z",
            updatedAt: "2026-01-02T00:00:00.000Z",
          },
        ]}
      />,
    );

    expect(
      screen.getByRole("link", { name: /completion certificate/i }),
    ).toHaveAttribute(
      "href",
      "/workspace/learning/courses/course-1/certificates/template-1",
    );

    fireEvent.click(
      screen.getByRole("button", { name: /delete completion certificate/i }),
    );

    await waitFor(() => {
      expect(deleteCertificateTemplateMock).toHaveBeenCalledWith(
        "course-1",
        "template-1",
      );
    });
    expect(refreshMock).toHaveBeenCalled();
    expect(
      screen.queryByText("Completion certificate"),
    ).not.toBeInTheDocument();
    expect(screen.getByText("Keep this certificate")).toBeInTheDocument();
    expect(screen.getByText("No description")).toBeInTheDocument();
    expect(screen.getByText("inactive")).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /delete keep this certificate/i }),
    ).toBeDisabled();
    expect(
      screen.getByText("Certificate template deleted."),
    ).toBeInTheDocument();
  });

  it("shows a create failure after exposing the pending state", async () => {
    let finish:
      ((value: { success: false; error: string }) => void) | undefined;
    createCertificateTemplateMock.mockImplementation(
      () =>
        new Promise((resolve) => {
          finish = resolve;
        }),
    );
    render(<CertificateTemplateManager courseId="course-1" templates={[]} />);

    fireEvent.change(screen.getByLabelText(/^name$/i), {
      target: { value: " Failed template " },
    });
    fireEvent.change(screen.getByLabelText(/^html$/i), {
      target: { value: "<p>{{recipientName}}</p>" },
    });
    fireEvent.click(screen.getByRole("button", { name: /create template/i }));

    expect(
      await screen.findByRole("button", { name: /create template/i }),
    ).toBeDisabled();
    finish?.({ success: false, error: "The template could not be created." });

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "The template could not be created.",
    );
    expect(refreshMock).not.toHaveBeenCalled();
  });

  it("shows delete failures without removing the template", async () => {
    deleteCertificateTemplateMock.mockResolvedValue({
      success: false,
      error: "The template is still in use.",
    });
    render(
      <CertificateTemplateManager
        courseId="course-1"
        templates={[
          {
            id: "template-1",
            courseId: "course-1",
            name: "Completion certificate",
            description: "Default credential",
            status: "active",
            issuedCount: 0,
            createdAt: "2026-01-01T00:00:00.000Z",
            updatedAt: "2026-01-02T00:00:00.000Z",
          },
        ]}
      />,
    );

    fireEvent.click(
      screen.getByRole("button", { name: /delete completion certificate/i }),
    );

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "The template is still in use.",
    );
    expect(screen.getByText("Completion certificate")).toBeInTheDocument();
    expect(refreshMock).not.toHaveBeenCalled();
  });
});
