import { auth } from "@/auth";
import { getMyTasks } from "@/lib/learning";
import { getMyLearningCourses } from "@/lib/learner/courses";
import { createLearnerRoutes } from "@/lib/learner/routes";
import { LearnerDashboard } from "@game-guild/courses/components/learner";

export default async function LearnerHomePage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  const session = await auth();
  const name =
    session?.user?.name?.trim() ||
    session?.user?.email?.split("@")[0] ||
    "learner";

  const [courses, tasksResult] = await Promise.all([
    getMyLearningCourses(),
    getMyTasks(),
  ]);
  const studentTasks = tasksResult.ok
    ? tasksResult.tasks.filter((task) => task.type === "do" || task.type === "review")
    : [];
  const routes = createLearnerRoutes(locale);
  const tasks =
    studentTasks.length > 0
      ? {
          doCount: studentTasks.filter((task) => task.type === "do").length,
          reviewCount: studentTasks.filter((task) => task.type === "review").length,
          href: routes.tasks,
        }
      : undefined;

  return (
    <LearnerDashboard
      learnerName={name.split(" ")[0] || name}
      courses={courses}
      routes={routes}
      tasks={tasks}
    />
  );
}
