import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';

import LicensesPage from './[locale]/(legal)/licenses/page';
import FerpaWaiverPage from './[locale]/(legal)/ferpa-waiver/page';
import AcademicHonestyPage from './[locale]/(legal)/academic-honesty/page';
import RoadmapPage from './[locale]/(institutional)/about/(project)/roadmap/page';
import ContributorsPage from './[locale]/(institutional)/about/(project)/contributors/page';

describe('static legal and project pages', () => {
  it('renders real license guidance instead of a reconstruction placeholder', async () => {
    render(await LicensesPage());

    expect(screen.getByRole('heading', { name: /licenses/i })).toBeInTheDocument();
    expect(screen.getAllByText(/third-party packages/i).length).toBeGreaterThan(0);
    expect(screen.queryByText(/under reconstruction/i)).not.toBeInTheDocument();
  });

  it('renders FERPA waiver content with consent and revocation guidance', async () => {
    render(await FerpaWaiverPage({} as PageProps<'/[locale]/ferpa-waiver'>));

    expect(screen.getByRole('heading', { name: /ferpa waiver/i })).toBeInTheDocument();
    expect(screen.getAllByText(/education records/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/revoke/i).length).toBeGreaterThan(0);
    expect(screen.queryByText(/under reconstruction/i)).not.toBeInTheDocument();
  });

  it('renders academic honesty policy content', async () => {
    render(await AcademicHonestyPage({} as PageProps<'/[locale]/academic-honesty'>));

    expect(screen.getByRole('heading', { name: /academic honesty/i })).toBeInTheDocument();
    expect(screen.getAllByText(/plagiarism/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/assessment/i).length).toBeGreaterThan(0);
    expect(screen.queryByText(/under reconstruction/i)).not.toBeInTheDocument();
  });

  it('renders the roadmap with day-zero product tracks', async () => {
    render(await RoadmapPage({} as PageProps<'/[locale]/about/roadmap'>));

    expect(screen.getByRole('heading', { name: /development roadmap/i })).toBeInTheDocument();
    expect(screen.getByText(/learning platform/i)).toBeInTheDocument();
    expect(screen.getByText(/testing lab/i)).toBeInTheDocument();
    expect(screen.getByText(/launch pad/i)).toBeInTheDocument();
    expect(screen.queryByText(/under reconstruction/i)).not.toBeInTheDocument();
  });

  it('renders contributor governance and project ownership content', async () => {
    render(await ContributorsPage({} as PageProps<'/[locale]/about/contributors'>));

    expect(screen.getByRole('heading', { level: 1, name: /contributors/i })).toBeInTheDocument();
    expect(screen.getAllByText(/maintainers/i).length).toBeGreaterThan(0);
    expect(screen.getAllByText(/review standards/i).length).toBeGreaterThan(0);
    expect(screen.queryByText(/under reconstruction/i)).not.toBeInTheDocument();
  });
});
