"use client";

import { createContext, useCallback, useContext, useMemo, type ReactNode } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { ApiError, api } from "@/lib/api";
import type { AuthResponse, Role, User } from "@/types";
import type { LoginValues, RegisterValues } from "@/lib/validations";

interface AuthContextValue {
  user: User | null;
  isLoading: boolean;
  /** True when the session check failed for a reason other than "not signed in". */
  isUnavailable: boolean;
  login: (values: LoginValues) => Promise<User>;
  register: (values: RegisterValues) => Promise<User>;
  logout: () => Promise<void>;
  refetch: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export const currentUserKey = ["auth", "me"] as const;

export function AuthProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const router = useRouter();

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: currentUserKey,
    queryFn: () => api.get<User>("/api/auth/me"),
    // A 401 is the expected answer for a signed-out visitor, not a failure worth retrying.
    retry: (failureCount, err) =>
      !(err instanceof ApiError && err.isUnauthorized) && failureCount < 2,
    staleTime: 5 * 60 * 1000,
  });

  const isSignedOut = error instanceof ApiError && error.isUnauthorized;

  const applySession = useCallback(
    (auth: AuthResponse) => {
      // The token itself lives in an HTTP-only cookie the browser sets for us; only the
      // user profile is worth keeping in the cache.
      queryClient.setQueryData(currentUserKey, auth.user);
      return auth.user;
    },
    [queryClient],
  );

  const loginMutation = useMutation({
    mutationFn: (values: LoginValues) => api.post<AuthResponse>("/api/auth/login", values),
    onSuccess: applySession,
  });

  const registerMutation = useMutation({
    mutationFn: (values: RegisterValues) => api.post<AuthResponse>("/api/auth/register", values),
    onSuccess: applySession,
  });

  const logoutMutation = useMutation({
    mutationFn: () => api.post<void>("/api/auth/logout"),
    onSettled: () => {
      // Clear everything: cached RFQs and quotations belong to the session that just ended.
      queryClient.clear();
      router.replace("/login");
    },
  });

  const value = useMemo<AuthContextValue>(
    () => ({
      user: data ?? null,
      isLoading,
      isUnavailable: !!error && !isSignedOut,
      login: async (values) => applySession(await loginMutation.mutateAsync(values)),
      register: async (values) => applySession(await registerMutation.mutateAsync(values)),
      logout: async () => {
        await logoutMutation.mutateAsync();
      },
      refetch: () => void refetch(),
    }),
    [data, isLoading, error, isSignedOut, applySession, loginMutation, registerMutation, logoutMutation, refetch],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside an AuthProvider.");
  }
  return context;
}

/** Where a signed-in user of each role belongs. */
export function homePathFor(role: Role): string {
  return role === "Buyer" ? "/buyer/rfqs" : "/supplier/rfqs";
}
