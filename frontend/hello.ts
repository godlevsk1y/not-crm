// Задание 1. Система управления космическим кораблём

interface Spacecraft {
  name: string;
  readonly model: string;
  fuel: number;
}

interface CargoShip extends Spacecraft {
  cargoCapacity: number;
}

const cargoShip: CargoShip = {
  name: "Орион",
  model: "CS-100",
  fuel: 85,
  cargoCapacity: 500,
};

function printShipInfo(ship: CargoShip): void {
  console.log(
    `Корабль: ${ship.name}, модель: ${ship.model}, ` +
      `топливо: ${ship.fuel}%, грузоподъёмность: ${ship.cargoCapacity} т`,
  );
}

printShipInfo(cargoShip);

// cargoShip.model = "CS-200";

function analyzeMessage(value: string | number): number {
  if (typeof value === "string") {
    return value.length;
  }

  return value ** 2;
}

console.log(analyzeMessage("Привет")); // 6
console.log(analyzeMessage(7)); // 49

// Задание 8. Проверка данных космической экспедиции

interface CrewMember {
  name: string;
  specialization: string;
}

function isCrewMember(value: unknown): value is CrewMember {
  return (
    typeof value === "object" &&
    value !== null &&
    "name" in value &&
    typeof value.name === "string" &&
    "specialization" in value &&
    typeof value.specialization === "string"
  );
}

const expeditionData: unknown[] = [
  { name: "Анна", specialization: "Пилот" },
  { name: "Иван", specialization: 123 },
  null,
];

for (const value of expeditionData) {
  if (isCrewMember(value)) {
    console.log(`Участник: ${value.name}, роль: ${value.specialization}`);
  } else {
    console.log("Некорректные данные участника");
  }
}

// Задание 9. Редактор персонажа для ролевой игры

interface GameCharacter {
  name: string;
  className: string;
  level: number;
  health: number;
}

function updateCharacter(
  character: GameCharacter,
  changes: Partial<GameCharacter>,
): GameCharacter {
  return { ...character, ...changes };
}

const hero: GameCharacter = {
  name: "Альрик",
  className: "Воин",
  level: 1,
  health: 100,
};

const leveledHero = updateCharacter(hero, { level: 2 });
console.log(leveledHero);

const updatedHero = updateCharacter(leveledHero, {
  level: 3,
  health: 120,
});
console.log(updatedHero);

// Задание 10. Управление библиотекой заклинаний

interface Spell {
  name: string;
  element: string;
  manaCost: number;
  description: string;
}

type SpellCard = Pick<Spell, "name" | "element">;
type SpellWithoutDescription = Omit<Spell, "description">;

const fireCard: SpellCard = {
  name: "Огненный шар",
  element: "Огонь",
};

const iceCard: SpellCard = {
  name: "Ледяная стрела",
  element: "Лёд",
};

const lightning: SpellWithoutDescription = {
  name: "Удар молнии",
  element: "Воздух",
  manaCost: 30,
};

const stoneShield: SpellWithoutDescription = {
  name: "Каменный щит",
  element: "Земля",
  manaCost: 20,
};

function printSpellInfo(spell: SpellCard): void {
  console.log(`Заклинание: ${spell.name}, стихия: ${spell.element}`);
}

printSpellInfo(fireCard);
printSpellInfo(iceCard);

printSpellInfo(lightning);
printSpellInfo(stoneShield);
