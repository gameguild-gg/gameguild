"use client";

import { AlertCircle } from "lucide-react";

import { Badge } from "@game-guild/ui/components/badge";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card";

import { useServices } from "@/lib/polling";

type Service = {
  name: string;
  url: string;
  status: "pass" | "fail";
  responseTimeMs: number;
  httpStatus: number;
};

export default function ServicesPage() {
  const { data, isLoading, error } = useServices();
  const services = Array.isArray(data) ? (data as Service[]) : [];

  if (isLoading) {
    return <div className="text-muted-foreground">Loading services…</div>;
  }
  if (error) {
    return (
      <div className="flex items-center gap-2 text-destructive">
        <AlertCircle className="size-4" />
        Failed to load services.
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold">Services</h1>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        {services.map((svc) => {
          const ok = svc.status === "pass";
          return (
            <Card key={svc.name} className="service-card">
              <CardHeader>
                <CardTitle className="flex items-center justify-between">
                  <span>{svc.name}</span>
                  <Badge
                    className={
                      ok
                        ? "bg-green-500 text-white"
                        : "bg-red-500 text-white"
                    }
                  >
                    {ok ? "Operational" : "Down"}
                  </Badge>
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-1 text-sm">
                <div className="text-muted-foreground">
                  Response:{" "}
                  <span className="font-medium text-foreground">
                    {svc.responseTimeMs}ms
                  </span>
                </div>
                <div className="truncate font-mono text-xs text-muted-foreground">
                  {svc.url}
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
