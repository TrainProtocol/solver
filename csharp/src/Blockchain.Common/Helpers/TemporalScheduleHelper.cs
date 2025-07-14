using System.Reflection;
using Temporalio.Client;
using Temporalio.Client.Schedules;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace Train.Solver.Blockchain.Common.Helpers;

public static class TemporalScheduleHelper
{
    public static async Task RegisterTemporalSchedulesAsync(
       this ITemporalClient temporalClient)
    {
        var temporalJobTypes = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .Where(x => x.GetCustomAttribute<TemporalJobScheduleAttribute>() != null);

        var existingSchedules = temporalClient
            .ListSchedulesAsync()
            .ToBlockingEnumerable()
            .ToList();

        await RemoveNonExistingSchedulesAsync(
            temporalClient, 
            temporalJobTypes, 
            existingSchedules);

        foreach (var type in temporalJobTypes)
        {
            var scheduleAttribute = type
                .GetCustomAttribute<TemporalJobScheduleAttribute>()!;

            await CreateOrUpdateScheduleAsync(
                temporalClient,
                type,
                scheduleAttribute,
                existingSchedules);
        }
    }

    private static async Task CreateOrUpdateScheduleAsync(
        ITemporalClient temporalClient,
        Type type,
        TemporalJobScheduleAttribute scheduleAttribute,
        List<ScheduleListDescription> existingSchedules)
    {
        if (!type.Name.EndsWith(nameof(Workflow)))
            return;

        var scheduleName = type.Name[..^nameof(Workflow).Length];

        var descriptor = existingSchedules.FirstOrDefault(x => x.Id == scheduleName);

        if (descriptor is not null
           && !descriptor!.Schedule!.Spec.CronExpressions.Any(x => x == scheduleAttribute.Chron))
        {
            await temporalClient
                   .GetScheduleHandle(scheduleName)
                   .DeleteAsync();
        }

        try
        {
            await temporalClient.CreateScheduleAsync(scheduleName, new(
                Action: ScheduleActionStartWorkflow.Create(
                    scheduleName,
                    args: [],
                    options: new WorkflowOptions(id: scheduleName, Constants.CoreTaskQueue)),
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
            .Select(x => x.Name[..^nameof(Workflow).Length])
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
