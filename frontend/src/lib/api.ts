import type { RfqQuery } from "@/types";

/**
 * A failure the API described in RFC 7807 ProblemDetails form. `fieldErrors` is populated for
 * 400 responses from FluentValidation, so forms can attach messages to the right input.
 */
export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail: string;
  readonly fieldErrors?: Record<string, string[]>;

  constructor(
    status: number,
    title: string,
    detail: string,
    fieldErrors?: Record<string, string[]>,
  ) {
    super(detail || title);
    this.name = "ApiError";
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.fieldErrors = fieldErrors;
  }

  get isUnauthorized() {
    return this.status === 401;
  }

  get isForbidden() {
    return this.status === 403;
  }

  get isNotFound() {
    return this.status === 404;
  }

  get isValidation() {
    return this.status === 400 && !!this.fieldErrors;
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

/**
 * Requests go to /api on this origin; next.config.ts proxies them to the API. Because it is
 * same-origin, the HTTP-only auth cookie rides along with no CORS involvement.
 */
async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;

  try {
    response = await fetch(path, {
      ...init,
      credentials: "include",
      headers: {
        Accept: "application/json",
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...init?.headers,
      },
    });
  } catch {
    // The request never reached the server: offline, DNS, or the API is down.
    throw new ApiError(0, "Network error", "Could not reach the server. Check your connection and try again.");
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const isJson = response.headers.get("content-type")?.includes("json");
  const body = isJson ? ((await response.json()) as unknown) : null;

  if (!response.ok) {
    const problem = (body ?? {}) as ProblemDetails;
    throw new ApiError(
      response.status,
      problem.title ?? "Request failed",
      problem.detail ?? defaultMessageFor(response.status),
      normaliseFieldErrors(problem.errors),
    );
  }

  return body as T;
}

function defaultMessageFor(status: number): string {
  switch (status) {
    case 401:
      return "You need to sign in to do that.";
    case 403:
      return "You don't have permission to do that.";
    case 404:
      return "We couldn't find what you were looking for.";
    case 409:
      return "That conflicts with something that already exists.";
    default:
      return "Something went wrong. Please try again.";
  }
}

/**
 * ASP.NET Core reports field errors with PascalCase keys ("ProductName"); react-hook-form
 * addresses fields by their camelCase form names.
 */
function normaliseFieldErrors(
  errors: Record<string, string[]> | undefined,
): Record<string, string[]> | undefined {
  if (!errors) return undefined;

  return Object.fromEntries(
    Object.entries(errors).map(([key, messages]) => [
      key.charAt(0).toLowerCase() + key.slice(1),
      messages,
    ]),
  );
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, { method: "POST", body: body === undefined ? undefined : JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: "PUT", body: JSON.stringify(body) }),
  delete: <T>(path: string) => request<T>(path, { method: "DELETE" }),
};

/** Serialises RFQ filters, dropping empty values so the URL stays readable. */
export function buildRfqQueryString(query: RfqQuery): string {
  const params = new URLSearchParams();

  if (query.search?.trim()) params.set("search", query.search.trim());
  if (query.location?.trim()) params.set("location", query.location.trim());
  if (query.status) params.set("status", query.status);
  if (query.includeExpired) params.set("includeExpired", "true");
  if (query.sortBy) params.set("sortBy", query.sortBy);
  if (query.page && query.page > 1) params.set("page", String(query.page));
  if (query.pageSize) params.set("pageSize", String(query.pageSize));

  const serialised = params.toString();
  return serialised ? `?${serialised}` : "";
}
