
export interface Quiz {
    readonly id:string;
    kind:"quiz";
    title:string;
    correctAnswers:number;
    totalQuestions:number;
}

export interface LabAssignment {
  readonly id: string;
  kind: "lab";
  title: string;
  functionalityScore: number;
  codeQualityScrore: number;
}

export type AssessmentItem = Quiz | LabAssignment;

export function calculateGrade(item:AssessmentItem): number {
    switch (item.kind){
        case "quiz":
            return Math.round((item.correctAnswers / item.totalQuestions) * 100);
            case "lab":
                return Math.round(
                    item.functionalityScore * 0.7 + item.codeQualityScrore * 0.3,
                );

    }
}