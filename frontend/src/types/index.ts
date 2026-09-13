/** Mirrors the DTOs returned by the ASP.NET Core API. */

export type Role = "Buyer" | "Supplier";

export type RfqStatus = "Open" | "Closed";

/**
 * What actually arrives on the wire.
 *
 * System.Text.Json serialises the C# RfqStatus enum numerically (0 = Open, 1 = Closed) on the
 * RFQ endpoints, because no string converter is registered; the quotations endpoint sends a
 * real string because that DTO calls ToString() server-side. Compare it through the helpers in
 * lib/rfq-status rather than against a literal.
 */
export type RfqStatusValue = RfqStatus | number;

export interface User {
  id: string;
  name: string;
  companyName: string;
  email: string;
  role: Role;
  createdAt: string;
}

export interface AuthResponse {
  user: User;
  token: string;
  expiresAt: string;
}

export interface RfqListItem {
  id: string;
  productName: string;
  quantity: number;
  deliveryLocation: string;
  deadline: string;
  status: RfqStatusValue;
  isExpired: boolean;
  buyerCompanyName: string;
  quotationCount: number;
  hasQuoted: boolean;
  createdAt: string;
}

export interface RfqDetail {
  id: string;
  productName: string;
  description: string;
  quantity: number;
  deliveryLocation: string;
  deadline: string;
  status: RfqStatusValue;
  isExpired: boolean;
  acceptsQuotations: boolean;
  buyerId: string;
  buyerCompanyName: string;
  isOwner: boolean;
  /** Only populated for the buyer who owns the RFQ. */
  quotationCount: number | null;
  hasQuoted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Quotation {
  id: string;
  rfqId: string;
  supplierId: string;
  supplierName: string;
  supplierCompanyName: string;
  quotedPrice: number;
  estimatedDeliveryDays: number;
  message: string | null;
  createdAt: string;
}

export interface MyQuotation {
  id: string;
  rfqId: string;
  rfqProductName: string;
  rfqDeliveryLocation: string;
  rfqDeadline: string;
  rfqStatus: RfqStatusValue;
  buyerCompanyName: string;
  quotedPrice: number;
  estimatedDeliveryDays: number;
  message: string | null;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface BuyerDashboard {
  totalRfqs: number;
  openRfqs: number;
  closedRfqs: number;
  quotationsReceived: number;
}

export interface SupplierDashboard {
  availableRfqs: number;
  quotationsSubmitted: number;
  openRfqsNotQuoted: number;
}

export interface RfqQuery {
  search?: string;
  location?: string;
  status?: RfqStatus;
  includeExpired?: boolean;
  sortBy?: string;
  page?: number;
  pageSize?: number;
}
