// Certificate service for course completion notifications and generation
export interface CertificateEligibility {
  isEligible: boolean;
  courseId: string;
  courseTitle: string;
  completionPercentage: number;
  finalGrade?: number;
  completedAt: Date;
  requirements: {
    minimumGrade?: number;
    minimumCompletion: number;
    requiresAllActivities: boolean;
  };
  missingRequirements?: string[];
}

export interface CertificateRequest {
  courseId: string;
  courseTitle: string;
  studentName: string;
  completionDate: string;
  instructorName?: string;
  skillsLearned?: string[];
  finalGrade?: number;
}

export interface CertificateResponse {
  success: boolean;
  certificateId?: string;
  downloadUrl?: string;
  message: string;
  error?: string;
}

export class CourseCompletionCertificateService {
  /**
   * Check if a student is eligible for a certificate based on course completion
   */
  static async checkCertificateEligibility(courseId: string, studentProgress: any): Promise<CertificateEligibility> {
    // Mock certificate requirements - in real implementation, these would come from course configuration
    const requirements = {
      minimumGrade: 70,
      minimumCompletion: 100,
      requiresAllActivities: true,
    };

    const completionPercentage = this.calculateCompletionPercentage(studentProgress);
    const finalGrade = this.calculateFinalGrade(studentProgress);

    const missingRequirements: string[] = [];
    let isEligible = true;

    // Check minimum completion
    if (completionPercentage < requirements.minimumCompletion) {
      isEligible = false;
      missingRequirements.push(`Complete ${requirements.minimumCompletion}% of course content`);
    }

    // Check minimum grade
    if (requirements.minimumGrade && (!finalGrade || finalGrade < requirements.minimumGrade)) {
      isEligible = false;
      missingRequirements.push(`Achieve minimum grade of ${requirements.minimumGrade}%`);
    }

    // Check all activities completed
    if (requirements.requiresAllActivities && !this.areAllActivitiesCompleted(studentProgress)) {
      isEligible = false;
      missingRequirements.push('Complete all required activities and assignments');
    }

    return {
      isEligible,
      courseId,
      courseTitle: studentProgress.courseTitle || 'Course',
      completionPercentage,
      finalGrade,
      completedAt: new Date(),
      requirements,
      missingRequirements: missingRequirements.length > 0 ? missingRequirements : undefined,
    };
  }

  /**
   * Generate a certificate for a completed course
   */
  static async generateCertificate(request: CertificateRequest): Promise<CertificateResponse> {
    try {
      // Import the existing certificate generation action
      const { generateCertificate } = await import('@/lib/certificates/actions');

      const result = await generateCertificate(request);

      if (result.success) {
        return {
          success: true,
          certificateId: result.certificate.id,
          downloadUrl: result.certificate.downloadUrl,
          message: 'Certificate generated successfully!',
        };
      } else {
        return {
          success: false,
          message: 'Failed to generate certificate',
          error: result.message || 'Unknown error',
        };
      }
    } catch (error) {
      console.error('Certificate generation error:', error);
      return {
        success: false,
        message: 'Failed to generate certificate',
        error: error instanceof Error ? error.message : 'Unknown error',
      };
    }
  }

  /**
   * Calculate completion percentage based on student progress
   */
  private static calculateCompletionPercentage(studentProgress: any): number {
    if (!studentProgress?.modules) return 0;

    const allItems = studentProgress.modules.flatMap((module: any) => module.items || []);
    const completedItems = allItems.filter((item: any) => item.status === 'completed');

    return allItems.length > 0 ? Math.round((completedItems.length / allItems.length) * 100) : 0;
  }

  /**
   * Calculate final grade based on completed activities
   */
  private static calculateFinalGrade(studentProgress: any): number | undefined {
    if (!studentProgress?.modules) return undefined;

    const allItems = studentProgress.modules.flatMap((module: any) => module.items || []);
    const gradedItems = allItems.filter((item: any) => item.status === 'completed' && item.grade !== undefined && item.grade !== null);

    if (gradedItems.length === 0) return undefined;

    const totalGrade = gradedItems.reduce((sum: number, item: any) => sum + item.grade, 0);
    return Math.round(totalGrade / gradedItems.length);
  }

  /**
   * Check if all required activities are completed
   */
  private static areAllActivitiesCompleted(studentProgress: any): boolean {
    if (!studentProgress?.modules) return false;

    const allItems = studentProgress.modules.flatMap((module: any) => module.items || []);
    const requiredItems = allItems.filter((item: any) => item.isRequired !== false);
    const completedRequiredItems = requiredItems.filter((item: any) => item.status === 'completed');

    return requiredItems.length > 0 && completedRequiredItems.length === requiredItems.length;
  }

  /**
   * Trigger certificate notification when course is completed
   */
  static async handleCourseCompletion(courseId: string, studentProgress: any, studentName: string) {
    const eligibility = await this.checkCertificateEligibility(courseId, studentProgress);

    if (eligibility.isEligible) {
      console.log(`🎉 Certificate available for ${studentName} - Course: ${eligibility.courseTitle}`);

      // In a real implementation, this would:
      // 1. Send notification to student
      // 2. Update student's dashboard
      // 3. Trigger email notification
      // 4. Log achievement in analytics

      return {
        showCertificateNotification: true,
        eligibility,
      };
    } else {
      console.log(`Certificate not yet available for ${studentName}. Missing requirements:`, eligibility.missingRequirements);

      return {
        showCertificateNotification: false,
        eligibility,
      };
    }
  }
}
