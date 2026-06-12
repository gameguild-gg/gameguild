import React from 'react';
import { getCourse, getCourseRevenueAnalytics } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CreditCard, DollarSign, Receipt, TrendingUp } from 'lucide-react';

/**
 * Revenue Analytics Page
 *
 * Route: /courses/[course]/analytics/revenue
 * Condition: course.features.hasPricing = true
 */
export default async function RevenueAnalyticsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/analytics/revenue'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const course = await getCourse(courseId);
  if (!course) return <div className="text-muted-foreground p-6">Course not found.</div>;

  const revenue = await getCourseRevenueAnalytics(courseId);

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 md:grid-cols-4">
        <Card><CardContent className="flex items-center gap-3 p-4"><DollarSign className="size-5 text-emerald-600" /><div><p className="text-2xl font-semibold">{revenue.currency} {revenue.totalRevenue.toFixed(2)}</p><p className="text-sm text-muted-foreground">Total revenue</p></div></CardContent></Card>
        <Card><CardContent className="flex items-center gap-3 p-4"><Receipt className="size-5 text-blue-600" /><div><p className="text-2xl font-semibold">{revenue.totalTransactions}</p><p className="text-sm text-muted-foreground">Transactions</p></div></CardContent></Card>
        <Card><CardContent className="flex items-center gap-3 p-4"><CreditCard className="size-5 text-purple-600" /><div><p className="text-2xl font-semibold">{revenue.currency} {revenue.avgTransactionValue.toFixed(2)}</p><p className="text-sm text-muted-foreground">Average order</p></div></CardContent></Card>
        <Card><CardContent className="flex items-center gap-3 p-4"><TrendingUp className="size-5 text-amber-600" /><div><p className="text-2xl font-semibold">{revenue.refundRate}%</p><p className="text-sm text-muted-foreground">Refund rate</p></div></CardContent></Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Revenue Sources</CardTitle>
          <CardDescription>{course.title} revenue by tier and acquisition source.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          {revenue.revenueByTier.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No revenue has been recorded for this course.</div>
          ) : (
            revenue.revenueByTier.map((tier) => (
              <div key={tier.tierId} className="flex items-center justify-between rounded-lg border p-4">
                <div><p className="font-medium">{tier.tierName}</p><p className="text-sm text-muted-foreground">{tier.count} purchases</p></div>
                <Badge>{revenue.currency} {tier.revenue.toFixed(2)}</Badge>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
