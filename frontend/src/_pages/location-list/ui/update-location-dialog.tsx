"use client";

import {
  LocationFormValues,
  LocationListItemDto,
  locationSchema,
} from "@/entities/location";
import { Button } from "@/shared/ui/button";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/shared/ui/dialog";
import { Input } from "@/shared/ui/input";
import { zodResolver } from "@hookform/resolvers/zod";
import { MapPinIcon } from "@phosphor-icons/react";
import { useId } from "react";
import { FieldPath, useForm } from "react-hook-form";
import { useUpdateLocation } from "../model/use-update-location";
import { ApiRequestError } from "@/shared/api";
import { applyServerErrors } from "@/shared/lib/react-hook-form";

const fieldNameMap = new Map<string, FieldPath<LocationFormValues>>([
  ["LocationName", "name"],
  ["Country", "country"],
  ["City", "city"],
  ["Street", "street"],
  ["HouseNumber", "houseNumber"],
]);

const errorCodeMessagesMap = new Map<string, string>([
  ["location.name.empty", "Укажите название локации"],
  ["location.name.long", "Название локации слишком длинное"],
  ["address.country.empty", "Укажите страну"],
  ["address.city.empty", "Укажите город"],
  ["address.street.empty", "Укажите улицу"],
  ["address.housenumber.empty", "Укажите номер дома"],
]);

export function UpdateLocationDialog({
  location,
  onClose,
}: {
  location: LocationListItemDto;
  onClose: () => void;
}) {
  const id = useId();

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isValid },
  } = useForm<LocationFormValues>({
    resolver: zodResolver(locationSchema),
    mode: "onTouched",
    defaultValues: {
      name: location.name,
      country: location.address.country,
      region: location.address.region ?? "",
      city: location.address.city,
      district: location.address.district ?? "",
      street: location.address.street,
      houseNumber: location.address.houseNumber,
      postalCode: location.address.postalCode ?? "",
    },
  });

  const { updateLocationFunc, isPending } = useUpdateLocation();

  const onSubmit = (values: LocationFormValues) => {
    updateLocationFunc(
      {
        id: location.id,
        ...values,
        region: values.region || undefined,
        district: values.district || undefined,
        postalCode: values.postalCode || undefined,
      },
      {
        onSuccess: () => {
          reset();
          onClose();
        },
        onError: (error) => {
          if (error instanceof ApiRequestError) {
            applyServerErrors({
              setErrors: setError,
              error: error,
              fieldNameMap: fieldNameMap,
              errorCodeMessagesMap: errorCodeMessagesMap,
            });
          }
        },
      },
    );
  };

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open) onClose();
      }}
    >
      <DialogContent className="max-h-[calc(100dvh-2rem)] gap-6 overflow-y-auto p-6 sm:max-w-xl">
        <DialogHeader className="gap-3 pr-6">
          <span className="flex size-10 shrink-0 items-center justify-center bg-sky-500/10 text-sky-600 dark:text-sky-400">
            <MapPinIcon size={24} weight="duotone" aria-hidden="true" />
          </span>
          <DialogTitle className="text-xl font-semibold tracking-tight">
            Редактировать локацию
          </DialogTitle>
          <DialogDescription>
            Уточните название и адрес офиса или рабочего пространства.
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
              aria-invalid={Boolean(errors.name)}
              aria-describedby={errors.name ? `${id}-name-error` : undefined}
            />
            <p
              id={`${id}-name-error`}
              role={errors.name ? "alert" : undefined}
              className="h-4 text-xs leading-4 text-destructive"
            >
              {errors.name?.message}
            </p>
          </div>

          <fieldset className="min-w-0 border-t pt-4">
            <legend className="pr-3 text-sm font-medium">Адрес</legend>
            <div className="grid items-start gap-4 sm:grid-cols-2">
              <div className="grid gap-2">
                <label htmlFor={`${id}-country`} className="font-medium">
                  Страна
                </label>
                <Input
                  id={`${id}-country`}
                  placeholder="Россия"
                  {...register("country")}
                  aria-invalid={Boolean(errors.country)}
                  aria-describedby={
                    errors.country ? `${id}-country-error` : undefined
                  }
                />
                <p
                  id={`${id}-country-error`}
                  role={errors.country ? "alert" : undefined}
                  className="h-4 text-xs leading-4 text-destructive"
                >
                  {errors.country?.message}
                </p>
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
                  aria-invalid={Boolean(errors.region)}
                  aria-describedby={
                    errors.region ? `${id}-region-error` : undefined
                  }
                />
                <p
                  id={`${id}-region-error`}
                  role={errors.region ? "alert" : undefined}
                  className="h-4 text-xs leading-4 text-destructive"
                >
                  {errors.region?.message}
                </p>
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-city`} className="font-medium">
                  Город
                </label>
                <Input
                  id={`${id}-city`}
                  placeholder="Москва"
                  {...register("city")}
                  aria-invalid={Boolean(errors.city)}
                  aria-describedby={
                    errors.city ? `${id}-city-error` : undefined
                  }
                />
                <p
                  id={`${id}-city-error`}
                  role={errors.city ? "alert" : undefined}
                  className="h-4 text-xs leading-4 text-destructive"
                >
                  {errors.city?.message}
                </p>
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
                  aria-invalid={Boolean(errors.district)}
                  aria-describedby={
                    errors.district ? `${id}-district-error` : undefined
                  }
                />
                <p
                  id={`${id}-district-error`}
                  role={errors.district ? "alert" : undefined}
                  className="h-4 text-xs leading-4 text-destructive"
                >
                  {errors.district?.message}
                </p>
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-street`} className="font-medium">
                  Улица
                </label>
                <Input
                  id={`${id}-street`}
                  placeholder="Лесная"
                  {...register("street")}
                  aria-invalid={Boolean(errors.street)}
                  aria-describedby={
                    errors.street ? `${id}-street-error` : undefined
                  }
                />
                <p
                  id={`${id}-street-error`}
                  role={errors.street ? "alert" : undefined}
                  className="h-4 text-xs leading-4 text-destructive"
                >
                  {errors.street?.message}
                </p>
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-house-number`} className="font-medium">
                  Дом
                </label>
                <Input
                  id={`${id}-house-number`}
                  placeholder="7, корпус 1"
                  {...register("houseNumber")}
                  aria-invalid={Boolean(errors.houseNumber)}
                  aria-describedby={
                    errors.houseNumber ? `${id}-house-number-error` : undefined
                  }
                />
                <p
                  id={`${id}-house-number-error`}
                  role={errors.houseNumber ? "alert" : undefined}
                  className="h-4 text-xs leading-4 text-destructive"
                >
                  {errors.houseNumber?.message}
                </p>
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
                  aria-invalid={Boolean(errors.postalCode)}
                  aria-describedby={
                    errors.postalCode ? `${id}-postal-code-error` : undefined
                  }
                />
                <p
                  id={`${id}-postal-code-error`}
                  role={errors.postalCode ? "alert" : undefined}
                  className="h-4 text-xs leading-4 text-destructive"
                >
                  {errors.postalCode?.message}
                </p>
              </div>
            </div>
          </fieldset>

          <DialogFooter className="border-t pt-4">
            <DialogClose render={<Button type="button" variant="outline" />}>
              Отмена
            </DialogClose>
            <Button type="submit" disabled={isPending || !isValid}>
              Обновить локацию
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
