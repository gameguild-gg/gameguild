import { CODING_WORKSPACE_ROOT, MAX_CODING_FILE_PATH_LENGTH } from './contracts';

export function normalizeCodingPath(value: string): string {
  if (typeof value !== 'string') throw new TypeError('Coding path must be a string.');
  if (value !== value.trim()) throw new TypeError('Coding path cannot have surrounding whitespace.');
  const path = value;
  let relative: string;
  if (path.startsWith(`${CODING_WORKSPACE_ROOT}/`)) {
    relative = path.slice(CODING_WORKSPACE_ROOT.length + 1);
  } else if (path.startsWith('/user/')) {
    relative = path.slice('/user/'.length);
  } else if (path.startsWith('/')) {
    throw new TypeError('Coding path must be relative to the learner workspace.');
  } else {
    relative = path;
  }

  const segments = relative.split('/');
  if (
    relative.length === 0 ||
    relative.length > MAX_CODING_FILE_PATH_LENGTH ||
    relative.includes('\\') ||
    relative.includes(':') ||
    [...relative].some((character) => character.charCodeAt(0) < 32) ||
    segments.some((segment) => segment === '' || segment === '.' || segment === '..') ||
    segments[0] === '.gameguild'
  ) {
    throw new TypeError('Coding path is invalid or reserved.');
  }

  return `${CODING_WORKSPACE_ROOT}/${relative}`;
}
