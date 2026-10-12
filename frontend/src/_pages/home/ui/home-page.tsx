import HomeHeader from "./home-header";
import HomeRelationships from "./home-relationships";
import HomeSections from "./home-sections";

export function HomePage() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center bg-muted/30 px-6 py-10 sm:py-15">
      <div className="w-full max-w-5xl">
        <HomeHeader />
        <HomeSections />
        <HomeRelationships />
      </div>
    </main>
  );
}
