import { describe, it, expect, jest, beforeEach } from '@jest/globals';
import { getCourseEnrollmentStatus, getProductsContainingCourse, enrollInFreeCourse } from '@/lib/courses/actions/enrollment.actions';

// Mock the auth module
jest.mock('@/auth', () => ({
  auth: jest.fn(),
}));

const mockAuth = require('@/auth').auth as jest.MockedFunction<any>;

describe('Enrollment Actions', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('getCourseEnrollmentStatus', () => {
    it('returns not enrolled for unauthenticated users', async () => {
      mockAuth.mockResolvedValue(null);

      const result = await getCourseEnrollmentStatus('test-course');

      expect(result).toEqual({
        isEnrolled: false,
        isFree: false,
      });
    });

    it('returns default enrollment status for unknown courses', async () => {
      mockAuth.mockResolvedValue({ user: { id: 'user1' } });

      const result = await getCourseEnrollmentStatus('unknown-course');

      expect(result).toEqual({
        isEnrolled: false,
        isFree: true,
        price: 0,
        productId: 'prod-unknown-course',
      });
    });

    it('returns correct enrollment status for known courses', async () => {
      mockAuth.mockResolvedValue({ user: { id: 'user1' } });

      const result = await getCourseEnrollmentStatus('game-dev-portfolio');

      expect(result).toMatchObject({
        isFree: true,
        price: 0,
      });
    });
  });

  describe('getProductsContainingCourse', () => {
    it('returns default product for unknown courses', async () => {
      const result = await getProductsContainingCourse('unknown-course');

      expect(result).toHaveLength(1);
      expect(result[0]).toMatchObject({
        id: 'prod-unknown-course',
        title: 'Course: Unknown Course',
        price: 0,
        courses: ['unknown-course'],
        type: 'course',
      });
    });

    it('returns existing products for known courses', async () => {
      const result = await getProductsContainingCourse('game-dev-portfolio');

      expect(result).toHaveLength(1);
      expect(result[0]).toMatchObject({
        id: 'prod-game-dev-1',
        title: 'Game Development Portfolio Course',
        price: 0,
        courses: ['game-dev-portfolio'],
        type: 'course',
      });
    });
  });

  describe('enrollInFreeCourse', () => {
    it('requires authentication', async () => {
      mockAuth.mockResolvedValue(null);

      const result = await enrollInFreeCourse('test-course');

      expect(result).toEqual({
        success: false,
        message: 'Authentication required',
      });
    });

    it('enrolls user in unknown free courses', async () => {
      mockAuth.mockResolvedValue({ user: { id: 'user1' } });

      const result = await enrollInFreeCourse('unknown-course');

      expect(result).toEqual({
        success: true,
        message: 'Successfully enrolled in course',
      });
    });

    it('enrolls user in known free courses', async () => {
      mockAuth.mockResolvedValue({ user: { id: 'user1' } });

      const result = await enrollInFreeCourse('game-dev-portfolio');

      expect(result).toEqual({
        success: true,
        message: 'Successfully enrolled in course',
      });
    });

    it('rejects enrollment in paid courses', async () => {
      mockAuth.mockResolvedValue({ user: { id: 'user1' } });

      const result = await enrollInFreeCourse('game-jam-survival');

      expect(result).toEqual({
        success: false,
        message: 'This course requires payment',
      });
    });
  });
});
