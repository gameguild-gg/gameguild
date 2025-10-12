import React from 'react';

export default async function Page() {
  return (
    <article className="flex flex-1 flex-col gap-8">
      <div className="flex flex-auto gap-8">
        <div className="min-w-60">
          <aside>
            {/* TODO: Add the post category here */}
            {/* TODO: Add the date here */}
            <section></section>
            {/*  TODO: I should separate it in a component*/}
            <section>
              <summary>
                <ol>
                  <li></li>
                  <li></li>
                  <li></li>
                </ol>
              </summary>
            </section>
          </aside>
        </div>
        <article className="flex flex-1">
          <header className="">
            <h1 className=""></h1>
          </header>
          <article className="flex flex-1">{/* TODO: I should move the content here */}</article>
          <footer className="">{/* TODO: I do need this section? */}</footer>
        </article>
      </div>
    </article>
  );
}
