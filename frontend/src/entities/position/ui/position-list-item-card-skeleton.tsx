import { Card, CardFooter, CardHeader } from "@/shared/ui/card";
import { Skeleton } from "@/shared/ui/skeleton";

export function PositionListItemCardSkeleton() {
  return (
    <Card className="h-full shadow-sm [--card-spacing:--spacing(6)] **:data-[slot=skeleton]:motion-reduce:animate-none">
      <CardHeader className="gap-3">
        <Skeleton className="mb-2 size-10" />
        <Skeleton className="h-7 w-3/4" />
      </CardHeader>
      <CardFooter className="mt-auto">
        <Skeleton className="h-4 w-1/2" />
      </CardFooter>
    </Card>
  );
}
