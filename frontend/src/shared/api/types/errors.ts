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

export type ApiError = {
  messages: ApiErrorMessage[];
  type: ApiErrorType;
};

export type ApiErrorMessage = {
  code: string;
  message: string;
  invalidField: string | null;
};

export type ApiErrorType =
  | "failure"
  | "validation"
  | "not_found"
  | "conflict"
  | "internal"
  | "domain"
  | "bad_request";
