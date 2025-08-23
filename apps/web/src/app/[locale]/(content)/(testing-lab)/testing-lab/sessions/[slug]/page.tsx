import React from "react";
import {notFound} from "next/navigation";

import {SessionDetail} from "@/components/testing-lab/sessions/session-detail";
import {
  TestingSessionDetailSidebar
} from "@/app/[locale]/(content)/(testing-lab)/components/testing-session-details-sidebar";
import {SidebarInset, SidebarProvider} from "@/components/ui/sidebar";
import {getTestSessionBySlug} from "@/lib";
import {PropsWithSlugParams} from "@/types";

export default async function Page({params}: PropsWithSlugParams): Promise<React.JSX.Element> {
  const {slug} = await params;

  const session = await getTestSessionBySlug(slug);

  if (!session) notFound();

  return (
    <>
      <SessionDetail session={session}/>
      <SidebarProvider>
        {/* Collapsible Sidebar */}
        <TestingSessionDetailSidebar session={session}/>
        <SidebarInset>
          {/* Main Content Area */}
          <main>

          </main>
        </SidebarInset>
      </SidebarProvider>
      {/* Testing Requirements Sheet */}

      {/* Game Details Sheet */}

      {/* Join Session Modal */}
    </>
  );
}
