import { ApiErrorMessage, ApiErrorType } from "./types";

export class ApiRequestError extends Error {
  constructor(
    public readonly type: ApiErrorType | "network" | "http" | "protocol",
    public readonly messages: ApiErrorMessage[] = [],
    public readonly status?: number,
  ) {
    super(messages[0]?.message ?? "Failed to execute the request");

    this.name = "ApiRequestError";
  }
}
