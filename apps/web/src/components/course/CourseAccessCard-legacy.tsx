'use client';

import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import {
  getCourseEnrollmentStatus,
  enrollInFreeCourse,
  createPaymentIntent,
  getProductsContainingCourse,
  EnrollmentStatus,
  Product,
} from '@/actions/enrollment';
import { Users, Trophy, Star, Lock, CreditCard, Gift, BookOpen } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';

interface CourseAccessCardProps {
  courseId: string;
  courseTitle: string;
  courseSlug: string;
}

export default function CourseAccessCard({ courseId, courseTitle, courseSlug }: CourseAccessCardProps) {
  const [enrollment, setEnrollment] = useState<EnrollmentStatus | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [enrolling, setEnrolling] = useState(false);
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();

  useEffect(() => {
    const loadEnrollmentData = async () => {
      try {
        setLoading(true);
        setError(null);

        const [enrollmentStatus, availableProducts] = await Promise.all([getCourseEnrollmentStatus(courseId), getProductsContainingCourse(courseId)]);

        setEnrollment(enrollmentStatus);
        setProducts(availableProducts);
      } catch (err) {
        console.error('Error loading enrollment data:', err);
        setError('Failed to load course access information');
      } finally {
        setLoading(false);
      }
    };

    loadEnrollmentData();
  }, [courseId]);

  const handleFreeCourseEnrollment = async () => {
    try {
      setEnrolling(true);
      setError(null);

      const result = await enrollInFreeCourse(courseId);

      if (result.success) {
        // Refresh enrollment status
        const updatedStatus = await getCourseEnrollmentStatus(courseId);
        setEnrollment(updatedStatus);
      } else {
        setError(result.message);
      }
    } catch (err) {
      console.error('Error enrolling in free course:', err);
      setError('Failed to enroll in course');
    } finally {
      setEnrolling(false);
    }
  };

  const handlePaidCoursePayment = async (productId: string) => {
    try {
      setPaymentLoading(true);
      setError(null);

      const result = await createPaymentIntent(productId);

      if (result.success && result.paymentUrl) {
        // Redirect to payment page
        router.push(result.paymentUrl);
      } else {
        setError(result.message);
      }
    } catch (err) {
      console.error('Error creating payment intent:', err);
      setError('Failed to initiate payment');
    } finally {
      setPaymentLoading(false);
    }
  };

  if (loading) {
    return (
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-xl text-center">Loading...</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex justify-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500"></div>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-xl text-center text-red-400">Error</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-red-300 text-center">{error}</p>
        </CardContent>
      </Card>
    );
  }

  if (!enrollment) {
    return null;
  }

  // User is already enrolled
  if (enrollment.isEnrolled) {
    return (
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-xl text-center">Your Progress</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {enrollment.progress !== undefined && (
            <div>
              <div className="flex justify-between items-center mb-2">
                <span className="text-sm font-medium">Progress</span>
                <span className="text-sm text-gray-400">{enrollment.progress}%</span>
              </div>
              <Progress value={enrollment.progress} className="mb-4" />
            </div>
          )}

          <div className="space-y-3 text-sm">
            <div className="flex justify-between">
              <span className="text-gray-400">Status:</span>
              <Badge className="bg-green-500/10 border-green-500 text-green-400">Enrolled</Badge>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-400">Access:</span>
              <span className="text-green-400">Lifetime</span>
            </div>
            {enrollment.enrollmentDate && (
              <div className="flex justify-between">
                <span className="text-gray-400">Enrolled:</span>
                <span>{new Date(enrollment.enrollmentDate).toLocaleDateString()}</span>
              </div>
            )}
          </div>

          <div className="space-y-2 pt-4">
            <Button asChild className="w-full bg-blue-600 hover:bg-blue-700">
              <Link href={`/course/${courseSlug}/content`}>
                <BookOpen className="mr-2 h-4 w-4" />
                Access Course Content
              </Link>
            </Button>
            {enrollment.progress !== undefined && enrollment.progress > 0 && (
              <Button asChild className="w-full bg-green-600 hover:bg-green-700">
                <Link href={`/course/${courseSlug}/content`}>Continue Learning</Link>
              </Button>
            )}
          </div>
        </CardContent>
      </Card>
    );
  }

  // User is not enrolled - show enrollment options
  return (
    <div className="space-y-4">
      {products.map((product) => (
        <Card key={product.id} className="bg-gray-800 border-gray-700">
          <CardHeader>
            <CardTitle className="text-xl text-center">{product.price === 0 ? 'Free Course' : 'Purchase Course'}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="text-center">
              {product.price === 0 ? (
                <div className="space-y-2">
                  <Badge className="bg-green-500/10 border-green-500 text-green-400 text-lg px-3 py-1">
                    <Gift className="mr-1 h-4 w-4" />
                    FREE
                  </Badge>
                  <p className="text-gray-300 text-sm">Get instant access to all course materials</p>
                </div>
              ) : (
                <div className="space-y-2">
                  <div className="text-3xl font-bold text-white">${product.price}</div>
                  <p className="text-gray-300 text-sm">One-time payment • Lifetime access</p>
                </div>
              )}
            </div>

            <div className="space-y-3 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-400">Access:</span>
                <span>Lifetime</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Type:</span>
                <span className="capitalize">{product.type}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Courses:</span>
                <span>
                  {product.courses.length} course{product.courses.length > 1 ? 's' : ''}
                </span>
              </div>
            </div>

            <div className="space-y-2 pt-4">
              {product.price === 0 ? (
                <Button onClick={handleFreeCourseEnrollment} disabled={enrolling} className="w-full bg-green-600 hover:bg-green-700">
                  {enrolling ? (
                    <>
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                      Enrolling...
                    </>
                  ) : (
                    <>
                      <Gift className="mr-2 h-4 w-4" />
                      Get Free Access
                    </>
                  )}
                </Button>
              ) : (
                <Button onClick={() => handlePaidCoursePayment(product.id)} disabled={paymentLoading} className="w-full bg-blue-600 hover:bg-blue-700">
                  {paymentLoading ? (
                    <>
                      <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                      Processing...
                    </>
                  ) : (
                    <>
                      <CreditCard className="mr-2 h-4 w-4" />
                      Purchase Course
                    </>
                  )}
                </Button>
              )}
            </div>

            {product.price > 0 && (
              <div className="text-xs text-gray-400 text-center pt-2">
                <Lock className="inline h-3 w-3 mr-1" />
                Secure payment • 30-day money-back guarantee
              </div>
            )}
          </CardContent>
        </Card>
      ))}

      {/* Course Stats */}
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-lg">Course Stats</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <Users className="mr-2 h-4 w-4 text-blue-400" />
              <span className="text-sm">Students</span>
            </div>
            <span className="text-sm font-medium">1,247</span>
          </div>
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <Star className="mr-2 h-4 w-4 text-yellow-400" />
              <span className="text-sm">Rating</span>
            </div>
            <div className="flex items-center">
              <span className="text-sm font-medium mr-1">4.8</span>
              <div className="flex">
                {[...Array(5)].map((_, i) => (
                  <Star key={i} className="h-3 w-3 fill-yellow-400 text-yellow-400" />
                ))}
              </div>
            </div>
          </div>
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <Trophy className="mr-2 h-4 w-4 text-green-400" />
              <span className="text-sm">Completion Rate</span>
            </div>
            <span className="text-sm font-medium">89%</span>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
