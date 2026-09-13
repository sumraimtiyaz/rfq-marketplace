const currency = new Intl.NumberFormat("en-IN", {
  style: "currency",
  currency: "INR",
  maximumFractionDigits: 2,
});

const compactNumber = new Intl.NumberFormat("en-IN");

const longDate = new Intl.DateTimeFormat("en-IN", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

/** Prices are quoted in a single currency; multi-currency is out of scope (see README). */
export function formatCurrency(value: number): string {
  return currency.format(value);
}

export function formatNumber(value: number): string {
  return compactNumber.format(value);
}

/** The API sends deadlines as plain "yyyy-MM-dd" dates, with no timezone to shift them. */
export function formatDate(value: string): string {
  const date = new Date(`${value.slice(0, 10)}T00:00:00`);
  return Number.isNaN(date.getTime()) ? value : longDate.format(date);
}

export function formatDateTime(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : longDate.format(date);
}

/** "3 days left", "Due today", "Closed 2 days ago" - the state a supplier actually cares about. */
export function describeDeadline(deadline: string): { label: string; urgent: boolean; past: boolean } {
  const target = new Date(`${deadline.slice(0, 10)}T00:00:00`);
  const now = new Date();
  const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());

  const days = Math.round((target.getTime() - startOfToday.getTime()) / 86_400_000);

  if (days < 0) return { label: `Closed ${Math.abs(days)} day${Math.abs(days) === 1 ? "" : "s"} ago`, urgent: false, past: true };
  if (days === 0) return { label: "Due today", urgent: true, past: false };
  if (days === 1) return { label: "1 day left", urgent: true, past: false };
  if (days <= 3) return { label: `${days} days left`, urgent: true, past: false };
  return { label: `${days} days left`, urgent: false, past: false };
}

export function pluralise(count: number, singular: string, plural = `${singular}s`): string {
  return `${formatNumber(count)} ${count === 1 ? singular : plural}`;
}

/** Today's date as yyyy-MM-dd, for the `min` attribute on date inputs. */
export function todayIsoDate(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}
