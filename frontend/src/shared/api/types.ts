export type ApiEnvelope<T> = {
  result: T | null;
  error: ApiError | null;
  isError: boolean;
  timeGenerated: string;
};

export type PagedResult<T> = {
  results: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};

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
