"use client"

import React, { FunctionComponent, PropsWithChildren } from "react";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card";

interface ResetPasswordFormProps {
  email: string;
  setEmail: (email: string) => void;
  isLoading: boolean;
  error: string;
  success: string;
  resetSent: boolean;
  onSendReset: (event: React.FormEvent) => void;
  onCancel: () => void;
  onResend: () => void;
}

export const ResetPasswordForm: FunctionComponent<ResetPasswordFormProps> = (): React.JSX.Element => {
  return (
    <AuthForm>
      <AuthFormHeader title="Reset Password" description="Enter your details to reset your password">

      </AuthFormHeader>
      <AuthFormFooter>

      </AuthFormFooter>
    </AuthForm>
  );
}


interface AuthCardProps {
  title: string;
  description: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
}

export const AuthForm: FunctionComponent<PropsWithChildren> = ({ children }): React.JSX.Element => {
  return (
    <Card className="border-0 shadow-2xl overflow-hidden backdrop-blur-sm bg-white/95 animate-fade-in-up">
      <div className="h-1.5 bg-gradient-to-r from-blue-600 via-indigo-500 to-purple-600"/>



    </Card>
  );
};

interface AuthFormHeaderProps {
  title: string;
  description: string;
  children?: React.ReactNode;
}

export const AuthFormHeader: FunctionComponent<AuthFormHeaderProps> = ({ title, description, children }): React.JSX.Element => {
  return (
    <CardHeader className="pb-0 pt-8">
      <CardTitle className="text-3xl font-bold text-center text-slate-800">
        { title }
      </CardTitle>
      <CardDescription className="text-center text-slate-500 mt-2 text-base">
        { description }
      </CardDescription>
      { children }
    </CardHeader>
  );
}

export const AuthFormContent: FunctionComponent<PropsWithChildren> = ({ children }): React.JSX.Element => {
  return (
    <CardContent className="px-8 pt-8 pb-4">
      { children }
    </CardContent>
  );
}

export const AuthFormFooter: FunctionComponent<PropsWithChildren> = ({ children }): React.JSX.Element => {
  return (
    <CardFooter className="flex flex-col space-y-6 px-8 pt-2 pb-8">
      { children }
    </CardFooter>
  );
}