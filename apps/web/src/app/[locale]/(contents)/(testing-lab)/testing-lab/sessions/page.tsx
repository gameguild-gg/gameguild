import { redirect } from "next/navigation";

export default async function TestingLabSessionsPage() {
  redirect("/testing-lab/events");
}
