using System.Reflection;
using Temporalio.Client;
using Temporalio.Client.Schedules;
using Temporalio.Exceptions;

namespace Train.Solver.Workflow.Common.Helpers;

public static class TemporalScheduleHelper
{
    public static async Task RegisterTemporalSchedulesAsync(
       this ITemporalClient temporalClient)
    {
        var temporalJobTypes = Assembly
            .GetEntryAssembly()!
            .GetTypes()
            .Where(x => x.GetCustomAttribute<TemporalJobScheduleAttribute>() != null);

        var existingSchedules = temporalClient
            .ListSchedulesAsync()
            .ToBlockingEnumerable()
            .ToList();

        await temporalClient.RemoveNonExistingSchedulesAsync(
            temporalJobTypes, 
            existingSchedules);

        foreach (var type in temporalJobTypes)
        {
            await CreateOrUpdateScheduleAsync(
                temporalClient,
                type,
                existingSchedules);
        }
    }

    private static async Task CreateOrUpdateScheduleAsync(
        ITemporalClient temporalClient,
        Type type,
        List<ScheduleListDescription> existingSchedules)
    {
        var scheduleAttribute = type
                .GetCustomAttribute<TemporalJobScheduleAttribute>()!;

        if (!type.Name.EndsWith(nameof(Workflow)))
            return;

        var descriptor = existingSchedules.FirstOrDefault(x => x.Id == type.Name);

        if (descriptor is not null
           && !descriptor!.Schedule!.Spec.CronExpressions.Any(x => x == scheduleAttribute.Chron))
        {
            await temporalClient
                   .GetScheduleHandle(type.Name)
                   .DeleteAsync();
        }

        try
        {
            await temporalClient.CreateScheduleAsync(type.Name, new(
                Action: ScheduleActionStartWorkflow.Create(
                    type.Name,
                    args: [],
                    options: new WorkflowOptions(id: type.Name, Constants.CoreTaskQueue)),
                Spec: new()
                {
                    CronExpressions = [scheduleAttribute.Chron],
                    TimeZoneName = "Etc/UTC",
                }));
        }
        catch (ScheduleAlreadyRunningException) // ignore if multiple instances try to create the same schedule
        {
        }
    }

    private static async Task RemoveNonExistingSchedulesAsync(
        this ITemporalClient temporalClient,
        IEnumerable<Type> jobTypes,
        List<ScheduleListDescription> scheduleDescriptions)
    {
        var scheduleNames = jobTypes
            .Where(x => x.Name.EndsWith(nameof(Workflow)))
            .Select(x => x.Name)
            .ToHashSet();

        foreach (var scheduleDescription in scheduleDescriptions)
        {
            if (!scheduleNames.Contains(scheduleDescription.Id))
            {
                await temporalClient
                    .GetScheduleHandle(scheduleDescription.Id)
                    .DeleteAsync();
            }
        }

        scheduleDescriptions.RemoveAll(x => !scheduleNames.Contains(x.Id));
    }
}
