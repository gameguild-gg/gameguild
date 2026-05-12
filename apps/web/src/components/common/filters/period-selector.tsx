'use client';

import { Button } from '@/components/ui/button';
import { PERIOD_OPTIONS, type PeriodType } from './filter-context';

interface PeriodSelectorProps {
    selectedPeriod: PeriodType;
    onPeriodChange: (period: PeriodType) => void;
}

export function PeriodSelector({ selectedPeriod, onPeriodChange }: Readonly<PeriodSelectorProps>) {
    return (
        <div className="flex flex-wrap gap-2">
            {PERIOD_OPTIONS.map((option) => {
                const isSelected = option.value === selectedPeriod;

                return (
                    <Button
                        key={option.value}
                        type="button"
                        size="sm"
                        variant={isSelected ? 'default' : 'outline'}
                        onClick={() => onPeriodChange(option.value)}
                    >
                        {option.label}
                    </Button>
                );
            })}
        </div>
    );
}