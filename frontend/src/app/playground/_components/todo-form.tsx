"use client";

import { Button } from "@/shared/ui/button";
import { Card, CardContent } from "@/shared/ui/card";
import { Input } from "@/shared/ui/input";
import { PlusIcon } from "@phosphor-icons/react";
import { useState } from "react";
import type { SubmitEvent } from "react";

type TodoFormProps = {
  onAdd: (title: string, description: string | null) => void;
};

export default function TodoForm({ onAdd }: TodoFormProps) {
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");

  const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();

    setTitle("");
    setDescription("");

    onAdd(title.trim(), description.trim());
  };

  return (
    <Card className="gap-0 rounded-2xl border-border/70 py-0 shadow-sm">
      <CardContent className="p-5 sm:p-6">
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-2">
            <label htmlFor="todo-title" className="text-sm font-medium">
              Что нужно сделать?
            </label>
            <Input
              id="todo-title"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              placeholder="Например, спланировать неделю"
              className="h-12 rounded-lg bg-muted/40 px-4 text-sm md:text-sm"
              required
              maxLength={200}
            />
          </div>
          <div className="space-y-2">
            <label htmlFor="todo-description" className="text-sm font-medium">
              Детали{" "}
              <span className="font-normal text-muted-foreground">
                · необязательно
              </span>
            </label>
            <Input
              id="todo-description"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              placeholder="Добавьте немного контекста"
              className="h-11 rounded-lg border-transparent bg-muted/40 px-4 text-sm md:text-sm"
              maxLength={500}
            />
          </div>
          <div className="flex justify-end pt-1">
            <Button
              type="submit"
              disabled={!title.trim()}
              className="h-11 gap-2 rounded-lg px-5 text-sm"
            >
              <PlusIcon />
              Добавить задачу
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
