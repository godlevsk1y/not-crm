"use client";

import { useState } from "react";

import { Button } from "@/shared/ui/button";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/shared/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
  DialogTrigger,
} from "@/shared/ui/dialog";
import type { LocationSelectOption } from "../model/types";
import { LocationSelect } from "./location-select";

export function LocationSelectPlayground() {
  const [single, setSingle] = useState<LocationSelectOption | null>(null);
  const [multiple, setMultiple] = useState<LocationSelectOption[]>([]);
  const [modalValue, setModalValue] = useState<LocationSelectOption[]>([]);
  const [disabled, setDisabled] = useState(false);

  return (
    <main className="mx-auto grid w-full max-w-5xl gap-6 px-6 py-10">
      <h1 className="text-3xl font-bold">Выбор локаций</h1>
      <label className="flex items-center gap-2 text-sm">
        <input
          type="checkbox"
          checked={disabled}
          onChange={(event) => setDisabled(event.target.checked)}
        />
        Отключить поля
      </label>
      <div className="grid items-start gap-6 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Одна локация</CardTitle>
          </CardHeader>
          <CardContent>
            <LocationSelect
              mode="single"
              label="Локация"
              value={single}
              onChange={setSingle}
              disabled={disabled}
            />
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Несколько локаций</CardTitle>
          </CardHeader>
          <CardContent>
            <LocationSelect
              mode="multiple"
              value={multiple}
              onChange={setMultiple}
              placeholder="Выберите локации"
              disabled={disabled}
            />
          </CardContent>
        </Card>
      </div>
      <Dialog>
        <DialogTrigger render={<Button type="button" variant="outline" />}>
          Выбор в модальном окне
        </DialogTrigger>
        <DialogContent>
          <DialogTitle>Локации департамента</DialogTitle>
          <DialogDescription>
            Выберите локации для нового департамента.
          </DialogDescription>
          <LocationSelect
            mode="multiple"
            label="Локации департамента"
            value={modalValue}
            onChange={setModalValue}
            placeholder="Выберите локации"
            disabled={disabled}
          />
        </DialogContent>
      </Dialog>
    </main>
  );
}
