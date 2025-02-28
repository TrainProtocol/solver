import { AppConfigurationClient, ConfigurationSetting } from "@azure/app-configuration";

export async function AddLayerswapAzureAppConfiguration(): Promise<void> {

  if (!process.env.ConnectionStrings__AppConfig) {
    throw new Error("Azure App Configuration connection string is required.");
  }

  const client = new AppConfigurationClient(process.env.ConnectionStrings__AppConfig);

  try {

    const settingsIterator = client.listConfigurationSettings();

    for await (const setting of settingsIterator) {
      if (setting.key && setting.value) {
        const normalizedKey = setting.key.replace(/:/g, '_');
        process.env[normalizedKey] = setting.value;
      }
    }

  } catch (error) {
    console.error("Failed to load configuration from Azure App Configuration:", error);
    throw error;
  }
}
