import {
  BriefcaseIcon,
  MapPinIcon,
  TreeStructureIcon,
} from "@phosphor-icons/react/ssr";
import { ROUTES } from "@/shared/config/routes";
import HomeSectionCard from "./home-section-card";

const sections = [
  {
    href: ROUTES.DEPARTMENTS,
    title: "Подразделения",
    description:
      "Структура организации: подразделения, их иерархия и связи с локациями и должностями.",
    icon: TreeStructureIcon,
    accent: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
  },
  {
    href: ROUTES.LOCATIONS,
    title: "Локации",
    description:
      "Места работы с названиями и адресами, связанные с подразделениями организации.",
    icon: MapPinIcon,
    accent: "bg-sky-500/10 text-sky-600 dark:text-sky-400",
  },
  {
    href: ROUTES.POSITIONS,
    title: "Должности",
    description: "Справочник должностей организации.",
    icon: BriefcaseIcon,
    accent: "bg-violet-500/10 text-violet-600 dark:text-violet-400",
  },
] as const;

export default function HomeSections() {
  return (
    <nav aria-label="Разделы справочника" className="grid gap-5 md:grid-cols-3">
      {sections.map((section) => (
        <HomeSectionCard key={section.href} {...section} />
      ))}
    </nav>
  );
}
