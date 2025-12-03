/**
 * Stub exports for examples module.
 * This component is disabled in production.
 */

'use client';


export interface ExampleProps {
    title: string;
    code?: string;
    description?: string;
}

export function CodeExample({ title, code, description }: ExampleProps) {
    return (
        <div className="border rounded-lg p-4">
            <h3 className="font-semibold mb-2">{title}</h3>
            {description && <p className="text-sm text-slate-500 mb-2">{description}</p>}
            {code && (
                <pre className="bg-slate-100 p-2 rounded text-sm overflow-x-auto">
                    <code>{code}</code>
                </pre>
            )}
        </div>
    );
}

export function ExampleGallery({ examples }: { examples: ExampleProps[] }) {
    return (
        <div className="grid gap-4">
            {examples.map((example, i) => (
                <CodeExample key={i} {...example} />
            ))}
        </div>
    );
}

export function NotificationBarExample() {
    return (
        <div className="p-4 border rounded-lg">
            <p className="text-slate-500">Notification Bar Example disabled</p>
        </div>
    );
}

const examples = { CodeExample, ExampleGallery, NotificationBarExample };
export default examples;
