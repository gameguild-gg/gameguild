import { SignInForm } from '@/components/sign-in-form';

export default async function SignInPage({
    searchParams,
}: {
    searchParams: Promise<{ redirectTo?: string }>;
}) {
    const { redirectTo } = await searchParams;

    return <SignInForm redirectTo={redirectTo || '/'} />;
}