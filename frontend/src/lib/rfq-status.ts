import type { RfqStatus, RfqStatusValue } from "@/types";

/**
 * Reads an RFQ status regardless of how the API happened to encode it.
 *
 * System.Text.Json serialises a C# enum as its numeric value unless a string converter is
 * registered, so `status` arrives as 0 or 1 on the RFQ endpoints - while `rfqStatus` on the
 * quotations endpoint is a real string, because that DTO calls ToString() server-side.
 *
 * Comparing against "Closed" therefore silently never matched, and a closed RFQ rendered
 * with an "Open" badge. Accepting both shapes fixes that here and keeps working if the API
 * later adds a JsonStringEnumConverter.
 */

/** Numeric values of the RfqStatus enum, in declaration order. */
const CLOSED_ORDINAL = 1;

export function isRfqClosed(status: RfqStatusValue): boolean {
  return status === "Closed" || status === CLOSED_ORDINAL;
}

export function isRfqOpen(status: RfqStatusValue): boolean {
  return !isRfqClosed(status);
}

/** Capitalised label for display. */
export function rfqStatusLabel(status: RfqStatusValue): "Open" | "Closed" {
  return isRfqClosed(status) ? "Closed" : "Open";
}
