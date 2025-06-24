// This file is auto-generated. Do not modify it manually.
// Generated at: 2025-06-24 18:36:12
using UnityEngine;

namespace GMTools.Generated
{
    public static class GMCommandRegistry
    {
        public static void RegisterAllCommands(GMCommandManager manager)
        {
            if (manager == null) return;

            manager.RegisterCommand(new AddItemCommand());
            manager.RegisterCommand(new HelpCommand());
            manager.RegisterCommand(new SetLevelCommand());
        }
    }
}
