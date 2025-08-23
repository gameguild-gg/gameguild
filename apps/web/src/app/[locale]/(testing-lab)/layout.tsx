import { Header } from '@/components/common/header';
import { Footer } from '@/components/common/footer';

export default async function Layout({ children }: PropsWithChildren): Promise<React.JSX.Element> {
  return (
    <div className="flex flex-col min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
        <Header />
      <main className="flex-1">
        {children}
      </main>
      <Footer />
    </div>
  );
}
