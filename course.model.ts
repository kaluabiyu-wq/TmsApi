import { Temporal } from "@js-temporal/polyfill";
export interface Course {
  readonly id: string;
  title: string;
  capacity: number;
  startDate?: Temporal.PlainDate;
}


export type CourseStatus =
  | { status: "DRAFT"; createdBy: string; createdAt: Temporal.Instant }
  | { status: "PUBLISHED"; publishedAt: Temporal.Instant; syllabus: string }
  | { status: "ACTIVE"; enrolledCount: number; startDate: Temporal.PlainDate }
  | {
      status: "ARCHIVED";
      finalEnrollmentCount: number;
      archievedAt: Temporal.Instant;
    }
  | { status: "CANCELLED"; reason: string; cancelledAt: Temporal.Instant };


  export function describeCourse(coursestatus:CourseStatus): string 
  {
    switch (coursestatus.status) {
      case "DRAFT":
        return `DRAFT since ${coursestatus.createdBy}`;
      case "PUBLISHED":
        return `Published by ${coursestatus.publishedAt}`;
      case "ACTIVE":
        return coursestatus.enrolledCount !== undefined
          ? `in progress courses so far :${coursestatus.enrolledCount}`
          : `in progress not yet courses`;
      case "ARCHIVED":
        return `Archived with ${coursestatus.archievedAt}`;
      case "CANCELLED":
        return `Cancelled: ${coursestatus.cancelledAt}`;
        default: {
          const _check: never =coursestatus;
          throw new Error (`Unhandeled status:${JSON.stringify(_check)}`);

        };
      }
  }

