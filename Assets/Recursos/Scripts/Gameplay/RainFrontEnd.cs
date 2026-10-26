using UnityEngine;
using UnityEngine.InputSystem;

namespace MegaManX9.Gameplay
{
    public enum RainMenuPage { Closed, Cover, Main, CharacterSelect, Options }

    /// <summary>Cover, keyboard/mouse menus, character choice and an explicit save/cancel controls editor.</summary>
    [DisallowMultipleComponent]
    public sealed class RainFrontEnd : MonoBehaviour
    {
        private RainLevelGame game;
        private RainFrontEndAssets art;
        private RainInputBindings.BindingSet draft;
        private int selected, waitingForKey=-1;
        private int optionSelection, captureStartedFrame=-1;
        private bool captureMustReleaseConfirm;
        private float pageOpened;
        private HeroForm chosen;
        private GUIStyle titleStyle, subtitle, text, button;
        public RainMenuPage Page {get;private set;}=RainMenuPage.Closed;
        public bool IsOpen=>Page!=RainMenuPage.Closed;
        public HeroForm SelectedHero=>chosen;
        public RainInputBindings.BindingSet DraftBindings=>draft;
        public int OptionSelection=>optionSelection;
        public int OptionCount=>draft!=null?draft.keys.Length+3:0;
        public bool IsWaitingForKey=>waitingForKey>=0;
        public RainInputAction? WaitingForAction=>waitingForKey>=0?(RainInputAction?)waitingForKey:null;
        public int WaitingForActionIndex=>waitingForKey;
        public void SelectHero(HeroForm form){if(chosen==form)return;chosen=form;RainLevelAudio.Play("menuMove");}
        public void SelectMenuOption(int index){if(index<0||index>2)throw new System.ArgumentOutOfRangeException(nameof(index));ChooseMain(index);}
        public void ConfirmSelection()
        {
            if(Page==RainMenuPage.Cover){RainLevelAudio.Play("menuEnter");SetPage(RainMenuPage.Main);}
            else if(Page==RainMenuPage.Main)ChooseMain(selected);
            else if(Page==RainMenuPage.CharacterSelect)StartChosen();
            else if(Page==RainMenuPage.Options&&waitingForKey<0)ActivateOption();
        }
        public void CancelOptions(){RainLevelAudio.Play("menuMove");ResetCapture();draft=null;SetPage(RainMenuPage.Main);}
        public void ApplyOptions(){if(draft==null)return;RainLevelAudio.Play("menuConfirm");RainInputBindings.Save(draft);ResetCapture();SetPage(RainMenuPage.Main);}
        public void BeginRebind(RainInputAction action)
        {
            if(Page!=RainMenuPage.Options)return;
            RainLevelAudio.Play("menuConfirm");optionSelection=waitingForKey=(int)action;captureStartedFrame=Time.frameCount;
            var keyboard=Keyboard.current;var pad=Gamepad.current;
            captureMustReleaseConfirm=keyboard!=null&&(keyboard.enterKey.isPressed||keyboard.numpadEnterKey.isPressed)||pad!=null&&pad.buttonSouth.isPressed;
        }
        public void SelectOption(int index)
        {if(Page!=RainMenuPage.Options||draft==null)return;optionSelection=Mathf.Clamp(index,0,OptionCount-1);}
        private void ResetCapture(){waitingForKey=-1;captureStartedFrame=-1;captureMustReleaseConfirm=false;}
        public void Configure(RainLevelGame value){game=value;art=RainFrontEndAssets.Load();}
        public void ShowCover(){SetPage(RainMenuPage.Cover);selected=0;chosen=HeroForm.Blue;}
        public void Close(){SetPage(RainMenuPage.Closed);ResetCapture();}
        public void OpenOptions(){draft=RainInputBindings.CopyBindings();ResetCapture();optionSelection=0;SetPage(RainMenuPage.Options);}
        private void SetPage(RainMenuPage page){Page=page;pageOpened=Time.unscaledTime;}
        private void Update()
        {
            if(!IsOpen||Time.unscaledTime<=pageOpened+.06f)return;
            var k=Keyboard.current;var p=Gamepad.current;if(k==null&&p==null)return;
            bool accept=k!=null&&(k.enterKey.wasPressedThisFrame||k.numpadEnterKey.wasPressedThisFrame)||p!=null&&p.buttonSouth.wasPressedThisFrame;
            bool cancel=k!=null&&k.escapeKey.wasPressedThisFrame||p!=null&&p.buttonEast.wasPressedThisFrame;
            if(Page==RainMenuPage.Options)
            {
                if(waitingForKey>=0)
                {
                    if(cancel){RainLevelAudio.Play("menuMove");ResetCapture();return;}
                    if(Time.frameCount<=captureStartedFrame)return;
                    if(captureMustReleaseConfirm)
                    {
                        if(k!=null&&(k.enterKey.isPressed||k.numpadEnterKey.isPressed)||p!=null&&p.buttonSouth.isPressed)return;
                        captureMustReleaseConfirm=false;
                    }
                    if(k!=null)foreach(var key in k.allKeys)if(key.wasPressedThisFrame&&key.keyCode!=Key.Escape){draft.Set((RainInputAction)waitingForKey,key.keyCode);RainLevelAudio.Play("menuConfirm");ResetCapture();break;}
                    return;
                }
                if(cancel){CancelOptions();return;}
                int previousOption=optionSelection;
                if(k!=null&&(k.upArrowKey.wasPressedThisFrame||k.wKey.wasPressedThisFrame)||p!=null&&p.dpad.up.wasPressedThisFrame)optionSelection=(optionSelection+OptionCount-1)%OptionCount;
                if(k!=null&&(k.downArrowKey.wasPressedThisFrame||k.sKey.wasPressedThisFrame)||p!=null&&p.dpad.down.wasPressedThisFrame)optionSelection=(optionSelection+1)%OptionCount;
                if(optionSelection>=draft.keys.Length)
                {
                    if(k!=null&&k.leftArrowKey.wasPressedThisFrame||p!=null&&p.dpad.left.wasPressedThisFrame)optionSelection=draft.keys.Length+(optionSelection-draft.keys.Length+2)%3;
                    if(k!=null&&k.rightArrowKey.wasPressedThisFrame||p!=null&&p.dpad.right.wasPressedThisFrame)optionSelection=draft.keys.Length+(optionSelection-draft.keys.Length+1)%3;
                }
                if(previousOption!=optionSelection)RainLevelAudio.Play("menuMove");
                if(accept)ActivateOption();return;
            }
            if(Page==RainMenuPage.Cover){if(accept)ConfirmSelection();return;}
            if(Page==RainMenuPage.CharacterSelect)
            {
                if(k!=null&&(k.leftArrowKey.wasPressedThisFrame||k.aKey.wasPressedThisFrame)||p!=null&&p.dpad.left.wasPressedThisFrame)SelectHero(HeroForm.Blue);
                if(k!=null&&(k.rightArrowKey.wasPressedThisFrame||k.dKey.wasPressedThisFrame)||p!=null&&p.dpad.right.wasPressedThisFrame)SelectHero(HeroForm.Red);
                if(cancel){RainLevelAudio.Play("menuMove");SetPage(RainMenuPage.Main);}else if(accept)ConfirmSelection();return;
            }
            int previousSelected=selected;
            if(k!=null&&(k.upArrowKey.wasPressedThisFrame||k.wKey.wasPressedThisFrame)||p!=null&&p.dpad.up.wasPressedThisFrame)selected=(selected+2)%3;
            if(k!=null&&(k.downArrowKey.wasPressedThisFrame||k.sKey.wasPressedThisFrame)||p!=null&&p.dpad.down.wasPressedThisFrame)selected=(selected+1)%3;
            if(selected!=previousSelected)RainLevelAudio.Play("menuMove");
            if(cancel){RainLevelAudio.Play("menuMove");SetPage(RainMenuPage.Cover);}else if(accept)ConfirmSelection();
        }
        private void ChooseMain(int index)
        {
            selected=index;
            RainLevelAudio.Play("menuConfirm");
            if(index==0)SetPage(RainMenuPage.CharacterSelect);
            else if(index==1)OpenOptions();else Quit();
        }
        private void StartChosen(){RainLevelAudio.Play("menuConfirm");Close();game.StartCampaign(chosen);}
        private void ActivateOption()
        {
            if(Page!=RainMenuPage.Options||draft==null||waitingForKey>=0)return;
            if(optionSelection<draft.keys.Length)BeginRebind((RainInputAction)optionSelection);
            else if(optionSelection==draft.keys.Length)ApplyOptions();
            else if(optionSelection==draft.keys.Length+1)CancelOptions();
            else {RainLevelAudio.Play("menuConfirm");draft=RainInputBindings.DefaultBindings();ResetCapture();}
        }
        public static void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#endif
        }
        private void Styles()
        {
            if(titleStyle!=null)return;
            titleStyle=new GUIStyle(GUI.skin.label){fontSize=42,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            subtitle=new GUIStyle(titleStyle){fontSize=27};text=new GUIStyle(GUI.skin.label){fontSize=20,alignment=TextAnchor.MiddleLeft,wordWrap=true};
            button=new GUIStyle(GUI.skin.button){fontSize=28,fontStyle=FontStyle.Bold};
        }
        private void OnGUI()
        {
            if(!IsOpen)return;Styles();int previousDepth=GUI.depth;GUI.depth=-40;
            var matrix=GUI.matrix;var color=GUI.color;var background=GUI.backgroundColor;
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1600*scale)/2,(Screen.height-900*scale)/2),Quaternion.identity,Vector3.one*scale);
            try
            {
                GUI.color=Color.white;Fill(new Rect(0,0,1600,900),Color.black);
                if(art!=null&&art.cover!=null)GUI.DrawTexture(new Rect(0,0,1600,900),art.cover,ScaleMode.ScaleAndCrop);
                if(Page!=RainMenuPage.Cover)Fill(new Rect(0,0,1600,900),new Color(0,.015f,.04f,.5f));
                if(art!=null&&art.title!=null)GUI.DrawTexture(Page==RainMenuPage.Cover?new Rect(371,0,858,343.2f):new Rect(507.5f,-10,585,234),art.title,ScaleMode.ScaleToFit);
                else RainOutlinedGUI.Label(new Rect(350,45,900,130),"MEGA MAN X9",titleStyle,Color.white,3);
                if(Page==RainMenuPage.Cover)
                {
                    var prompt=new Rect(500,760,600,60);
                    var promptStyle=new GUIStyle(subtitle){fontSize=34,fontStyle=FontStyle.Bold};
                    float promptAlpha=.88f+.12f*Mathf.Sin(Time.unscaledTime*3.5f);
                    RainOutlinedGUI.Label(prompt,"Presionar Enter",promptStyle,new Color(1,1,1,promptAlpha),3);
                    if(GUI.Button(prompt,GUIContent.none,GUIStyle.none))ConfirmSelection();
                    GUI.color=Color.white;
                }
                else if(Page==RainMenuPage.Main)
                {
                    string[] labels={"Empezar","Opciones","Salir"};
                    for(int i=0;i<3;i++)
                    {
                        var option=new Rect(555,337+i*106,490,78);
                        if(Event.current.type==EventType.MouseMove&&option.Contains(Event.current.mousePosition)&&selected!=i){selected=i;RainLevelAudio.Play("menuMove");}
                        Color ink=i==selected?new Color(.45f,.95f,1):Color.white;
                        RainOutlinedGUI.Label(option,labels[i],new GUIStyle(subtitle){fontSize=40},ink,3);
                        if(GUI.Button(option,GUIContent.none,GUIStyle.none))SelectMenuOption(i);
                    }
                    RainOutlinedGUI.Label(new Rect(450,752,700,50),"↑ ↓ seleccionar   ·   Enter aceptar   ·   Esc volver",subtitle,Color.white,2.5f);
                }
                else if(Page==RainMenuPage.CharacterSelect)DrawCharacters();
                else DrawOptions();
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.backgroundColor=background;GUI.depth=previousDepth;}
        }
        private void DrawCharacters()
        {
            RainOutlinedGUI.Label(new Rect(350,195,900,65),"ELIGE TU HÉROE INICIAL",titleStyle,Color.white,3);
            DrawCharacter(new Rect(335,293,420,398),HeroForm.Blue,art!=null?art.bluePortrait:null,"X","Buster · disparo cargado · armadura");
            DrawCharacter(new Rect(845,293,420,398),HeroForm.Red,art!=null?art.redPortrait:null,"ZERO","Sable · combos · técnicas especiales");
            RainOutlinedGUI.Label(new Rect(300,704,1000,42),"Podrás cambiar entre ambos durante la misión.",subtitle,Color.white,2.5f);
            var start=new Rect(575,761,450,66);
            RainOutlinedGUI.Label(start,"Empezar con "+(chosen==HeroForm.Blue?"X":"Zero"),subtitle,new Color(.45f,.95f,1),3);
            if(GUI.Button(start,GUIContent.none,GUIStyle.none))ConfirmSelection();
            var back=new Rect(52,796,210,54);
            RainOutlinedGUI.Label(back,"Volver",subtitle,Color.white,2.5f);
            if(GUI.Button(back,GUIContent.none,GUIStyle.none)){RainLevelAudio.Play("menuMove");SetPage(RainMenuPage.Main);}
        }
        private void DrawCharacter(Rect rect,HeroForm form,Texture portrait,string label,string detail)
        {
            bool active=chosen==form;GUI.color=active?Color.white:new Color(1,1,1,.55f);
            if(portrait!=null)GUI.DrawTexture(new Rect(rect.x+34,rect.y+25,352,297),portrait,ScaleMode.ScaleToFit);
            GUI.color=Color.white;
            RainOutlinedGUI.Label(new Rect(rect.x+20,rect.y+319,380,40),label,subtitle,active?new Color(.45f,.95f,1):Color.white,3);
            RainOutlinedGUI.Label(new Rect(rect.x+20,rect.y+361,380,30),detail,new GUIStyle(text){fontSize=16,alignment=TextAnchor.MiddleCenter},Color.white,2);
            if(GUI.Button(rect,GUIContent.none,GUIStyle.none))SelectHero(form);
        }
        private void DrawOptions()
        {
            Fill(new Rect(300,180,1000,690),new Color(.015f,.04f,.08f,.96f));
            RainOutlinedGUI.Label(new Rect(350,186,900,54),"OPCIONES · TECLADO",subtitle,Color.white,2);
            for(int i=0;i<draft.keys.Length;i++)
            {
                float y=249+i*37;var row=new Rect(340,y,905,35);
                if(waitingForKey<0&&Event.current.type==EventType.MouseMove&&row.Contains(Event.current.mousePosition)&&optionSelection!=i){optionSelection=i;RainLevelAudio.Play("menuMove");}
                if(optionSelection==i){Fill(row,new Color(.08f,.31f,.44f,.65f));OptionOutline(row,waitingForKey==i?new Color(1,.68f,.2f):new Color(.25f,.9f,1));}
                RainOutlinedGUI.Label(new Rect(355,y,520,34),RainInputBindings.Label((RainInputAction)i),text,optionSelection==i?new Color(.6f,.96f,1):Color.white,1.5f);
                GUI.backgroundColor=waitingForKey==i?new Color(.7f,.4f,.08f):new Color(.12f,.28f,.4f);
                var keyRect=new Rect(920,y,315,33);
                if(GUI.Button(keyRect,GUIContent.none,new GUIStyle(button){fontSize=20}))BeginRebind((RainInputAction)i);
                RainOutlinedGUI.Label(keyRect,waitingForKey==i?"Pulsa una tecla…":RainInputBindings.KeyLabel(draft.Get((RainInputAction)i)),new GUIStyle(subtitle){fontSize=20},Color.white,1.5f);
            }
            RainOutlinedGUI.Label(new Rect(350,738,900,40),waitingForKey>=0?"Esc cancela la captura. Una tecla ocupada intercambia las dos acciones.":"↑ ↓ seleccionar · Enter cambiar / aceptar · Esc volver sin cambios",new GUIStyle(text){fontSize=16,alignment=TextAnchor.MiddleCenter},Color.white,1.5f);
            int bindingCount=draft.keys.Length;
            if(OptionButton(new Rect(350,796,270,48),bindingCount,"Aplicar y volver")){optionSelection=bindingCount;ActivateOption();GUIUtility.ExitGUI();}
            if(OptionButton(new Rect(665,796,270,48),bindingCount+1,"Volver sin cambios")){optionSelection=bindingCount+1;ActivateOption();GUIUtility.ExitGUI();}
            if(OptionButton(new Rect(980,796,270,48),bindingCount+2,"Predeterminados")){optionSelection=bindingCount+2;ActivateOption();GUIUtility.ExitGUI();}
        }
        private bool OptionButton(Rect rect,int index,string label)
        {
            if(waitingForKey<0&&Event.current.type==EventType.MouseMove&&rect.Contains(Event.current.mousePosition)&&optionSelection!=index){optionSelection=index;RainLevelAudio.Play("menuMove");}
            bool active=optionSelection==index;
            if(active)OptionOutline(new Rect(rect.x-3,rect.y-3,rect.width+6,rect.height+6),new Color(.25f,.9f,1));
            GUI.backgroundColor=active?new Color(.12f,.6f,.7f):new Color(.12f,.28f,.4f);
            bool clicked=GUI.Button(rect,GUIContent.none,new GUIStyle(button){fontSize=22});
            RainOutlinedGUI.Label(rect,label,new GUIStyle(subtitle){fontSize=22},waitingForKey>=0?new Color(.6f,.66f,.72f):Color.white,1.5f);
            return clicked&&waitingForKey<0;
        }
        private static void OptionOutline(Rect rect,Color color)
        {
            Fill(new Rect(rect.x,rect.y,rect.width,2),color);Fill(new Rect(rect.x,rect.yMax-2,rect.width,2),color);
            Fill(new Rect(rect.x,rect.y,2,rect.height),color);Fill(new Rect(rect.xMax-2,rect.y,2,rect.height),color);
        }
        private static void Fill(Rect rect,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        public static void DrawSprite(Rect rect,Sprite sprite)
        {
            if(sprite==null)return;var r=sprite.textureRect;var texture=sprite.texture;
            GUI.DrawTextureWithTexCoords(rect,texture,new Rect(r.x/texture.width,r.y/texture.height,r.width/texture.width,r.height/texture.height));
        }
    }
}


