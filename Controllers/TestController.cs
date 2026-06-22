
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using TmsApi.Data;

namespace TmsApi.Controllers;
[ApiController]
[Route("api/test")]
public class TestController(TmSDbContext context) : ControllerBase
{
    [HttpGet("deferred")]
    public IActionResult TestDeferred()
    {
        Console.WriteLine("\n>>> STEP 1: Building the query object (no database contact)...");
        var query = context.Students.Where(s => s.GPA >= 3.0m);

        Console.WriteLine(">>> STEP 2: Appending a sorting clause...");
        var orderedQuery = query.OrderBy( s=>s.Name);
        Console.WriteLine(">>> STEP 3: Materializing query into a C# List...");

        var results = orderedQuery.ToList();

        Console.WriteLine(">>> STEP 4: Materialization finished. List populated.\n");

        return Ok(results);
    }
    private static bool IsHonorRoll(decimal gpa)
    {
        return gpa >= 3.5m;
    }

    [HttpGet("translation-fail")]
    public IActionResult TestTranslationFail()
    {
        Console.WriteLine("\n>>> STEP 1: Running non-translatable query...");
        try
        {
            var students =context.Students
            .Where(s => IsHonorRoll(s.GPA)).ToList();
            return Ok(students);
        }
        catch(Exception ex)
        {
            Console.WriteLine($">>> EXCEPTION CAUGHT: {ex.Message}\n");
            return BadRequest (new { Message = ex.Message});
        }
    }
    [HttpGet("honor-roll-server")]
    public IActionResult HonorRollServer()
    {
        Console.WriteLine("\n>> SERVER-SIDE: Query with inline Logic...");
        var students = context.Students
        .Where(s=> s.GPA >=3.5m).ToList();

    Console.WriteLine($">>> SERVER-SIDE: {students.Count} students returned.\n");

    return Ok(students);
    }
    [HttpGet("honor-roll-client")]
    public IActionResult HonorRollClient()
    {
        Console.WriteLine("\n>> CLIENT-SIDE: Pulling ALL rows into RAM first...");
        var students = context.Students
        .AsEnumerable()
        .Where(s => IsHonorRoll(s.GPA))
        .ToList();
       Console.WriteLine($">>> CLIENT-SIDE: {students.Count} students returned.\n");
       return Ok(students);
    }


}