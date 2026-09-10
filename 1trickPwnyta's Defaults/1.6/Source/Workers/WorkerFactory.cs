using Defaults.Defs;
using System;
using System.Collections.Generic;

namespace Defaults.Workers
{
    public static class WorkerFactory
    {
        private static readonly Dictionary<Type, DefaultSettingsCategoryWorker> defaultSettingsCategoryWorkers = new Dictionary<Type, DefaultSettingsCategoryWorker>();
        private static readonly Dictionary<Type, IDefaultSettingWorker> defaultSettingWorkers = new Dictionary<Type, IDefaultSettingWorker>();

        public static DefaultSettingsCategoryWorker GetWorker(DefaultSettingsCategoryDef def)
        {
            DefaultSettingsCategoryWorker worker;
            if (!defaultSettingsCategoryWorkers.ContainsKey(def.workerClass))
            {
                worker = Activator.CreateInstance(def.workerClass, new[] { def }) as DefaultSettingsCategoryWorker;
                defaultSettingsCategoryWorkers[def.workerClass] = worker;
            }
            worker = defaultSettingsCategoryWorkers[def.workerClass];
            worker.def = def;
            return worker;
        }

        public static IDefaultSettingWorker GetWorker(DefaultSettingDef def)
        {
            IDefaultSettingWorker worker;
            if (!defaultSettingWorkers.ContainsKey(def.workerClass))
            {
                worker = Activator.CreateInstance(def.workerClass, new[] { def }) as IDefaultSettingWorker;
                defaultSettingWorkers[def.workerClass] = worker;
            }
            worker = defaultSettingWorkers[def.workerClass];
            worker.Def = def;
            return worker;
        }
    }
}
