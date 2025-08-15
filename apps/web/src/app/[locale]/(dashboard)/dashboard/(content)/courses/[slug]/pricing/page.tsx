"use client"

import * as React from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { useCourse } from "@/components/course-context"
import { updateCourse } from "@/lib/local-db"
import { useToast } from "@/hooks/use-toast"
import type { CoursePricing } from "@/lib/types"

export default function CoursePricingPage() {
  const course = useCourse()
  const { toast } = useToast()
  const [pricing, setPricing] = React.useState<CoursePricing>(course.pricing)

  const handleSave = () => {
    const updated = updateCourse(course.id, { pricing })
    if (updated) {
      toast({ title: "Pricing updated successfully" })
    }
  }

  return (
    <div className="space-y-8">
      <Card className="dark-card">
        <CardHeader>
          <CardTitle>Course Pricing</CardTitle>
          <CardDescription>
            Set your course pricing model and configure payment options
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div>
            <Label className="text-base font-medium">Pricing Model</Label>
            <RadioGroup 
              value={pricing.type} 
              onValueChange={(value: "free" | "paid" | "subscription") => 
                setPricing({ ...pricing, type: value })
              }
              className="mt-2"
            >
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="free" id="free" />
                <Label htmlFor="free">Free</Label>
              </div>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="paid" id="paid" />
                <Label htmlFor="paid">One-time payment</Label>
              </div>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="subscription" id="subscription" />
                <Label htmlFor="subscription">Subscription</Label>
              </div>
            </RadioGroup>
          </div>

          {pricing.type !== "free" && (
            <>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <Label htmlFor="price">Price</Label>
                  <Input
                    id="price"
                    type="number"
                    min="0"
                    step="0.01"
                    value={pricing.price}
                    onChange={(e) => setPricing({ ...pricing, price: parseFloat(e.target.value) || 0 })}
                    placeholder="0.00"
                  />
                </div>
                <div>
                  <Label htmlFor="currency">Currency</Label>
                  <Select value={pricing.currency} onValueChange={(value) => setPricing({ ...pricing, currency: value })}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="USD">USD ($)</SelectItem>
                      <SelectItem value="EUR">EUR (€)</SelectItem>
                      <SelectItem value="GBP">GBP (£)</SelectItem>
                      <SelectItem value="BRL">BRL (R$)</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>

              {pricing.type === "subscription" && (
                <div>
                  <Label htmlFor="subscriptionPeriod">Billing Period</Label>
                  <Select 
                    value={pricing.subscriptionPeriod} 
                    onValueChange={(value: "monthly" | "yearly") => setPricing({ ...pricing, subscriptionPeriod: value })}
                  >
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="monthly">Monthly</SelectItem>
                      <SelectItem value="yearly">Yearly</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              )}
            </>
          )}

          <div className="flex justify-end">
            <Button onClick={handleSave}>Save Pricing</Button>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
