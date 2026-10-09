using System;
using UnityEngine.InputSystem;

namespace LastSignal
{
    /// <summary>Only gameplay buttons are rebindable. Navigation, look/move and UI recovery stay fixed.</summary>
    public static class InputBindingPolicy
    {
        public static bool Editable(InputAction action, InputBinding binding) =>
            action != null && (action.actionMap.name == "Player" || action.actionMap.name == "Vehicle") &&
            action.type == InputActionType.Button && action.name != "Pause" &&
            action.name != "Next" && action.name != "Previous" && action.name != "Jump" && !binding.isComposite && !binding.isPartOfComposite &&
            (binding.path.StartsWith("<Keyboard>/", StringComparison.Ordinal) ||
             binding.path.StartsWith("<Mouse>/", StringComparison.Ordinal) ||
             binding.path.StartsWith("<Gamepad>/", StringComparison.Ordinal));

        public static bool Valid(InputActionAsset asset, out string reason)
        {
            foreach (var map in asset.actionMaps)
            foreach (var action in map.actions)
            {
                bool reachable = false;
                foreach (var binding in action.bindings)
                {
                    if (!string.IsNullOrEmpty(binding.effectivePath)) reachable = true;
                    if (!binding.hasOverrides) continue;
                    if (!Editable(action, binding) || !string.IsNullOrEmpty(binding.overrideProcessors) ||
                        !string.IsNullOrEmpty(binding.overrideInteractions) || !AllowedPath(binding.path, binding.effectivePath))
                    { reason = "Bu kontrol değiştirilemez. Varsayılan bağlar geri yüklendi."; return false; }
                    foreach (var otherAction in map.actions)
                    foreach (var other in otherAction.bindings)
                        if (binding.id != other.id && otherAction != action &&
                            string.Equals(binding.effectivePath, other.effectivePath, StringComparison.OrdinalIgnoreCase))
                        { reason = "Bu tuş aynı kontrol düzeninde başka bir eyleme atanmış."; return false; }
                }
                if (!reachable) { reason = "Zorunlu bir kontrol boş bırakılamaz."; return false; }
            }
            reason = null; return true;
        }

        public static bool AllowedPath(string original, string candidate)
        {
            if (string.IsNullOrEmpty(candidate)) return false;
            bool gamepad = original.StartsWith("<Gamepad>/", StringComparison.OrdinalIgnoreCase);
            int slash = candidate.IndexOf('/');
            if (slash < 0) return false;
            string control = candidate.Substring(slash + 1);
            if (gamepad)
            {
                if (!candidate.StartsWith("<Gamepad>/", StringComparison.OrdinalIgnoreCase)) return false;
                switch (control.ToLowerInvariant())
                {
                    case "buttonsouth": case "buttonnorth": case "buttoneast": case "buttonwest":
                    case "lefttrigger": case "righttrigger": case "leftshoulder": case "rightshoulder":
                    case "leftstickpress": case "rightstickpress": case "select":
                    case "dpad/up": case "dpad/down": case "dpad/left": case "dpad/right": return true;
                    default: return false;
                }
            }
            if (candidate.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase))
                return control == "leftButton" || control == "rightButton" || control == "middleButton" || control == "forwardButton" || control == "backButton";
            return candidate.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase) &&
                Enum.TryParse<Key>(control, true, out var key) && Enum.IsDefined(typeof(Key), key) && key != Key.None && key != Key.Escape && key != Key.Enter && key != Key.NumpadEnter;
        }

        public static bool LoadSafely(InputActionAsset asset, string json, out string reason)
        {
            asset.RemoveAllBindingOverrides();
            if (string.IsNullOrWhiteSpace(json)) { reason = null; return true; }
            try
            {
                asset.LoadBindingOverridesFromJson(json);
                if (Valid(asset, out reason)) return true;
            }
            catch (Exception) { reason = "Kaydedilmiş tuş ayarları okunamadı. Varsayılanlar kullanılıyor."; }
            asset.RemoveAllBindingOverrides();
            return false;
        }
    }
}
