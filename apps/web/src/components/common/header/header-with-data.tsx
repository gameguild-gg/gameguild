import { auth } from '@/auth';
import { getCurrentUserProfile } from '@/lib/user-profile/user-profile.actions';
import Header from './default-header';
import type { Session } from 'next-auth';
import type { UserResponseDto } from '@/lib/api/generated/types.gen';
import React, { PropsWithChildren } from 'react';

type Props = PropsWithChildren<React.HTMLAttributes<HTMLDivElement>>;

const HeaderWithData: React.FunctionComponent<Readonly<Props>> = async ({ ...props }) => {
  const session = await auth();
  let userProfile: UserResponseDto | null = null;
  
  if (session?.user?.id) {
    const userProfileResult = await getCurrentUserProfile();
    userProfile = userProfileResult.success ? (userProfileResult.data ?? null) : null;
  }

  return <Header session={session} userProfile={userProfile} {...props} />;
};

export default HeaderWithData;