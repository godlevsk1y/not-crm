import { TodoEntry } from "../page";
import { ChecksIcon } from "@phosphor-icons/react";
import TodoCard from "./todo-card";

type TodoListProps = {
  todos: TodoEntry[];
  onCardToggle: (id: string, checked: boolean) => void;
  onCardDelete: (id: string) => void;
};

export default function TodoList({
  todos,
  onCardToggle,
  onCardDelete,
}: TodoListProps) {
  const completedCount = todos.filter((todo) => todo.completed).length;

  return (
    <section aria-labelledby="todo-list-heading" className="mt-10">
      <div className="mb-4 flex items-center justify-between gap-4">
        <h2 id="todo-list-heading" className="text-sm font-medium">
          Список задач
        </h2>
        <span role="status" className="text-xs text-muted-foreground">
          {completedCount} из {todos.length} выполнено
        </span>
      </div>
      {todos.length > 0 ? (
        <>
          <div
            role="progressbar"
            aria-label="Выполнение задач"
            aria-valuemin={0}
            aria-valuemax={todos.length}
            aria-valuenow={completedCount}
            className="mb-6 h-1 overflow-hidden rounded-full bg-muted"
          >
            <div
              className="h-full rounded-full bg-emerald-500 transition-[width] duration-300 motion-reduce:transition-none"
              style={{ width: `${(completedCount / todos.length) * 100}%` }}
            />
          </div>
          <ul className="space-y-3">
            {todos.map((todo) => (
              <li key={todo.id}>
                <TodoCard
                  todo={todo}
                  onToggle={onCardToggle}
                  onDelete={onCardDelete}
                />
              </li>
            ))}
          </ul>
        </>
      ) : (
        <div className="flex flex-col items-center rounded-2xl border border-dashed border-border px-6 py-12 text-center">
          <div className="mb-4 flex size-12 items-center justify-center rounded-full bg-emerald-50 text-emerald-600 dark:bg-emerald-950">
            <ChecksIcon className="size-6" />
          </div>
          <h3 className="text-sm font-medium">Всё начинается с одной задачи</h3>
          <p className="mt-2 max-w-xs text-sm leading-relaxed text-muted-foreground">
            Добавьте первую задачу выше. Даже маленький шаг — это движение
            вперёд.
          </p>
        </div>
      )}
    </section>
  );
}
