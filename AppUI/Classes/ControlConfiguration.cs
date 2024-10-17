using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace AppUI.Classes
{
    public class ControlConfiguration
    {
        public Dictionary<GameControl, ControlInputSetting> KeyboardInputs;
        public Dictionary<GameControl, ControlInputSetting> GamepadInputs;

        public ControlConfiguration()
        {
            KeyboardInputs = new Dictionary<GameControl, ControlInputSetting>();
            GamepadInputs = new Dictionary<GameControl, ControlInputSetting>();
        }

        internal void SetKeyboardInput(GameControl control, Key key)
        {
            RemoveExistingKeyboardBinding(key); // check if this key is binded to another control and remove it there first

            if (KeyboardInputs.ContainsKey(control))
            {
                KeyboardInputs[control] = ControlMapper.GetControlInputFromKey(key);
            }
            else
            {
                KeyboardInputs.Add(control, ControlMapper.GetControlInputFromKey(key));
            }
        }

        internal bool IsButtonBinded(GamePadButton button)
        {
            if (GamepadInputs == null || GamepadInputs?.Count == 0)
            {
                return false;
            }

            return GamepadInputs.Values.Any(c => c != null && c.GamepadInput.HasValue && c.GamepadInput.Value == button);
        }

        internal void SetControllerInput(GameControl control, GamePadButton button)
        {
            RemoveExistingGamepadBinding(button); // check if this button is binded to another control and remove it there first

            if (GamepadInputs.ContainsKey(control))
            {
                GamepadInputs[control] = ControlMapper.GetControlInputFromButton(button);
            }
            else
            {
                GamepadInputs.Add(control, ControlMapper.GetControlInputFromButton(button));
            }
        }

        internal void SetKeyboardInput(GameControl control, ControlInputSetting newSetting)
        {
            RemoveExistingKeyboardBinding(newSetting.ConfigValue); // check if this key is binded to another control and remove it there first


            if (KeyboardInputs.ContainsKey(control))
            {
                KeyboardInputs[control] = newSetting;
            }
            else
            {
                KeyboardInputs.Add(control, newSetting);
            }
        }

        /// <summary>
        /// Looks for the game control that <paramref name="key"/> is binded to and removes it by setting it to null;
        /// </summary>
        /// <param name="key"></param>
        private void RemoveExistingKeyboardBinding(Key key)
        {
            // 'numpadenter' and 'return' have same key code so exclude 'numpadenter' from this where clause since it is handled differently (see other RemoveExistingBinding method)
            var existingBinding = KeyboardInputs.Where(kv => kv.Value != null && kv.Value.DisplayText != "NUMPADENTER" && kv.Value.KeyboardKey.HasValue && kv.Value.KeyboardKey.Value == key).FirstOrDefault();

            if (existingBinding.Value != null)
            {
                KeyboardInputs[existingBinding.Key] = null;
            }
        }

        /// <summary>
        /// Looks for the game control that <paramref name="button"/> is binded to and removes it by setting it to null;
        /// </summary>
        private void RemoveExistingGamepadBinding(GamePadButton button)
        {
            var existingBinding = GamepadInputs.Where(kv => kv.Value != null && kv.Value.GamepadInput.HasValue && kv.Value.GamepadInput.Value == button).FirstOrDefault();

            if (existingBinding.Value != null)
            {
                GamepadInputs[existingBinding.Key] = null;
            }
        }

        /// <summary>
        /// Looks for the game control that is binded to the given <paramref name="configVal"/> and removes it by setting it to null;
        /// </summary>
        private void RemoveExistingKeyboardBinding(int configVal)
        {
            if (KeyboardInputs.Any(kv => kv.Value != null && kv.Value.ConfigValue == configVal))
            {
                GameControl existingBinding = KeyboardInputs.Where(kv => kv.Value != null && kv.Value.ConfigValue == configVal)
                                                            .Select(kv => kv.Key)
                                                            .FirstOrDefault();
                KeyboardInputs[existingBinding] = null;
            }
        }
    }

}



