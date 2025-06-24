// This file is auto-generated. Do not modify it manually.
// Generated at: 2025-06-24 18:50:43
using UnityEngine;

namespace GameLogic
{
    public static class GMCommandRegistry
    {
        public static void RegisterAllCommands(GMCommandManager manager)
        {
            if (manager == null) return;

            manager.RegisterCommand(new AddItemCommand());
            manager.RegisterCommand(new AddValue01Command());
            manager.RegisterCommand(new HelpCommand());
            manager.RegisterCommand(new SetLevelCommand());
            manager.RegisterCommand(new SetValue01Command());
            manager.RegisterCommand(new TriggerValue01Command());
        }
    }
}
