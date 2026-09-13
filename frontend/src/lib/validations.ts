import { z } from "zod";

/**
 * Client-side mirrors of the FluentValidation rules on the API.
 *
 * These exist to give immediate feedback, not to enforce anything: the API re-validates every
 * request, because anything the browser checks can be skipped by a client that chooses to.
 */

const today = () => {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate());
};

export const loginSchema = z.object({
  email: z.string().min(1, "Email is required.").email("Enter a valid email address."),
  password: z.string().min(1, "Password is required."),
});

export type LoginValues = z.infer<typeof loginSchema>;

export const registerSchema = z.object({
  name: z.string().trim().min(1, "Name is required.").max(120, "Name must be 120 characters or fewer."),
  companyName: z
    .string()
    .trim()
    .min(1, "Company name is required.")
    .max(160, "Company name must be 160 characters or fewer."),
  email: z.string().min(1, "Email is required.").email("Enter a valid email address."),
  password: z
    .string()
    .min(8, "Password must be at least 8 characters.")
    .max(128, "Password must be 128 characters or fewer.")
    .regex(/[A-Z]/, "Password must contain an uppercase letter.")
    .regex(/[a-z]/, "Password must contain a lowercase letter.")
    .regex(/[0-9]/, "Password must contain a number."),
  role: z.enum(["Buyer", "Supplier"], { required_error: "Choose whether you are buying or supplying." }),
});

export type RegisterValues = z.infer<typeof registerSchema>;

export const rfqSchema = z.object({
  productName: z
    .string()
    .trim()
    .min(1, "Product name is required.")
    .max(200, "Product name must be 200 characters or fewer."),
  description: z
    .string()
    .trim()
    .min(1, "Description is required.")
    .max(5000, "Description must be 5000 characters or fewer."),
  quantity: z.coerce
    .number({ invalid_type_error: "Quantity must be a number." })
    .int("Quantity must be a whole number.")
    .gt(0, "Quantity must be greater than 0.")
    .max(10_000_000, "Quantity must be 10,000,000 or fewer."),
  deliveryLocation: z
    .string()
    .trim()
    .min(1, "Delivery location is required.")
    .max(200, "Delivery location must be 200 characters or fewer."),
  deadline: z
    .string()
    .min(1, "Deadline is required.")
    .refine((value) => !Number.isNaN(Date.parse(value)), "Enter a valid date.")
    .refine((value) => new Date(value) >= today(), "Deadline must be today or a future date."),
});

export type RfqValues = z.infer<typeof rfqSchema>;

export const quotationSchema = z.object({
  quotedPrice: z.coerce
    .number({ invalid_type_error: "Quoted price must be a number." })
    .gt(0, "Quoted price must be greater than 0.")
    .max(999_999_999_999.99, "Quoted price is too large.")
    .refine((value) => Number(value.toFixed(2)) === value, "Quoted price can have at most 2 decimal places."),
  estimatedDeliveryDays: z.coerce
    .number({ invalid_type_error: "Estimated delivery must be a number." })
    .int("Estimated delivery must be a whole number of days.")
    .gt(0, "Estimated delivery must be at least 1 day.")
    .max(3650, "Estimated delivery must be 3650 days or fewer."),
  message: z.string().trim().max(2000, "Message must be 2000 characters or fewer.").optional(),
});

export type QuotationValues = z.infer<typeof quotationSchema>;
