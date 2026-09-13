import type { FieldValues, Path, UseFormSetError } from "react-hook-form";
import { ApiError } from "@/lib/api";

/**
 * Attaches an API validation failure to the matching form inputs.
 *
 * Returns the message to show above the form, or null when every error found a home on a field.
 * Without that fallback a 400 naming a field the form does not render would leave the user
 * staring at a form that simply refuses to submit, with nothing explaining why.
 */
export function applyApiErrors<T extends FieldValues>(
  error: unknown,
  values: T,
  setError: UseFormSetError<T>,
  fallback = "Something went wrong. Please try again.",
): string | null {
  if (!(error instanceof ApiError)) {
    return fallback;
  }

  if (!error.fieldErrors) {
    return error.detail || fallback;
  }

  let applied = 0;

  for (const [field, messages] of Object.entries(error.fieldErrors)) {
    if (field in values && messages.length > 0) {
      setError(field as Path<T>, { message: messages[0] });
      applied += 1;
    }
  }

  if (applied > 0) {
    return null;
  }

  // Nothing matched a rendered field: surface whatever the API said, plus the stray field names.
  const unmatched = Object.values(error.fieldErrors).flat();
  return unmatched.length > 0 ? unmatched.join(" ") : error.detail || fallback;
}
