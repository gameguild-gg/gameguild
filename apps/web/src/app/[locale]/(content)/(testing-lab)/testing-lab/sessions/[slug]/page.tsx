import React from "react";
import {notFound} from 'next/navigation';
import {SessionDetail} from "@/components/testing-lab/sessions/session-detail";
import {getTestSessionBySlug} from "@/lib";
import {PropsWithSlugParams} from "@/types";

export default async function Page({params}: PropsWithSlugParams): Promise<React.JSX.Element> {
  const {slug} = await params;

  const session = await getTestSessionBySlug(slug);

  if (!session) notFound();

  return (
    <>
      <SessionDetail session={session}/>
    </>
  );
}
