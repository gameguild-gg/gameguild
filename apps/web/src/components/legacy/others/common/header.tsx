'use client';
import { User } from 'lucide-react';

function Header() {
  return (
    <header className="bg-[#18181c] text-white h-16 px-12 py-0.5">
      <div className="flex items-center justify-between h-full w-full">
        <div className="flex min-w-[150px] items-center">
          <img className="w-[135px] h-[46px] m-1.5" src="/assets/images/logo-text.png" alt="Logo" />
          &nbsp;
          {/*<a href="/">*/}
          {/*  <span*/}
          {/*    style={{*/}
          {/*      paddingLeft: '15px',*/}
          {/*      paddingRight: '15px',*/}
          {/*      color: '#ffffff',*/}
          {/*    }}*/}
          {/*  >*/}
          {/*    About*/}
          {/*  </span>*/}
          {/*</a>*/}
          {/*<a href="/">*/}
          {/*  <span*/}
          {/*    style={{*/}
          {/*      paddingLeft: '15px',*/}
          {/*      paddingRight: '15px',*/}
          {/*      color: '#ffffff',*/}
          {/*    }}*/}
          {/*  >*/}
          {/*    Courses*/}
          {/*  </span>*/}
          {/*</a>*/}
          {/*<a href="/">*/}
          {/*  <span*/}
          {/*    style={{*/}
          {/*      paddingLeft: '15px',*/}
          {/*      paddingRight: '15px',*/}
          {/*      color: '#ffffff',*/}
          {/*    }}*/}
          {/*  >*/}
          {/*    Jobs*/}
          {/*  </span>*/}
          {/*</a>*/}
          {/*<a href="/">*/}
          {/*  <span*/}
          {/*    style={{*/}
          {/*      paddingLeft: '15px',*/}
          {/*      paddingRight: '15px',*/}
          {/*      color: '#ffffff',*/}
          {/*    }}*/}
          {/*  >*/}
          {/*    Blog*/}
          {/*  </span>*/}
          {/*</a>*/}
          <a href="/competition">
            <span className="px-[15px] text-white">Competition</span>
          </a>
        </div>
        <div className="flex items-center">
          <a href="/login">
            <span className="flex items-center gap-1 py-2.5 px-[15px] text-black rounded-[30px] bg-white">
              <User className="w-4 h-4" /> Login
            </span>
          </a>
          <img width={25} src="/assets/images/language.svg" className="m-1.5" alt="Language" />
        </div>
      </div>
    </header>
  );
}

export default Header;
