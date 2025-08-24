import React, {PropsWithChildren} from "react";

export default async function Layout({children}: PropsWithChildren): Promise<React.JSX.Element> {
  // TODO Fetch all testing lab sessions here using API Key and provide then into a context.

  return (
    <>
      {children}
    </>
  );
}
