/**
 * Stub for certificate service.
 * This module is disabled in production.
 */

export async function generateCertificate(_courseId: string, _userId: string) {
    return {
        success: false,
        error: 'Certificate generation is disabled',
        certificateUrl: null
    };
}

export async function getCertificateById(_certificateId: string) {
    return { data: null, error: null };
}

export async function verifyCertificate(_certificateId: string) {
    return { valid: false, error: 'Certificate verification is disabled' };
}

export async function downloadCertificate(_certificateId: string) {
    return { url: null, error: 'Certificate download is disabled' };
}

export async function getCertificateStatus(_courseId: string, _userId: string) {
    return { eligible: false, generated: false, url: null };
}

// Export as class-like object for compatibility
export const CourseCompletionCertificateService = {
    generateCertificate,
    getCertificateById,
    verifyCertificate,
    downloadCertificate,
    getCertificateStatus,
    // Static method style
    generate: generateCertificate,
    check: getCertificateStatus,
    // Additional methods for course completion flow
    handleCourseCompletion: async (_courseId: string, _courseData: any, _studentName: string) => ({
        showCertificateNotification: false,
        eligibility: { eligible: false, reason: 'Certificates are disabled' }
    }),
};

export const certificateService = CourseCompletionCertificateService;

export default certificateService;
