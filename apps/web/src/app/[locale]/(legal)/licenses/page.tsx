import { MarkdownContent } from '@/components/content/markdown';
import { Header } from '@/components/common/header';
import { Footer } from '@/components/common/footer';
import { getLicensesFromGitHub } from '@/lib/integrations/github/github.actions';

export default async function LicensesPage() {
  const licensesData = await getLicensesFromGitHub();

  if (!licensesData || licensesData.length === 0) {
    return (
      <div className="min-h-screen flex flex-col">
        <Header />
        <main className="flex-1">
          <div className="container mx-auto px-4 py-8">
            <h1 className="text-4xl font-bold mb-6">Licenses</h1>
            <p className="text-muted-foreground">Unable to load license information.</p>
          </div>
        </main>
        <Footer />
      </div>
    );
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Header />
      <main className="flex-1">
        <div className="container mx-auto px-4 py-8">
          <div className="mb-8">
            <h1 className="text-4xl font-bold mb-4">Licenses</h1>
            <p className="text-muted-foreground mb-4">
              GameGuild is available under a dual licensing model. You can choose the license that best fits your needs.
            </p>
            <div className="text-sm text-muted-foreground">
              <p><strong>Number of licenses:</strong> {licensesData.length}</p>
            </div>
          </div>

          <div className="space-y-12">
            {licensesData.map((license, index) => (
              <div key={license.filename} className={index > 0 ? 'border-t pt-8' : ''}>
                <div className="mb-6">
                   <h2 className="text-2xl font-semibold mb-2">{license.name}</h2>
                   <p className="text-sm text-muted-foreground">
                     <strong>File:</strong>{' '}
                     <a 
                       href={`https://github.com/gameguild-gg/gameguild/blob/main/${license.filename}`}
                       target="_blank"
                       rel="noopener noreferrer"
                       className="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 underline"
                     >
                       {license.filename}
                     </a>
                   </p>
                 </div>
                <div className="prose prose-sm max-w-none dark:prose-invert">
                  <MarkdownContent content={license.content} />
                </div>
              </div>
            ))}
          </div>
        </div>
      </main>
      <Footer />
    </div>
  );
}
