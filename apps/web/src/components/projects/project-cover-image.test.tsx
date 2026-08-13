import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ProjectCoverImage } from './project-cover-image';

describe('ProjectCoverImage', () => {
  it('falls back when the configured project image cannot be loaded', () => {
    render(
      <ProjectCoverImage
        src="https://cdn.gameguild.gg/projects/missing/cover.jpg"
        alt="Missing project cover"
        width={320}
        height={180}
      />,
    );

    const image = screen.getByRole('img', { name: 'Missing project cover' });
    fireEvent.error(image);

    expect(image.getAttribute('src')).toContain('images.unsplash.com');
  });
});
