'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Badge } from '@/components/ui/badge';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, LineChart, Line, PieChart, Pie, Cell, AreaChart, Area } from 'recharts';

// Mock data for charts
const userGrowthData = [
  { month: 'Jan', users: 120, newUsers: 25 },
  { month: 'Feb', users: 145, newUsers: 35 },
  { month: 'Mar', users: 180, newUsers: 42 },
  { month: 'Apr', users: 222, newUsers: 38 },
  { month: 'May', users: 260, newUsers: 45 },
  { month: 'Jun', users: 305, newUsers: 52 },
];

const revenueData = [
  { month: 'Jan', revenue: 12400, courses: 8900, subscriptions: 3500 },
  { month: 'Feb', revenue: 15600, courses: 11200, subscriptions: 4400 },
  { month: 'Mar', revenue: 18900, courses: 13500, subscriptions: 5400 },
  { month: 'Apr', revenue: 22100, courses: 15800, subscriptions: 6300 },
  { month: 'May', revenue: 25800, courses: 18200, subscriptions: 7600 },
  { month: 'Jun', revenue: 29400, courses: 20600, subscriptions: 8800 },
];

const courseCompletionData = [
  { name: 'Completed', value: 68, color: '#10b981' },
  { name: 'In Progress', value: 24, color: '#f59e0b' },
  { name: 'Not Started', value: 8, color: '#ef4444' },
];

const topCoursesData = [
  { name: 'React Fundamentals', enrollments: 245, revenue: 12250 },
  { name: 'JavaScript Mastery', enrollments: 198, revenue: 9900 },
  { name: 'Node.js Advanced', enrollments: 167, revenue: 16700 },
  { name: 'Python Bootcamp', enrollments: 143, revenue: 7150 },
  { name: 'Vue.js Complete', enrollments: 121, revenue: 6050 },
];

const userEngagementData = [
  { day: 'Mon', activeUsers: 890, pageViews: 2340 },
  { day: 'Tue', activeUsers: 945, pageViews: 2580 },
  { day: 'Wed', activeUsers: 1120, pageViews: 3010 },
  { day: 'Thu', activeUsers: 1050, pageViews: 2890 },
  { day: 'Fri', activeUsers: 980, pageViews: 2650 },
  { day: 'Sat', activeUsers: 670, pageViews: 1820 },
  { day: 'Sun', activeUsers: 620, pageViews: 1650 },
];

export function DashboardCharts() {
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Simulate loading time
    const timer = setTimeout(() => {
      setIsLoading(false);
    }, 1500);

    return () => clearTimeout(timer);
  }, []);

  if (isLoading) {
    return (
      <div className="grid gap-6 md:grid-cols-2">
        {[...Array(4)].map((_, i) => (
          <Card key={i}>
            <CardHeader>
              <div className="h-6 bg-muted rounded animate-pulse" />
            </CardHeader>
            <CardContent>
              <div className="h-64 bg-muted rounded animate-pulse" />
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  return (
    <Tabs defaultValue="overview" className="space-y-6">
      <TabsList className="grid w-full grid-cols-4">
        <TabsTrigger value="overview">Overview</TabsTrigger>
        <TabsTrigger value="users">Users</TabsTrigger>
        <TabsTrigger value="revenue">Revenue</TabsTrigger>
        <TabsTrigger value="courses">Courses</TabsTrigger>
      </TabsList>

      <TabsContent value="overview" className="space-y-6">
        <div className="grid gap-6 md:grid-cols-2">
          {/* User Growth */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-base font-medium">User Growth</CardTitle>
              <Badge variant="secondary">+15% this month</Badge>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <AreaChart data={userGrowthData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="month" />
                  <YAxis />
                  <Tooltip />
                  <Area type="monotone" dataKey="users" stroke="#3b82f6" fill="#3b82f6" fillOpacity={0.1} />
                  <Area type="monotone" dataKey="newUsers" stroke="#10b981" fill="#10b981" fillOpacity={0.3} />
                </AreaChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* Course Completion */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base font-medium">Course Completion Rate</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={courseCompletionData}
                    cx="50%"
                    cy="50%"
                    outerRadius={80}
                    fill="#8884d8"
                    dataKey="value"
                    label={({ name, value }) => `${name}: ${value}%`}
                  >
                    {courseCompletionData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={entry.color} />
                    ))}
                  </Pie>
                  <Tooltip />
                </PieChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </div>
      </TabsContent>

      <TabsContent value="users" className="space-y-6">
        <div className="grid gap-6 md:grid-cols-2">
          {/* User Engagement */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base font-medium">Weekly User Engagement</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={userEngagementData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="day" />
                  <YAxis />
                  <Tooltip />
                  <Bar dataKey="activeUsers" fill="#3b82f6" name="Active Users" />
                  <Bar dataKey="pageViews" fill="#10b981" name="Page Views" />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* User Growth Trend */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base font-medium">User Growth Trend</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={userGrowthData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="month" />
                  <YAxis />
                  <Tooltip />
                  <Line type="monotone" dataKey="users" stroke="#3b82f6" strokeWidth={2} name="Total Users" />
                  <Line type="monotone" dataKey="newUsers" stroke="#10b981" strokeWidth={2} name="New Users" />
                </LineChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </div>
      </TabsContent>

      <TabsContent value="revenue" className="space-y-6">
        <div className="grid gap-6 md:grid-cols-2">
          {/* Revenue Breakdown */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base font-medium">Revenue Breakdown</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={revenueData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="month" />
                  <YAxis />
                  <Tooltip formatter={(value) => [`$${value}`, '']} />
                  <Bar dataKey="courses" stackId="a" fill="#3b82f6" name="Courses" />
                  <Bar dataKey="subscriptions" stackId="a" fill="#10b981" name="Subscriptions" />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* Revenue Trend */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base font-medium">Total Revenue Trend</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <AreaChart data={revenueData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="month" />
                  <YAxis />
                  <Tooltip formatter={(value) => [`$${value}`, '']} />
                  <Area type="monotone" dataKey="revenue" stroke="#3b82f6" fill="#3b82f6" fillOpacity={0.1} />
                </AreaChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </div>
      </TabsContent>

      <TabsContent value="courses" className="space-y-6">
        <div className="grid gap-6 md:grid-cols-2">
          {/* Top Courses */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base font-medium">Top Performing Courses</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={topCoursesData} layout="horizontal">
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis type="number" />
                  <YAxis dataKey="name" type="category" width={100} />
                  <Tooltip />
                  <Bar dataKey="enrollments" fill="#3b82f6" name="Enrollments" />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          {/* Course Revenue */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base font-medium">Course Revenue</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={topCoursesData}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" angle={-45} textAnchor="end" height={80} />
                  <YAxis />
                  <Tooltip formatter={(value) => [`$${value}`, '']} />
                  <Bar dataKey="revenue" fill="#10b981" name="Revenue" />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </div>
      </TabsContent>
    </Tabs>
  );
}
