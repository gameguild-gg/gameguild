'use client';

import { Button } from '@/components/ui/button';
import { LayoutGrid, List, Table2 } from 'lucide-react';

type ViewMode = 'cards' | 'row' | 'table';

interface ViewModeToggleProps {
    viewMode: ViewMode;
    onViewModeChange: (mode: ViewMode) => void;
}

const VIEW_MODES: ReadonlyArray<{
    value: ViewMode;
    label: string;
    icon: typeof LayoutGrid;
}> = [
        { value: 'cards', label: 'Cards', icon: LayoutGrid },
        { value: 'row', label: 'Rows', icon: List },
        { value: 'table', label: 'Table', icon: Table2 },
    ];

export function ViewModeToggle({ viewMode, onViewModeChange }: Readonly<ViewModeToggleProps>) {
    return (
        <div className="flex gap-2">
            {VIEW_MODES.map((mode) => {
                const Icon = mode.icon;
                const isSelected = mode.value === viewMode;

                return (
                    <Button
                        key={mode.value}
                        type="button"
                        size="icon"
                        variant={isSelected ? 'default' : 'outline'}
                        aria-label={`Switch to ${mode.label.toLowerCase()} view`}
                        onClick={() => onViewModeChange(mode.value)}
                    >
                        <Icon className="size-4" />
                    </Button>
                );
            })}
        </div>
    );
}