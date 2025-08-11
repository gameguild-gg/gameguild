'use client';

import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';

import { createPaymentIntent, enrollInFreeCourse, type EnrollmentStatus, getCourseEnrollmentStatus, getProductsContainingCourse, type Product } from '@/lib/courses/actions/enrollment.actions';
import { BookOpen, CreditCard, Gift, Loader2, Lock, Star, Trophy, Users } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';

interface CourseAccessCardProps {
  readonly courseSlug: string;
  readonly courseTitle: string;
}

// Extracted sub-components for better modularity
function LoadingCard() {
  return (
    <Card className="bg-gray-800 border-gray-700">
      <CardHeader>
        <CardTitle className="text-xl text-center">Loading...</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="flex justify-center">
          <Loader2 className="h-6 w-6 animate-spin" />
        </div>
      </CardContent>
    </Card>
  );
}

function ErrorCard({ error }: { readonly error: string }) {
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

function EnrolledCard({ enrollment, courseSlug }: { readonly enrollment: EnrollmentStatus; readonly courseSlug: string }) {
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
            <Button asChild variant="outline" className="w-full border-green-600 text-green-400 hover:bg-green-950">
              <Link href={`/course/${courseSlug}/content`}>Continue Learning</Link>
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}

function ProductCard({
  product,
  onFreeCourseEnroll,
  onPaidCoursePayment,
  isEnrolling,
  isPaymentLoading,
}: {
  readonly product: Product;
  readonly onFreeCourseEnroll: () => void;
  readonly onPaidCoursePayment: (productId: string) => void;
  readonly isEnrolling: boolean;
  readonly isPaymentLoading: boolean;
}) {
  const isFree = product.price === 0;

  return (
    <Card className="bg-gray-800 border-gray-700">
      <CardHeader>
        <CardTitle className="text-xl text-center">{isFree ? 'Free Course' : 'Purchase Course'}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="text-center">
          {isFree ? (
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
          {isFree ? (
            <Button onClick={onFreeCourseEnroll} disabled={isEnrolling} className="w-full bg-green-600 hover:bg-green-700">
              {isEnrolling ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
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
            <Button onClick={() => onPaidCoursePayment(product.id)} disabled={isPaymentLoading} className="w-full bg-blue-600 hover:bg-blue-700">
              {isPaymentLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
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

        {!isFree && (
          <div className="text-xs text-gray-400 text-center pt-2">
            <Lock className="inline h-3 w-3 mr-1" />
            Secure payment • 30-day money-back guarantee
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function CourseStatsCard() {
  const stats = useMemo(
    () => [
      { icon: Users, label: 'Students', value: '1,247', color: 'text-blue-400' },
      { icon: Star, label: 'Rating', value: '4.8', color: 'text-yellow-400' },
      { icon: Trophy, label: 'Completion Rate', value: '89%', color: 'text-green-400' },
    ],
    [],
  );

  return (
    <Card className="bg-gray-800 border-gray-700">
      <CardHeader>
        <CardTitle className="text-lg">Course Stats</CardTitle>
      </CardHeader>
      <CardContent className="space-y-3">
        {stats.map(({ icon: Icon, label, value, color }) => (
          <div key={label} className="flex items-center justify-between">
            <div className="flex items-center">
              <Icon className={`mr-2 h-4 w-4 ${color}`} />
              <span className="text-sm">{label}</span>
            </div>
            <span className="text-sm font-medium">{value}</span>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}

export default function CourseAccessCard({ courseSlug }: Pick<CourseAccessCardProps, 'courseSlug'>) {
  const [enrollment, setEnrollment] = useState<EnrollmentStatus | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(true);
  const [enrolling, setEnrolling] = useState(false);
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();

  const loadEnrollmentData = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);

      const [enrollmentStatus, availableProducts] = await Promise.all([getCourseEnrollmentStatus(courseSlug), getProductsContainingCourse(courseSlug)]);

      setEnrollment(enrollmentStatus);
      setProducts(availableProducts);
    } catch (err) {
      console.error('Error loading enrollment data:', err);
      setError('Failed to load course access information');
    } finally {
      setLoading(false);
    }
  }, [courseSlug]);

  useEffect(() => {
    loadEnrollmentData();
  }, [loadEnrollmentData]);

  const handleFreeCourseEnrollment = useCallback(async () => {
    try {
      setEnrolling(true);
      setError(null);

      const result = await enrollInFreeCourse(courseSlug);

      if (result.success) {
        const updatedStatus = await getCourseEnrollmentStatus(courseSlug);
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
  }, [courseSlug]);

  const handlePaidCoursePayment = useCallback(
    async (productId: string) => {
      try {
        setPaymentLoading(true);
        setError(null);

        const result = await createPaymentIntent(productId);

        if (result.success && result.paymentUrl) {
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
    },
    [router],
  );

  if (loading) return <LoadingCard />;
  if (error) return <ErrorCard error={error} />;
  if (!enrollment) return null;

  if (enrollment.isEnrolled) {
    return <EnrolledCard enrollment={enrollment} courseSlug={courseSlug} />;
  }

  return (
    <div className="space-y-4">
      {products.map((product) => (
        <ProductCard key={product.id} product={product} onFreeCourseEnroll={handleFreeCourseEnrollment} onPaidCoursePayment={handlePaidCoursePayment} isEnrolling={enrolling} isPaymentLoading={paymentLoading} />
      ))}
      <CourseStatsCard />
    </div>
  );
}
