import {TestingSession} from "@/lib";
import React from "react";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem
} from "@/components/ui/sidebar";
import {Collapsible, CollapsibleContent, CollapsibleTrigger} from "@/components/ui/collapsible";
import {GamepadIcon, LucideIcon, Target, Users} from "lucide-react";

const getSessionTypeIcon = (type: string): LucideIcon | null => {
  switch (type) {
    case 'gameplay':
      return GamepadIcon;
    case 'usability':
      return Users;
    case 'bug-hunting':
      return Target;
    default:
      return null;
  }
};

interface TestingSessionDetailSidebarProps {
  session: TestingSession;
}

export const TestingSessionDetailSidebar = ({session}: TestingSessionDetailSidebarProps): React.JSX.Element => {

  const data = {
    title: session.sessionName,
    icon: getSessionTypeIcon(session.type),
  };

  return (
    <>
      <Sidebar collapsible="icon">
        <SidebarHeader>

        </SidebarHeader>
        <SidebarContent>
          <SidebarGroup>
            <SidebarGroupLabel>

            </SidebarGroupLabel>
            <SidebarMenu>
              <Collapsible asChild>
                <SidebarMenuItem>
                  <CollapsibleTrigger asChild>
                    <SidebarMenuButton tooltip={data.title}>
                      {data.icon && <data.icon className="size-6"/>}
                      <span>Session Info</span>
                    </SidebarMenuButton>
                  </CollapsibleTrigger>
                  <CollapsibleContent>
                    <>
                      <div className="">

                      </div>
                    </>
                  </CollapsibleContent>
                </SidebarMenuItem>
              </Collapsible>
            </SidebarMenu>

          </SidebarGroup>
        </SidebarContent>
        <SidebarFooter>

        </SidebarFooter>
      </Sidebar>
    </>
  );
}