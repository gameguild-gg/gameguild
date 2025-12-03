'use server';

/**
 * Stub implementations for certificate actions.
 * This module is disabled in production.
 */

export async function generateCertificate(_courseId: string, _userId: string) {
  return { 
    success: false, 
    error: 'Certificate generation is disabled',
    certificateUrl: null 
  };
}

export async function getCertificate(_certificateId: string) {
  return { data: null, error: null };
}

export async function getUserCertificates(_userId: string) {
  return { data: [], error: null };
}

export async function verifyCertificate(_certificateId: string) {
  return { valid: false, error: 'Certificate verification is disabled' };
}
