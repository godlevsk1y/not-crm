import { Card, CardContent, CardFooter, CardHeader } from "@/shared/ui/card";
import { Skeleton } from "@/shared/ui/skeleton";

export function DepartmentListItemCardSkeleton() {
  return (
    <Card className="h-full shadow-sm [--card-spacing:--spacing(6)] **:data-[slot=skeleton]:motion-reduce:animate-none">
      <CardHeader className="gap-3">
        <div className="mb-2 flex items-center justify-between gap-3">
          <Skeleton className="size-10" />
          <Skeleton className="h-6 w-24" />
        </div>
        <Skeleton className="h-7 w-3/4" />
      </CardHeader>
      <CardContent className="flex-1 space-y-2">
        <Skeleton className="h-4 w-1/2" />
        <Skeleton className="h-5 w-full" />
        <Skeleton className="h-5 w-2/3" />
      </CardContent>
      <CardFooter className="mt-auto">
        <Skeleton className="h-4 w-1/2" />
      </CardFooter>
    </Card>
  );
}
