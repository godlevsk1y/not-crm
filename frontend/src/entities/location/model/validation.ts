import z from "zod";

export const locationSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Укажите название локации")
    .max(100, "Максимум 100 символов"),

  country: z.string().trim().min(1, "Укажите страну"),
  region: z.string().trim(),

  city: z.string().trim().min(1, "Укажите город"),
  district: z.string().trim(),

  street: z.string().trim().min(1, "Укажите улицу"),
  houseNumber: z.string().trim().min(1, "Укажите номер дома"),

  postalCode: z.string().trim(),
});

export type LocationFormValues = z.infer<typeof locationSchema>;
