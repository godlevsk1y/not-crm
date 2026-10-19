import { ApiRequestError } from "@/shared/api";
import type { FieldPath, FieldValues, UseFormSetError } from "react-hook-form";

export function applyServerErrors<T extends FieldValues>({
  setErrors: setError,
  error,
  fieldNameMap,
  errorCodeMessagesMap,
}: {
  setErrors: UseFormSetError<T>;
  error: ApiRequestError;

  fieldNameMap: Map<string, FieldPath<T>>;
  errorCodeMessagesMap: Map<string, string>;
}) {
  for (const msg of error.messages) {
    if (msg.invalidField === null) continue;

    const formFieldName = fieldNameMap.get(msg.invalidField);
    if (!formFieldName) continue;

    setError(formFieldName, {
      type: error.type,
      message:
        errorCodeMessagesMap.get(msg.code) ?? "Поле заполнено некорректно",
    });
  }
}
