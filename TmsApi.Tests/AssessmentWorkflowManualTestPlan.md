# Phase 4 assessment workflow verification checklist

Use a test database and test accounts with Admin, Instructor, and Student roles.

1. Instructor creates a draft assessment for a course they teach.
2. Instructor cannot create or edit an assessment for another instructor's course.
3. Assessment creation rejects a close time earlier than or equal to the open time.
4. Instructor adds multiple-choice, true/false, short-answer, and essay questions.
5. Publishing fails when no questions exist.
6. Publishing fails when question points do not sum to `MaxScore`.
7. Publishing succeeds when the assessment is valid.
8. Student who is not linked to a Student record cannot start an attempt.
9. Student who is not actively enrolled with `Approved` status cannot start an attempt.
10. Student cannot see a draft assessment or its answer key.
11. Student cannot start before `OpensAt` or at/after `ClosesAt`.
12. Student can start an assessment and resume the same in-progress attempt.
13. Student cannot exceed `MaxAttempts`.
14. Student cannot read or update another student's attempt.
15. Student can save answers and submit.
16. Objective questions are automatically marked; essay answers remain pending.
17. Repeated submission requests return the same submission.
18. Instructor can view submissions and grade pending answers.
19. Grade values below zero or above the question's points are rejected.
20. Once all answers have marks, the submission status becomes `Graded` and its score is the sum of awarded points.
