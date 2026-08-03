import { auth } from "@/auth";
import { getMyLearningCourses } from "@/lib/learner/courses";
import { LearnerDashboard } from "@game-guild/courses/components/learner";

export default async function LearnerHomePage() {
  const session = await auth();
  const name =
    session?.user?.name?.trim() ||
    session?.user?.email?.split("@")[0] ||
    "learner";

  return (
    <LearnerDashboard
      learnerName={name.split(" ")[0] || name}
      courses={await getMyLearningCourses()}
    />
  );
}
