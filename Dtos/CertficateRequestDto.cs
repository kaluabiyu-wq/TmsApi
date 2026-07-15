
using System.ComponentModel.DataAnnotations;

namespace Tms.Api.Dtos;

public record CreateCertficateRequest
{
    [Required, MaxLength(200)]
   public required string SerialNumber { get; set;}

    public  int StudentId { get;set;}

}