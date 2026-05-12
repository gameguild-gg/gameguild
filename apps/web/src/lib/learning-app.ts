const DEFAULT_LEARNING_APP_URL = 'http://localhost:3002';

function normalizeBaseUrl(url: string): string {
    return url.replace(/\/$/, '');
}

export function getLearningAppBaseUrl(): string {
    return normalizeBaseUrl(process.env.NEXT_PUBLIC_LEARNING_APP_URL || DEFAULT_LEARNING_APP_URL);
}

export function getLearningAppCourseUrl(courseSlug: string): string {
    return `${getLearningAppBaseUrl()}/courses/${encodeURIComponent(courseSlug)}`;
}

export function getLearningAppCourseContentUrl(courseSlug: string): string {
    return `${getLearningAppCourseUrl(courseSlug)}/content`;
}