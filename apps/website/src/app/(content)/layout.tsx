import React, { PropsWithChildren } from 'react';
import Link from 'next/link';

export default async function Layout({ children }: Readonly<PropsWithChildren>) {
  return (
    <div>
      {/* TODO: Move to a new component */}
      <nav>
        <ul>
          <li>
            <Link href="/about">About</Link>
          </li>
          <li>
            <Link href="/work">Work</Link>
          </li>
          <li>
            <Link href="/courses">Courses</Link>
          </li>
          <li>
            <Link href="/blog">Blog</Link>
          </li>
          <li>
            <Link href="/contact">Contact</Link>
          </li>
        </ul>
      </nav>
      {children}
    </div>
  );
}
