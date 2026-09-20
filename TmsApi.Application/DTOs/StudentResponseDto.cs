



namespace TmsApi.Application.DTOs;
public record StudentResponseDto(
  int ID,
 string RegistrationNumber,
string Name,
   decimal GPA,
   bool IsActive
   );