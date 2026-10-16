"use client";

import { useId } from "react";
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

export function CreateLocationDialog() {
  const id = useId();

  const handleSubmit = (e: React.SubmitEvent<HTMLFormElement>) => {
    e.preventDefault();
  };

  return (
    <Dialog>
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

        <form className="grid gap-6" onSubmit={handleSubmit}>
          <div className="grid gap-2">
            <label htmlFor={`${id}-name`} className="font-medium">
              Название локации
            </label>
            <Input
              id={`${id}-name`}
              name="name"
              placeholder="Например, Центральный офис"
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
                  name="country"
                  placeholder="Россия"
                />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-region`} className="font-medium">
                  Регион{" "}
                  <span className="text-muted-foreground">(необязательно)</span>
                </label>
                <Input
                  id={`${id}-region`}
                  name="region"
                  placeholder="Московская область"
                />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-city`} className="font-medium">
                  Город
                </label>
                <Input id={`${id}-city`} name="city" placeholder="Москва" />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-district`} className="font-medium">
                  Район{" "}
                  <span className="text-muted-foreground">(необязательно)</span>
                </label>
                <Input
                  id={`${id}-district`}
                  name="district"
                  placeholder="Тверской"
                />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-street`} className="font-medium">
                  Улица
                </label>
                <Input id={`${id}-street`} name="street" placeholder="Лесная" />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-house-number`} className="font-medium">
                  Дом
                </label>
                <Input
                  id={`${id}-house-number`}
                  name="houseNumber"
                  placeholder="7, корпус 1"
                />
              </div>
              <div className="grid gap-2">
                <label htmlFor={`${id}-postal-code`} className="font-medium">
                  Индекс{" "}
                  <span className="text-muted-foreground">(необязательно)</span>
                </label>
                <Input
                  id={`${id}-postal-code`}
                  name="postalCode"
                  inputMode="numeric"
                  placeholder="125047"
                />
              </div>
            </div>
          </fieldset>

          <DialogFooter className="border-t pt-4">
            <DialogClose render={<Button type="button" variant="outline" />}>
              Отмена
            </DialogClose>
            <Button type="submit">Добавить локацию</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
