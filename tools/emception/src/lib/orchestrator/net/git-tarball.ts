/**
 * Git tarball URL translation: convert repository URLs to downloadable tarball URLs.
 * Supports GitHub, GitLab, and Bitbucket.
 */

export interface TarballInfo {
  url: string;
  ref: string;
  host: 'github' | 'gitlab' | 'bitbucket' | 'unknown';
}

/**
 * Translate a Git repository URL + optional ref into a tarball download URL.
 *
 * Examples:
 *   github.com/user/repo         -> https://github.com/user/repo/archive/refs/heads/main.tar.gz
 *   github.com/user/repo@v1.0    -> https://github.com/user/repo/archive/refs/tags/v1.0.tar.gz
 *   gitlab.com/user/repo@main    -> https://gitlab.com/user/repo/-/archive/main/repo-main.tar.gz
 *   bitbucket.org/user/repo@main -> https://bitbucket.org/user/repo/get/main.tar.gz
 */
export function resolveGitTarball(input: string): TarballInfo {
  let url = input.trim();
  let ref = 'main';

  // Split off @ref if present
  const atIdx = url.lastIndexOf('@');
  if (atIdx > 0 && !url.substring(0, atIdx).includes('@')) {
    ref = url.substring(atIdx + 1);
    url = url.substring(0, atIdx);
  }

  // Normalize: strip protocol and trailing slashes
  url = url.replace(/^https?:\/\//, '').replace(/\.git$/, '').replace(/\/+$/, '');

  const parts = url.split('/');
  const host = parts[0]?.toLowerCase() ?? '';
  const user = parts[1] ?? '';
  const repo = parts[2] ?? '';

  if (!user || !repo) {
    return { url: input, ref, host: 'unknown' };
  }

  if (host.includes('github.com')) {
    const isTag = /^v?\d/.test(ref);
    const refPath = isTag ? `refs/tags/${ref}` : `refs/heads/${ref}`;
    return {
      url: `https://github.com/${user}/${repo}/archive/${refPath}.tar.gz`,
      ref,
      host: 'github',
    };
  }

  if (host.includes('gitlab.com') || host.includes('gitlab')) {
    return {
      url: `https://${host}/${user}/${repo}/-/archive/${ref}/${repo}-${ref}.tar.gz`,
      ref,
      host: 'gitlab',
    };
  }

  if (host.includes('bitbucket.org')) {
    return {
      url: `https://bitbucket.org/${user}/${repo}/get/${ref}.tar.gz`,
      ref,
      host: 'bitbucket',
    };
  }

  return { url: input, ref, host: 'unknown' };
}
