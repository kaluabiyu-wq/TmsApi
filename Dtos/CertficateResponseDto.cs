
public record CertficateResponseDto(
    int Id,
    string SerialNumber,
    DateTime IsuedAt,
    int StudentId,
    int CourseId
);