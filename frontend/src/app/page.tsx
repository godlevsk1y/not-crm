import HomeHeader from "./_components/home-header";
import HomeRelationships from "./_components/home-relationships";
import HomeSections from "./_components/home-sections";

export default function Home() {
  return (
    <main className="flex min-h-screen flex-col items-center justify-center bg-muted/30 px-6 py-24">
      <div className="w-full max-w-5xl">
        <HomeHeader />
        <HomeSections />
        <HomeRelationships />
      </div>
    </main>
  );
}
