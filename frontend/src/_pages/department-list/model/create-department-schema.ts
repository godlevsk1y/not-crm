import { departmentSchema } from "@/entities/department";
import z from "zod";

const selectOptionSchema = z.object({ id: z.string(), name: z.string() });

export const createDepartmentSchema = departmentSchema.extend({
  parent: selectOptionSchema.nullable(),
  locations: z.array(selectOptionSchema),
});

export type CreateDepartmentFormValues = z.infer<typeof createDepartmentSchema>;
