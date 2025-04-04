import { ActivityOptions} from "@temporalio/workflow";

export const defaultActivityOptions: ActivityOptions = {
    scheduleToCloseTimeout: "2 days",
    startToCloseTimeout: "1 hour",
  };