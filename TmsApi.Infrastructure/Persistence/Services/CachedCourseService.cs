using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;

using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Caching;


namespace TmsApi.Infrastructure.Persistence.Services;

public class CachedCourseService(
    HybridCache cache,ICourseService service,
    ILogger<CachedCourseService> logger
) : ICachedCourseService
{
    public async Task<CourseResponseDto> GetCourseAsync(string code, CancellationToken ct)
    {
        var key = CacheKeys.Course(code);
        var dbHit = false;

        var dto = await cache.GetOrCreateAsync(
            key,
            (service, code),
            async (state, token) =>
            {
                dbHit = true;
                logger.LogInformation("Cache MISS for {Key}, fetching from DB", key);

                var course = await state.service.GetByCodeAsync(state.code, token)
                    ?? throw new KeyNotFoundException($"Course {state.code} not found");

                return new CourseResponseDto(
                    course.Id, course.Code, course.Title, course.MaxCapacity, course.EnrollmentCount);
            },
            tags: [CacheKeys.CoursesTag],
            cancellationToken: ct);

        if (!dbHit)
        {
            logger.LogInformation("Cache HIT for {Key}", key);
        }

        return dto;
    }
    public async Task<List<CourseResponseDto>> GetAllCoursesAsync(CancellationToken ct)
    {
        var key = CacheKeys.CoursesAll;
        var dbHit = false;

        var list = await cache.GetOrCreateAsync(key,service,
        async (state,token)=>
        {
            dbHit = true;
            logger.LogInformation("Cache MISS for {key} fetching from DB",key);

            var courses = await state.GetAllAsync(token);
            return courses.Select(c=> new CourseResponseDto(
                c.Id,c.Code,c.Title,c.MaxCapacity,c.EnrollmentCount)).ToList();
            
        },
         tags: [CacheKeys.CoursesTag],
              cancellationToken: ct);
        
             if(!dbHit)
             logger.LogInformation("Cache HIT for {key}",key);
             return list;
    }

    public async Task InvalidateCourseCachedAsync(CancellationToken ct)
    {
        logger.LogInformation("Invalidating Cache tag {Tag}",CacheKeys.CoursesTag);
        await cache.RemoveByTagAsync(CacheKeys.CoursesTag,ct);
    }



}