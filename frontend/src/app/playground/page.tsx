"use client";

import { useState } from "react";
import TodoForm from "./_components/todo-form";
import TodoList from "./_components/todo-list";

export type TodoEntry = {
  id: string;
  title: string;
  description: string | null;
  completed: boolean;
};

export default function Todo() {
  const [todos, setTodos] = useState<TodoEntry[]>([]);

  const handleToggle = (id: string, checked: boolean) => {
    setTodos((prev) =>
      prev.map((todo) =>
        todo.id === id ? { ...todo, completed: checked } : todo,
      ),
    );
  };

  const handleAdd = (title: string, description: string | null) => {
    const newEntry: TodoEntry = {
      id: crypto.randomUUID(),
      title: title.trim(),
      description: description?.trim() ?? null,
      completed: false,
    };

    setTodos((prev) => [...prev, newEntry]);
  };

  const handleDelete = (id: string) => {
    setTodos((prev) => prev.filter((todo) => todo.id !== id));
  };

  return (
    <main className="min-h-screen bg-stone-50/70 px-5 py-12 font-sans sm:px-8 sm:py-20 dark:bg-background">
      <div className="mx-auto max-w-2xl">
        <TodoHeader />

        <TodoForm onAdd={handleAdd} />

        <TodoList
          todos={todos}
          onCardToggle={handleToggle}
          onCardDelete={handleDelete}
        />

        <TodoNotice />
      </div>
    </main>
  );
}

function TodoHeader() {
  return (
    <header className="mb-10">
      <div className="mb-6 flex items-center gap-2 text-xs font-medium tracking-[0.18em] text-muted-foreground uppercase">
        <span className="size-2 rounded-full bg-emerald-500" />
        Личное пространство
      </div>
      <h1 className="text-4xl font-semibold tracking-tight sm:text-5xl">
        Мои задачи<span className="text-emerald-600">.</span>
      </h1>
      <p className="mt-4 text-sm leading-relaxed text-muted-foreground sm:text-base">
        Освободите голову. Запишите важное и двигайтесь шаг за шагом.
      </p>
    </header>
  );
}

function TodoNotice() {
  return (
    <p className="mt-8 text-center text-xs leading-relaxed text-muted-foreground/75">
      Задачи хранятся до перезагрузки страницы.
    </p>
  );
}
