import { ApiError } from "./errors";

export type ApiEnvelope<T> = {
  result: T | null;
  error: ApiError | null;
  isError: boolean;
  timeGenerated: string;
};
