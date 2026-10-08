import { Button } from "@/shared/ui/button";

export default function Home() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center gap-4 p-6">
      <h1 className="text-4xl font-bold tracking-tight">Directory Service</h1>

      <p className="text-muted-foreground">Тут будут разделы</p>

      <Button variant="outline" disabled>
        Скоро появятся разделы
      </Button>
    </main>
  );
}
