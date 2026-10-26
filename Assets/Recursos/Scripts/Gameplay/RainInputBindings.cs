using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MegaManX9.Gameplay
{
    public enum RainInputAction { MoveLeft, MoveRight, MoveUp, MoveDown, Jump, Attack, Dash, Special, SwitchHero, ToggleArmor, ToggleSaber, Pause, Retry }

    /// <summary>One persistent primary key per action. Default aliases remain available until that action is rebound.</summary>
    public static class RainInputBindings
    {
        private const string Preference = "MegaManX9.Rain.Input.v1";
        private static readonly Key[] Defaults = { Key.A,Key.D,Key.W,Key.S,Key.Space,Key.J,Key.K,Key.L,Key.Tab,Key.U,Key.H,Key.P,Key.R };
        private static BindingSet current;
        [Serializable] public sealed class BindingSet
        {
            public int[] keys;
            public BindingSet Clone() => new BindingSet { keys = (int[])keys.Clone() };
            public Key Get(RainInputAction action) => (Key)keys[(int)action];
            public void Set(RainInputAction action, Key key)
            {
                int index=(int)action; int old=keys[index];
                for(int i=0;i<keys.Length;i++) if(i!=index&&keys[i]==(int)key) keys[i]=old;
                keys[index]=(int)key;
            }
        }
        public static BindingSet DefaultBindings()
        {
            var result=new BindingSet {keys=new int[Defaults.Length]};
            for(int i=0;i<Defaults.Length;i++)result.keys[i]=(int)Defaults[i];
            return result;
        }
        public static BindingSet CopyBindings(){EnsureLoaded();return current.Clone();}
        public static void Save(BindingSet bindings)
        {
            if(!Valid(bindings))throw new ArgumentException("La configuración de teclas está incompleta.");
            current=bindings.Clone();PlayerPrefs.SetString(Preference,JsonUtility.ToJson(current));PlayerPrefs.Save();
        }
        private static bool Valid(BindingSet value)
        {
            if(value?.keys==null||value.keys.Length!=Defaults.Length)return false;
            for(int i=0;i<value.keys.Length;i++)
            {
                if(value.keys[i]<=(int)Key.None||!Enum.IsDefined(typeof(Key),value.keys[i]))return false;
                for(int j=0;j<i;j++)if(value.keys[i]==value.keys[j])return false;
            }
            return true;
        }
        private static void EnsureLoaded()
        {
            if(current!=null)return;
            try{current=JsonUtility.FromJson<BindingSet>(PlayerPrefs.GetString(Preference,""));}catch(ArgumentException){current=null;}
            if(!Valid(current))current=DefaultBindings();
        }
        public static string Label(RainInputAction action)
        {
            string[] labels={"Mover a la izquierda","Mover a la derecha","Subir / apuntar arriba","Bajar / agacharse","Saltar","Atacar / cargar","Dash","Ataque especial","Cambiar X / Zero","Armadura de X","Poder de Zero","Pausa","Reintentar"};
            return labels[(int)action];
        }
        public static string KeyLabel(RainInputAction action){EnsureLoaded();return KeyLabel(current.Get(action));}
        public static string KeyLabel(Key key)=>key==Key.Space?"Espacio":key==Key.LeftShift?"Mayús":key==Key.LeftArrow?"←":key==Key.RightArrow?"→":key==Key.UpArrow?"↑":key==Key.DownArrow?"↓":key.ToString();
        public static bool Pressed(RainInputAction action)=>Read(action,0);
        public static bool Held(RainInputAction action)=>Read(action,1);
        public static bool Released(RainInputAction action)=>Read(action,2);
        private static bool Test(ButtonControl key,int mode)=>key!=null&&(mode==0?key.wasPressedThisFrame:mode==1?key.isPressed:key.wasReleasedThisFrame);
        private static bool Alias(KeyControl key,RainInputAction action,int mode)
        {
            for(int i=0;i<current.keys.Length;i++)if(i!=(int)action&&current.keys[i]==(int)key.keyCode)return false;
            return Test(key,mode);
        }
        private static bool Read(RainInputAction action,int mode)
        {
            EnsureLoaded();var keyboard=Keyboard.current;var pad=Gamepad.current;bool value=false;
            if(keyboard!=null)
            {
                var key=current.Get(action);value=Test(keyboard[key],mode);
                if(key==Defaults[(int)action])
                {
                    switch(action)
                    {
                        case RainInputAction.MoveLeft:value|=Alias(keyboard.leftArrowKey,action,mode);break;
                        case RainInputAction.MoveRight:value|=Alias(keyboard.rightArrowKey,action,mode);break;
                        case RainInputAction.MoveUp:value|=Alias(keyboard.upArrowKey,action,mode);break;
                        case RainInputAction.MoveDown:value|=Alias(keyboard.downArrowKey,action,mode);break;
                        case RainInputAction.Jump:value|=Alias(keyboard.zKey,action,mode);break;
                        case RainInputAction.Attack:value|=Alias(keyboard.xKey,action,mode);break;
                        case RainInputAction.Dash:value|=Alias(keyboard.cKey,action,mode)||Alias(keyboard.leftShiftKey,action,mode);break;
                        case RainInputAction.Special:value|=Alias(keyboard.vKey,action,mode);break;
                        case RainInputAction.Pause:value|=Alias(keyboard.escapeKey,action,mode);break;
                    }
                }
            }
            if(pad!=null)
            {
                ButtonControl button=null;
                switch(action)
                {
                    case RainInputAction.Jump:button=pad.buttonSouth;break;
                    case RainInputAction.Attack:button=pad.buttonWest;break;
                    case RainInputAction.Dash:button=pad.buttonEast;break;
                    case RainInputAction.Special:button=pad.buttonNorth;break;
                    case RainInputAction.SwitchHero:button=pad.rightShoulder;break;
                    case RainInputAction.ToggleArmor:button=pad.leftShoulder;break;
                    case RainInputAction.ToggleSaber:button=pad.selectButton;break;
                    case RainInputAction.Pause:button=pad.startButton;break;
                }
                value|=Test(button,mode);
            }
            return value;
        }
        public static RainHeroInput ReadHeroInput()
        {
            var value=new RainHeroInput {move=new Vector2((Held(RainInputAction.MoveRight)?1:0)-(Held(RainInputAction.MoveLeft)?1:0),(Held(RainInputAction.MoveUp)?1:0)-(Held(RainInputAction.MoveDown)?1:0)),
                jumpPressed=Pressed(RainInputAction.Jump),jumpHeld=Held(RainInputAction.Jump),attackPressed=Pressed(RainInputAction.Attack),attackHeld=Held(RainInputAction.Attack),attackReleased=Released(RainInputAction.Attack),
                dashPressed=Pressed(RainInputAction.Dash),specialPressed=Pressed(RainInputAction.Special),switchPressed=Pressed(RainInputAction.SwitchHero),armorPressed=Pressed(RainInputAction.ToggleArmor),enhancedPressed=Pressed(RainInputAction.ToggleSaber)};
            var pad=Gamepad.current;if(pad!=null){if(pad.leftStick.ReadValue().magnitude>.2f)value.move=pad.leftStick.ReadValue();if(pad.dpad.ReadValue().magnitude>.2f)value.move=pad.dpad.ReadValue();}
            return value;
        }
    }
}


