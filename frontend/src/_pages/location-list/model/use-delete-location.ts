import { locationsApi } from "@/entities/location";
import { ApiRequestError } from "@/shared/api";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

const errorMessagesByCode = new Map<string, string>([
  ["location.not.found", "Локация с таким именем не найдена"],
]);

export function useDeleteLocation() {
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: locationsApi.deleteLocationAsync,
    onSettled: async () => {
      await queryClient.invalidateQueries({
        queryKey: ["locations"],
      });
    },
    onSuccess: () => {
      toast.success("Локация была успешно удалена!");
    },
    onError: (error) => {
      if (error instanceof ApiRequestError && error.messages.length == 1) {
        const displayMessage = errorMessagesByCode.get(error.messages[0]?.code);

        if (displayMessage === undefined) {
          toast.error("Не удалось удалить локацию");
          return;
        }

        toast.error(displayMessage);
        return;
      }

      toast.error("Не удалось удалить локацию");
    },
  });

  return {
    deleteLocationFunc: mutation.mutate,
    isError: mutation.isError,
    error: mutation.error,
    isPending: mutation.isPending,
  };
}
