"use client";

import * as React from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Button } from "@/components/ui/button";
import { PricingDto } from "@/lib/api/generated/types.gen";

type ProgramPricingFormProps = {
  initial: PricingDto;
  saveAction: (formData: FormData) => Promise<void>;
  monetizeAction?: () => Promise<void>;
  disableMonetizationAction?: () => Promise<void>;
};

export function ProgramPricingForm({ initial, saveAction, monetizeAction, disableMonetizationAction }: ProgramPricingFormProps) {
  const [isSubscription, setIsSubscription] = React.useState<boolean>(Boolean(initial.isSubscription));
  const [monetizationEnabled, setMonetizationEnabled] = React.useState<boolean>(Boolean(initial.isMonetizationEnabled));

  return (
    <div className="grid gap-6 md:grid-cols-2">
      <Card>
        <CardHeader>
          <CardTitle>Pricing</CardTitle>
        </CardHeader>
        <CardContent>
          <form action={saveAction} className="space-y-4">
            <div className="grid gap-4">
              <div className="grid gap-2">
                <Label htmlFor="price">Price</Label>
                <Input id="price" name="price" type="number" step="0.01" defaultValue={initial.price ?? ""} />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="currency">Currency</Label>
                <Input id="currency" name="currency" placeholder="USD" defaultValue={initial.currency ?? "USD"} />
              </div>
              <div className="flex items-center justify-between">
                <div className="grid gap-1">
                  <Label>Subscription</Label>
                  <span className="text-xs text-muted-foreground">Charge on a recurring basis</span>
                </div>
                <input type="hidden" name="isSubscription" value={isSubscription ? "true" : "false"} />
                <Switch checked={isSubscription} onCheckedChange={(v) => setIsSubscription(Boolean(v))} />
              </div>
              {isSubscription && (
                <div className="grid gap-2">
                  <Label htmlFor="subscriptionDurationDays">Subscription Duration (days)</Label>
                  <Input
                    id="subscriptionDurationDays"
                    name="subscriptionDurationDays"
                    type="number"
                    min={1}
                    defaultValue={initial.subscriptionDurationDays ?? 30}
                  />
                </div>
              )}
            </div>
            <div className="flex gap-2">
              <Button type="submit">Save pricing</Button>
            </div>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Monetization</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="grid gap-1">
              <Label>Enable Monetization</Label>
              <span className="text-xs text-muted-foreground">Allow users to purchase this course</span>
            </div>
            {/* This switch is visual; actions are triggered by buttons below to use server actions */}
            <Switch checked={monetizationEnabled} onCheckedChange={(v) => setMonetizationEnabled(Boolean(v))} disabled />
          </div>
          <div className="flex gap-2">
            {monetizeAction && (
              <form action={async () => monetizeAction?.()}>
                <Button type="submit" variant="default">Enable</Button>
              </form>
            )}
            {disableMonetizationAction && (
              <form action={async () => disableMonetizationAction?.()}>
                <Button type="submit" variant="outline">Disable</Button>
              </form>
            )}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

export default ProgramPricingForm;
