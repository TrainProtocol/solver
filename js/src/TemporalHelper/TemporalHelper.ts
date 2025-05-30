import { ActivityOptions } from "@temporalio/workflow";

export function defaultActivityOptions(taskQueue?: string): ActivityOptions {
  return {
    scheduleToCloseTimeout: '2 days',
    startToCloseTimeout: '1 hour',
    taskQueue,
  };
}