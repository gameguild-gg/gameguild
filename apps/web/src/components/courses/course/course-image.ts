export function shouldUseUnoptimizedCourseImage(src: string): boolean {
  if (src.endsWith('.svg')) {
    return true;
  }

  try {
    const url = new URL(src);
    return url.protocol === 'http:' || url.protocol === 'https:';
  } catch {
    return false;
  }
}
