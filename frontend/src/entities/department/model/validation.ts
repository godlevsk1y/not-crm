import z from "zod";

export const departmentSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, "Укажите название департамента")
    .max(100, "Максимум 100 символов"),
  slug: z
    .string()
    .min(1, "Укажите slug")
    .max(100, "Максимум 100 символов")
    .regex(
      /^[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$/,
      "Используйте строчные латинские буквы, цифры и дефисы. Дефис не может быть в начале или конце",
    ),
});

export type DepartmentFormValues = z.infer<typeof departmentSchema>;
