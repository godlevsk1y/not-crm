export type ApiEnvelope<T> = {
  result?: T;
  error?: ApiError;
  isError: boolean;
  timeGenerated: Date;
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
  invalidField?: string;
};

export type ApiErrorType =
  | "failure"
  | "validation"
  | "not_found"
  | "conflict"
  | "internal"
  | "domain"
  | "bad_request";
