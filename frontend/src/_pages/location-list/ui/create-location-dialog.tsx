"use client";

import { useId, useState } from "react";
import { MapPinIcon, PlusIcon } from "@phosphor-icons/react";

import { Button } from "@/shared/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/shared/ui/dialog";
import { Input } from "@/shared/ui/input";
import { useForm } from "react-hook-form";
import {
  CreateLocationFormValues,
  createLocationSchema,
  useCreateLocation,
} from "../model/use-create-location";
import { zodResolver } from "@hookform/resolvers/zod";

export function CreateLocationDialog() {
  const id = useId();
  const [open, setOpen] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isValid },
  } = useForm<CreateLocationFormValues>({
    resolver: zodResolver(createLocationSchema),
    defaultValues: {
      name: "",
      country: "",
      region: "",
      city: "",
      district: "",
      street: "",
      houseNumber: "",
      postalCode: "",
    },
  });

  const { createLocationFunc, isPending } = useCreateLocation();

  const onSubmit = (values: CreateLocationFormValues) => {
    createLocationFunc(
      {
        ...values,
        region: values.region || undefined,
        district: values.district || undefined,
        postalCode: values.postalCode || undefined,
      },
      {
        onSuccess: () => {
          reset();
          setOpen(false);
        },
      },
    );
  };

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger render={<Button size="lg" />}>
        <PlusIcon aria-hidden="true" />
        Добавить локацию
      </DialogTrigger>

      <DialogContent className="max-h-[calc(100dvh-2rem)] gap-6 overflow-y-auto p-6 sm:max-w-xl">
        <DialogHeader className="gap-3 pr-6">
          <span className="flex size-10 shrink-0 items-center justify-center bg-sky-500/10 text-sky-600 dark:text-sky-400">
            <MapPinIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <DialogTitle className="text-xl font-semibold tracking-tight">
            Новая локация
          </DialogTitle>
          <DialogDescription>
            Укажите название и адрес офиса или рабочего пространства.
          </DialogDescription>
        </DialogHeader>

        <form className="grid gap-6" onSubmit={handleSubmit(onSubmit)}>
          <div className="grid gap-2">
            <label htmlFor={`${id}-name`} className="font-medium">
              Название локации
            </label>
            <Input
              id={`${id}-name`}
              placeholder="Например, Центральный офис"
              {...register("name")}
            />
          </div>

          <fieldset className="min-w-0 border-t pt-4">
            <legend className="pr-3 text-sm font-medium">Адрес</legend>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="grid gap-2">
                <label htmlFor={`${id}-country`} className="font-medium">
                  Страна
                </label>
                <Input
                  id={`${id}-country`}
                  placeholder="Россия"
                  {...register("country")}
                />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-region`} className="font-medium">
                  Регион{" "}
                  <span className="text-muted-foreground">(необязательно)</span>
                </label>
                <Input
                  id={`${id}-region`}
                  placeholder="Московская область"
                  {...register("region")}
                />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-city`} className="font-medium">
                  Город
                </label>
                <Input
                  id={`${id}-city`}
                  placeholder="Москва"
                  {...register("city")}
                />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-district`} className="font-medium">
                  Район{" "}
                  <span className="text-muted-foreground">(необязательно)</span>
                </label>
                <Input
                  id={`${id}-district`}
                  placeholder="Тверской"
                  {...register("district")}
                />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-street`} className="font-medium">
                  Улица
                </label>
                <Input
                  id={`${id}-street`}
                  placeholder="Лесная"
                  {...register("street")}
                />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-house-number`} className="font-medium">
                  Дом
                </label>
                <Input
                  id={`${id}-house-number`}
                  placeholder="7, корпус 1"
                  {...register("houseNumber")}
                />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-postal-code`} className="font-medium">
                  Индекс{" "}
                  <span className="text-muted-foreground">(необязательно)</span>
                </label>
                <Input
                  id={`${id}-postal-code`}
                  inputMode="numeric"
                  placeholder="125047"
                  {...register("postalCode")}
                />
              </div>
            </div>
          </fieldset>

          <DialogFooter className="border-t pt-4">
            <DialogClose render={<Button type="button" variant="outline" />}>
              Отмена
            </DialogClose>
            <Button type="submit" disabled={isPending || !isValid}>
              Добавить локацию
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
