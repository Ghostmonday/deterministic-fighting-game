using System.Collections.Generic;

namespace NeuralDraft
{
    public static class ActionLibrary
    {
        private static Dictionary<int, ActionDef> _actions = new Dictionary<int, ActionDef>();

        public static void RegisterAction(ActionDef action)
        {
            if (!_actions.ContainsKey(action.actionId))
            {
                _actions.Add(action.actionId, action);
            }
        }

        public static ActionDef? GetAction(int actionHash)
        {
            if (_actions.TryGetValue(actionHash, out var action))
            {
                return action;
            }
            return null;
        }
    }
}
