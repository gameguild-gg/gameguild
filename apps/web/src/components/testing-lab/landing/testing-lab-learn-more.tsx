import { Button } from '@/components/ui/button';
import Link from 'next/link';

export function TestingLabLearnMore() {
    return (
        <div className="flex flex-col items-center mt-24 mb-12">
            <div className="flex items-center gap-6 mb-8">
                <div className="h-px bg-gradient-to-r from-transparent via-slate-600 to-transparent w-32"></div>
                <span className="text-slate-400 text-base font-medium">curious how it all works?</span>
                <div className="h-px bg-gradient-to-r from-transparent via-slate-600 to-transparent w-32"></div>
            </div>
            <Button
                asChild
                size="lg"
                variant="outline"
                className="bg-slate-900/20 backdrop-blur-md border border-slate-700/50 text-slate-200 hover:text-white hover:bg-slate-800/30 hover:border-slate-600/50 px-8 py-4 text-lg transition-all duration-200"
            >
                <Link href="#learn-more">Learn More</Link>
            </Button>
        </div>
    );
}
