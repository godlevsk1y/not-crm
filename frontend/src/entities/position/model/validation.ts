import z from "zod";

export const positionSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Укажите название должности")
    .max(100, "Максимум 100 символов"),
});

export type PositionFormValues = z.infer<typeof positionSchema>;
