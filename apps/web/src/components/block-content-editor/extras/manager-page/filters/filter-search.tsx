"use client"

import React from 'react'
import { Input } from "@/components/ui/input"
import { Search } from 'lucide-react'

interface FilterSearchProps {
  value: string
  onChange: (value: string) => void
  placeholder: string
}

export function FilterSearch({ value, onChange, placeholder }: FilterSearchProps) {
  return (
    <div className="relative flex-1 min-w-[200px]">
      <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
      <Input
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="pl-9"
      />
    </div>
  )
}
