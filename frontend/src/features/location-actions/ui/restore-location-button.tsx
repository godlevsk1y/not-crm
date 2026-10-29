"use client";

import { locationsApi } from "@/entities/location";
import { ApiRequestError } from "@/shared/api";
import { Button } from "@/shared/ui/button";
import { Spinner } from "@/shared/ui/spinner";
import { ArrowCounterClockwiseIcon } from "@phosphor-icons/react/ssr";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useRef } from "react";
import { toast } from "sonner";

export function RestoreLocationButton({ id }: { id: string }) {
  const queryClient = useQueryClient();
  const inFlight = useRef(false);
  const mutation = useMutation({
    mutationFn: locationsApi.restoreLocationAsync,
    onSuccess: async () => {
      toast.success("Локация восстановлена");
      await Promise.all(
        ["departments", "locations", "positions"].map((key) =>
          queryClient.invalidateQueries({ queryKey: [key] }),
        ),
      );
    },
    onError: (error) => {
      if (error instanceof ApiRequestError) {
        if (error.status === 404) {
          toast.error("Локация не найдена");
          return;
        }
        if (error.status === 409) {
          toast.error("Не удалось восстановить: конфликт с существующими данными");
          return;
        }
      }
      toast.error("Не удалось восстановить локацию. Попробуйте ещё раз");
    },
    onSettled: () => {
      inFlight.current = false;
    },
  });

  return (
    <Button
      type="button"
      variant="outline"
      size="sm"
      disabled={mutation.isPending}
      aria-busy={mutation.isPending}
      onClick={() => {
        if (inFlight.current) return;
        inFlight.current = true;
        mutation.mutate({ id });
      }}
    >
      {mutation.isPending ? (
        <Spinner aria-hidden="true" />
      ) : (
        <ArrowCounterClockwiseIcon aria-hidden="true" />
      )}
      {mutation.isPending ? "Восстановление…" : "Восстановить"}
    </Button>
  );
}
