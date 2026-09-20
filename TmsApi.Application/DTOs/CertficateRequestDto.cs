
using System.ComponentModel.DataAnnotations;

namespace TmsApi.Application.DTOs;

public record CreateCertficateRequest
{
    [Required, MaxLength(200)]
   public required string SerialNumber { get; set;}

    public  int StudentId { get;set;}

}