import React from "react";
import {notFound} from 'next/navigation';
import {SessionDetail} from "@/components/testing-lab/sessions/session-detail";
import {getTestSessionBySlug} from "@/lib";

interface Props {
  params: Promise<{
    slug: string;
  }>;
}

export default async function Page({params}: Props): Promise<React.JSX.Element> {
  const { slug } = await params;

    const session = await getTestSessionBySlug(slug);

    if (!session) notFound();

    return <SessionDetail session={session}/>;
}
