import React from "react";
import {JoinProcessModal} from "@/components/testing-lab/join/join-process-modal";

export default async function Page(): Promise<React.JSX.Element> {
  return (
      <>
          <JoinProcessModal/>
      </>
  );
}
