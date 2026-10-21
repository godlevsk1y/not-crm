import axios, { AxiosRequestConfig } from "axios";
import { apiClient } from "./axios-instance";
import { ApiRequestError } from "./types/errors";
import { ApiEnvelope } from "./types/envelope";

export async function apiRequest<T>(config: AxiosRequestConfig): Promise<T> {
  try {
    const response = await apiClient.request<ApiEnvelope<T>>(config);

    if (response.status === 204) {
      return undefined as T;
    }

    const { data } = response;

    if (data.isError) {
      throw new ApiRequestError(
        data.error?.type ?? "failure",
        data.error?.messages ?? [],
        response.status,
      );
    }

    if (data.result == null) {
      throw new ApiRequestError("protocol");
    }

    return data.result;
  } catch (error) {
    if (error instanceof ApiRequestError) {
      throw error;
    }

    if (axios.isAxiosError<ApiEnvelope<unknown>>(error)) {
      const response = error.response;
      const body = response?.data;

      if (body?.isError && body.error) {
        throw new ApiRequestError(
          body.error.type,
          body.error.messages,
          response?.status,
        );
      }

      if (response) {
        throw new ApiRequestError("http", [], response.status);
      }

      throw new ApiRequestError("network");
    }

    throw error;
  }
}
