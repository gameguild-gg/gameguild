import React, {PropsWithChildren} from "react";
import {Footer} from '@/components/common/footer/default-footer';
import {Header} from '@/components/common/header';

export default async function Layout({children}: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <>
      <div className="flex flex-col flex-1 bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
        <Header/>
        <div className="flex flex-col flex-1">
          {children}
        </div>
        <Footer/>
      </div>
    </>
  );
}
