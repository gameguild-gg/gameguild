"use client";

import type { TestingLocationSummary } from "@/lib/testing-lab/queries";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import {
  RadioGroup,
  RadioGroupItem,
} from "@game-guild/ui/components/radio-group";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import { Textarea } from "@game-guild/ui/components/textarea";
import { Globe2, MapPin } from "lucide-react";
import { useState } from "react";

export function TestingLocationFields({
  idPrefix,
  location,
}: {
  idPrefix: string;
  location?: TestingLocationSummary;
}) {
  const [mode, setMode] = useState(location?.isVirtual ? "true" : "false");

  return (
    <div className="grid gap-5">
      <fieldset>
        <legend className="mb-2 text-sm font-medium">Delivery mode</legend>
        <RadioGroup
          name="isVirtual"
          value={mode}
          onValueChange={setMode}
          className="grid grid-cols-2 gap-2"
          aria-label="Location delivery mode"
        >
          <Label
            htmlFor={idPrefix + "-physical"}
            className="flex min-h-12 cursor-pointer items-center gap-3 rounded-md border px-3 py-2 has-[[data-state=checked]]:border-primary has-[[data-state=checked]]:bg-primary/5"
          >
            <RadioGroupItem id={idPrefix + "-physical"} value="false" />
            <MapPin aria-hidden="true" className="size-4" />
            <span>
              <span className="block text-sm font-medium">Physical</span>
              <span className="block text-xs text-muted-foreground">
                Campus room or venue
              </span>
            </span>
          </Label>
          <Label
            htmlFor={idPrefix + "-remote"}
            className="flex min-h-12 cursor-pointer items-center gap-3 rounded-md border px-3 py-2 has-[[data-state=checked]]:border-primary has-[[data-state=checked]]:bg-primary/5"
          >
            <RadioGroupItem id={idPrefix + "-remote"} value="true" />
            <Globe2 aria-hidden="true" className="size-4" />
            <span>
              <span className="block text-sm font-medium">Remote</span>
              <span className="block text-xs text-muted-foreground">
                Online moderated lab
              </span>
            </span>
          </Label>
        </RadioGroup>
      </fieldset>

      <div className="space-y-2">
        <Label htmlFor={idPrefix + "-name"}>Location name</Label>
        <Input
          id={idPrefix + "-name"}
          name="name"
          required
          defaultValue={location?.name ?? ""}
          placeholder="South Campus · Lab 204"
        />
      </div>

      {mode === "true" ? (
        <div className="space-y-2">
          <Label htmlFor={idPrefix + "-virtual-url"}>Meeting URL</Label>
          <Input
            id={idPrefix + "-virtual-url"}
            name="virtualUrl"
            type="url"
            required
            defaultValue={location?.virtualUrl ?? ""}
            placeholder="https://meet.gameguild.gg/testing-lab"
          />
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor={idPrefix + "-address"}>Street address</Label>
            <Input
              id={idPrefix + "-address"}
              name="address"
              defaultValue={location?.address ?? ""}
              placeholder="123 Campus Avenue · Room 204"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor={idPrefix + "-city"}>City</Label>
            <Input
              id={idPrefix + "-city"}
              name="city"
              defaultValue={location?.city ?? ""}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor={idPrefix + "-state"}>State / region</Label>
            <Input
              id={idPrefix + "-state"}
              name="state"
              defaultValue={location?.state ?? ""}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor={idPrefix + "-postal-code"}>Postal code</Label>
            <Input
              id={idPrefix + "-postal-code"}
              name="postalCode"
              defaultValue={location?.postalCode ?? ""}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor={idPrefix + "-country"}>Country</Label>
            <Input
              id={idPrefix + "-country"}
              name="country"
              defaultValue={location?.country ?? ""}
            />
          </div>
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="space-y-2">
          <Label htmlFor={idPrefix + "-status"}>Operating status</Label>
          <Select
            name="status"
            defaultValue={String(location?.status ?? "Active")}
          >
            <SelectTrigger id={idPrefix + "-status"}>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Active">Active</SelectItem>
              <SelectItem value="Maintenance">Maintenance</SelectItem>
              <SelectItem value="Inactive">Inactive</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor={idPrefix + "-testers"}>Tester capacity</Label>
          <Input
            id={idPrefix + "-testers"}
            name="maxTestersCapacity"
            type="number"
            min="0"
            defaultValue={location?.maxTestersCapacity ?? 20}
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor={idPrefix + "-projects"}>Project capacity</Label>
          <Input
            id={idPrefix + "-projects"}
            name="maxProjectsCapacity"
            type="number"
            min="0"
            defaultValue={location?.maxProjectsCapacity ?? 6}
          />
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor={idPrefix + "-contact-email"}>Operations email</Label>
          <Input
            id={idPrefix + "-contact-email"}
            name="contactEmail"
            type="email"
            defaultValue={location?.contactEmail ?? ""}
            placeholder="testing-lab@gameguild.gg"
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor={idPrefix + "-contact-phone"}>Operations phone</Label>
          <Input
            id={idPrefix + "-contact-phone"}
            name="contactPhone"
            type="tel"
            defaultValue={location?.contactPhone ?? ""}
          />
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor={idPrefix + "-equipment"}>
          Equipment and facilities
        </Label>
        <Input
          id={idPrefix + "-equipment"}
          name="equipmentAvailable"
          defaultValue={location?.equipmentAvailable ?? ""}
          placeholder="PCs, controllers, headsets, accessibility equipment"
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor={idPrefix + "-description"}>Operating notes</Label>
        <Textarea
          id={idPrefix + "-description"}
          name="description"
          rows={3}
          defaultValue={location?.description ?? ""}
        />
      </div>
    </div>
  );
}
