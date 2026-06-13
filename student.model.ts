import { Temporal } from "@js-temporal/polyfill";
export interface Student {
  readonly id: string;
  name: string;
  enrollmentDate: Temporal.Instant;
  gpa?: number;
}
export function isStudent(value: unknown): value is Student {
  return (
    typeof value === "object" &&
    value !== null &&
    "id" in value &&
    "name" in value &&
    typeof (value as Record<string, unknown>).id === "string" &&
    typeof (value as Record<string, unknown>).name === "string"
  );
}

export function parseStudent(raw: unknown): Student {
  if (typeof raw !== "object" || raw === null) {
    throw new TypeError(
      `Expected an object, received ${raw === null ? "null" : typeof raw}`,
    );
  }
  const obj = raw as Record<string, unknown>;
  if (typeof obj.id !== "string") {
    throw new TypeError(
      `Expected id to be a string, received ${typeof obj.id}`,
    );
  }
  if (typeof obj.name !== "string") {
    throw new TypeError(
      `Expected name to be a string, received ${typeof obj.name}`,
    );
  }
  return {
    id: obj.id,
    name: obj.name,
    enrollmentDate: Temporal.Now.instant(),
  };
}
