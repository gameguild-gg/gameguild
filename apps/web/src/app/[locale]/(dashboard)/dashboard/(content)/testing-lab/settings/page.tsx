import { redirect } from 'next/navigation';


export default async function Page(): Promise<void> {
  redirect('/dashboard/testing-lab/settings/general');
}
