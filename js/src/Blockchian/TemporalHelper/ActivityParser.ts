export function extractActivities(instance: any): Record<string, any> {
    const activities: Record<string, any> = {};
  
    const proto = Object.getPrototypeOf(instance);
    const methodNames = Object.getOwnPropertyNames(proto)
      .filter(name => typeof instance[name] === 'function' && name !== 'constructor');
  
    for (const methodName of methodNames) {
      activities[methodName] = instance[methodName].bind(instance);
    }
  
    return activities;
  }
  