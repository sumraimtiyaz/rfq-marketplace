"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiError, api, buildRfqQueryString } from "@/lib/api";
import type {
  BuyerDashboard,
  MyQuotation,
  PagedResult,
  Quotation,
  RfqDetail,
  RfqListItem,
  RfqQuery,
  SupplierDashboard,
} from "@/types";
import type { QuotationValues, RfqValues } from "@/lib/validations";

/**
 * Query keys are namespaced by resource so a mutation can invalidate a whole area
 * (`["rfqs"]`) without having to know every filter combination currently cached.
 */
export const queryKeys = {
  myRfqs: (query: RfqQuery) => ["rfqs", "mine", query] as const,
  browseRfqs: (query: RfqQuery) => ["rfqs", "browse", query] as const,
  rfq: (id: string) => ["rfqs", "detail", id] as const,
  rfqQuotations: (id: string) => ["rfqs", "detail", id, "quotations"] as const,
  myQuotations: () => ["quotations", "mine"] as const,
  locations: () => ["rfqs", "locations"] as const,
  buyerDashboard: () => ["dashboard", "buyer"] as const,
  supplierDashboard: () => ["dashboard", "supplier"] as const,
};

/** Retrying a 403 or 404 just delays the error state the user needs to see. */
const retryUnlessClientError = (failureCount: number, error: unknown) => {
  if (error instanceof ApiError && error.status >= 400 && error.status < 500) return false;
  return failureCount < 2;
};

export function useMyRfqs(query: RfqQuery) {
  return useQuery({
    queryKey: queryKeys.myRfqs(query),
    queryFn: () => api.get<PagedResult<RfqListItem>>(`/api/rfqs/my${buildRfqQueryString(query)}`),
    retry: retryUnlessClientError,
    placeholderData: (previous) => previous, // keeps the table on screen while filters change
  });
}

export function useBrowseRfqs(query: RfqQuery) {
  return useQuery({
    queryKey: queryKeys.browseRfqs(query),
    queryFn: () => api.get<PagedResult<RfqListItem>>(`/api/rfqs${buildRfqQueryString(query)}`),
    retry: retryUnlessClientError,
    placeholderData: (previous) => previous,
  });
}

export function useRfq(id: string) {
  return useQuery({
    queryKey: queryKeys.rfq(id),
    queryFn: () => api.get<RfqDetail>(`/api/rfqs/${id}`),
    retry: retryUnlessClientError,
    enabled: !!id,
  });
}

export function useRfqQuotations(id: string, enabled = true) {
  return useQuery({
    queryKey: queryKeys.rfqQuotations(id),
    queryFn: () => api.get<Quotation[]>(`/api/rfqs/${id}/quotations`),
    retry: retryUnlessClientError,
    enabled: enabled && !!id,
  });
}

export function useMyQuotations() {
  return useQuery({
    queryKey: queryKeys.myQuotations(),
    queryFn: () => api.get<MyQuotation[]>("/api/quotations/my"),
    retry: retryUnlessClientError,
  });
}

export function useDeliveryLocations() {
  return useQuery({
    queryKey: queryKeys.locations(),
    queryFn: () => api.get<string[]>("/api/rfqs/locations"),
    retry: retryUnlessClientError,
    staleTime: 5 * 60 * 1000,
  });
}

export function useBuyerDashboard() {
  return useQuery({
    queryKey: queryKeys.buyerDashboard(),
    queryFn: () => api.get<BuyerDashboard>("/api/dashboard/buyer"),
    retry: retryUnlessClientError,
  });
}

export function useSupplierDashboard() {
  return useQuery({
    queryKey: queryKeys.supplierDashboard(),
    queryFn: () => api.get<SupplierDashboard>("/api/dashboard/supplier"),
    retry: retryUnlessClientError,
  });
}

export function useCreateRfq() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (values: RfqValues) => api.post<RfqDetail>("/api/rfqs", values),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["rfqs"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
}

export function useUpdateRfq(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (values: RfqValues) => api.put<RfqDetail>(`/api/rfqs/${id}`, values),
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.rfq(id), updated);
      void queryClient.invalidateQueries({ queryKey: ["rfqs"] });
    },
  });
}

export function useDeleteRfq() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => api.delete<void>(`/api/rfqs/${id}`),
    onSuccess: (_result, id) => {
      queryClient.removeQueries({ queryKey: queryKeys.rfq(id) });
      void queryClient.invalidateQueries({ queryKey: ["rfqs"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
}

export function useSetRfqStatus(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (action: "close" | "reopen") => api.post<RfqDetail>(`/api/rfqs/${id}/${action}`),
    onSuccess: (updated) => {
      queryClient.setQueryData(queryKeys.rfq(id), updated);
      void queryClient.invalidateQueries({ queryKey: ["rfqs"] });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
}

export function useSubmitQuotation(rfqId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (values: QuotationValues) =>
      api.post<Quotation>(`/api/rfqs/${rfqId}/quotations`, values),
    onSuccess: () => {
      // The RFQ detail carries hasQuoted, and the browse list shows the same marker.
      void queryClient.invalidateQueries({ queryKey: queryKeys.rfq(rfqId) });
      void queryClient.invalidateQueries({ queryKey: ["rfqs", "browse"] });
      void queryClient.invalidateQueries({ queryKey: queryKeys.myQuotations() });
      void queryClient.invalidateQueries({ queryKey: ["dashboard"] });
    },
  });
}
