using MegaManX9.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MegaManX9.Gameplay
{
    /// <summary>Original health art plus readable, screen-space information for the playable level.</summary>
    [DisallowMultipleComponent]
    public sealed class RainLevelHUD : MonoBehaviour
    {
        private const float Width = 1600, Height = 900;
        [SerializeField] private RainLevelAssets assets;
        private AnimationStageHealthHUD healthHUD;
        private RainFrontEndAssets frontEndArt;
        private RainPauseAssets pauseArt;
        private int pauseSelection;
        private bool wasPauseMenuOpen;
        private GUIStyle title, heading, body, tiny, center, button;
        [System.NonSerialized] public bool SuppressPausePanel;
        public bool UsingSuppliedPauseArt { get { EnsurePauseArt(); return pauseArt != null && pauseArt.HasSuppliedArt; } }
        public bool UsesSuppliedPauseFont { get { EnsurePauseArt(); return pauseArt != null && pauseArt.HasSuppliedFont; } }
        public int PauseMenuSelection => pauseSelection;
        public bool ShowsGameplayHelpers => RainPresentationSettings.ShowGameplayHelpers;
        public bool UsingBossSkull => healthHUD != null && healthHUD.UsesBossSkull;

        public void Configure(RainLevelAssets value)
        {
            assets = value; EnsureHealthHUD();
        }

        private void Awake() { EnsureHealthHUD(); EnsurePauseArt(); }
        private void EnsurePauseArt() { if (pauseArt == null) pauseArt = RainPauseAssets.Load(); }
        private void Update()
        {
            var game = RainLevelGame.Instance;
            bool open = game != null && game.HasStarted && game.IsPaused && !SuppressPausePanel && (game.Narrative == null || !game.Narrative.IsActive);
            if (!open) { wasPauseMenuOpen = false; return; }
            if (!wasPauseMenuOpen) pauseSelection = 0;
            wasPauseMenuOpen = true;
            var keyboard = Keyboard.current; if (keyboard == null) return;
            int previous = pauseSelection;
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) pauseSelection = (pauseSelection + 2) % 3;
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) pauseSelection = (pauseSelection + 1) % 3;
            if (previous != pauseSelection) RainLevelAudio.Play("menuMove");
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame) ActivatePauseOption(game, pauseSelection);
        }
        private void ActivatePauseOption(RainLevelGame game, int option)
        {
            RainLevelAudio.Play("menuConfirm");
            if (option == 0) game.SetPaused(false);
            else if (option == 1) game.RestartLevel();
            else game.ReturnToMainMenu();
        }
        private void EnsureHealthHUD()
        {
            if (healthHUD == null) healthHUD = GetComponent<AnimationStageHealthHUD>();
            if (healthHUD == null) healthHUD = gameObject.AddComponent<AnimationStageHealthHUD>();
            if (assets != null && healthHUD.Assets != assets.healthHUD) healthHUD.Configure(assets.healthHUD);
        }

        private void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            title.normal.textColor = new Color(.48f, .95f, 1);
            heading = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            body.normal.textColor = new Color(.86f, .92f, 1);
            tiny = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            tiny.normal.textColor = new Color(.75f, .84f, .92f);
            center = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
        }

        private void OnGUI()
        {
            var game = RainLevelGame.Instance;
            if (game == null || game.Player == null || !game.HasStarted) return;
            if(game.ControlsGuide!=null&&game.ControlsGuide.IsActive)return;
            if (game.Narrative != null && game.Narrative.IsActive) return;
            if (assets == null) assets = game.Assets;
            EnsureHealthHUD(); Styles();
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color, previousBackground = GUI.backgroundColor;
            float scale = Mathf.Min(Screen.width / Width, Screen.height / Height);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - Width * scale) / 2, (Screen.height - Height * scale) / 2),
                Quaternion.identity, Vector3.one * scale);
            try
            {
                GUI.color = Color.white;
                if (game.Phase == RainLevelPhase.Won) { DrawResults(game); return; }
                if (game.IsPaused && !SuppressPausePanel) { DrawPause(game); return; }
                DrawHealth(game);
                if (ShowsGameplayHelpers)
                {
                    if (!game.IsPaused && GUI.Button(new Rect(1430, 130, 142, 40), "Pausa (" + RainInputBindings.KeyLabel(RainInputAction.Pause) + ")", button)) game.SetPaused(true);
                    DrawNotification(game);
                    DrawControls();
                }
                if (game.IsBossWarning) DrawWarning(game);
                if (game.Phase == RainLevelPhase.Dead) DrawDefeat(game);
            }
            finally { GUI.matrix = previousMatrix; GUI.color = previousColor; GUI.backgroundColor = previousBackground; }
        }

        private void DrawHeader(RainLevelGame game)
        {
            Panel(new Rect(425, 218, 750, 164), .55f);
            GUI.Label(new Rect(440, 225, 720, 35), "FUNDICIÓN BAJO LA TORMENTA", heading);
            GUI.Label(new Rect(440, 268, 720, 30), "PUNTOS " + game.Score.ToString("000000") + "     ·     " + Clock(game.Elapsed), heading);
            GUI.Label(new Rect(440, 310, 720, 26), "Punto de control: " + game.CheckpointName, center);
            GUI.Label(new Rect(440, 346, 720, 25), "Enemigos derrotados: " + game.DefeatedEnemies, center);
        }

        private void DrawHealth(RainLevelGame game)
        {
            var player = game.Player;
            bool redActive = player.CurrentForm == HeroForm.Red;
            DrawHeroBar(new Rect(22, 18, 396, 88), player.UltimateArmor ? "BlueUltimate" : "Blue",
                player.UltimateArmor ? "X · ARMADURA DEFINITIVA" : "X", player.BlueHealth, !redActive, game.Lives);
            DrawHeroBar(new Rect(22, 114, 396, 88), "Red",
                player.EnhancedSaber ? "Zero · FORMA POTENCIADA" : "Zero", player.RedHealth, redActive, game.Lives);
            if (ShowsGameplayHelpers && !redActive && player.Charge01 > 0)
            {
                GUI.Label(new Rect(36, 208, 360, 23), player.Charge01 >= .99f ? "¡DISPARO CARGADO!" : "Cargando disparo…", tiny);
                Gauge(new Rect(36, 237, 360, 8), player.Charge01, new Color(.2f, .9f, 1));
            }
            if (game.ActiveBossHealth != null)
            {
                string bossName = string.IsNullOrEmpty(game.ActiveBossName) ? "JEFE" : game.ActiveBossName;
                Panel(new Rect(1278, 18, 300, 104), .68f);
                GUI.Label(new Rect(1288, 23, 280, 25), bossName, heading);
                string unit = string.IsNullOrEmpty(game.ActiveBossUnit) ? "DragonRig" : game.ActiveBossUnit;
                if (!healthHUD.SupportsUnit(unit)) unit = "DragonRig";
                healthHUD.DrawHorizontal(new Rect(1288, 50, 280, 56), unit, game.ActiveBossHealth, -1, true);
            }
            if (!ShowsGameplayHelpers) return;
            string mode = redActive ? "Sable" : "Buster";
            float cooldown = Mathf.Clamp01(player.SpecialCooldown01);
            Panel(new Rect(1113, 723, 457, 65), .66f);
            GUI.Label(new Rect(1130, 730, 424, 24), mode + " · " + (cooldown <= .01f ? "Especial listo (" + RainInputBindings.KeyLabel(RainInputAction.Special) + ")" : "Recuperando especial"), tiny);
            Gauge(new Rect(1130, 762, 424, 8), 1 - cooldown, new Color(.2f, .88f, .75f));
        }

        private void DrawHeroBar(Rect rect, string unit, string label, CharacterHealth health, bool active, int sharedLives)
        {
            if (health == null) return;
            Panel(rect, active ? .69f : .4f);
            if (active)
            {
                GUI.color = new Color(.23f, .91f, 1, .9f);
                GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 3), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(rect.x + 14, rect.y + 3, rect.width - 28, 23), label + (active ? " · ACTIVO" : " · RESERVA"), tiny);
            healthHUD.DrawHorizontal(new Rect(rect.x + 14, rect.y + 26, rect.width - 28, 56), unit, health, sharedLives);
        }

        private void DrawControls()
        {
            Panel(new Rect(22, 813, 1556, 70), .8f);
            GUI.Label(new Rect(40, 823, 1520, 27),
                RainInputBindings.KeyLabel(RainInputAction.MoveLeft) + "/" + RainInputBindings.KeyLabel(RainInputAction.MoveRight) + ": mover   ·   " + RainInputBindings.KeyLabel(RainInputAction.Jump) + ": saltar   ·   " + RainInputBindings.KeyLabel(RainInputAction.Attack) + ": atacar (mantén para cargar)   ·   " + RainInputBindings.KeyLabel(RainInputAction.Dash) + ": dash   ·   " + RainInputBindings.KeyLabel(RainInputAction.Special) + ": especial", tiny);
            GUI.Label(new Rect(40, 851, 1520, 27),
                RainInputBindings.KeyLabel(RainInputAction.SwitchHero) + ": cambiar X/Zero   ·   " + RainInputBindings.KeyLabel(RainInputAction.ToggleArmor) + ": armadura de X   ·   " + RainInputBindings.KeyLabel(RainInputAction.ToggleSaber) + ": poder de Zero   ·   " + RainInputBindings.KeyLabel(RainInputAction.Pause) + ": pausa   ·   " + RainInputBindings.KeyLabel(RainInputAction.Retry) + ": reintentar", tiny);
        }

        private void DrawNotification(RainLevelGame game)
        {
            if (game.MessageRemaining <= 0 || string.IsNullOrEmpty(game.Message)) return;
            Panel(new Rect(352, 728, 736, 60), .84f);
            GUI.Label(new Rect(369, 735, 702, 46), game.Message, center);
        }

        private void DrawPause(RainLevelGame game)
        {
            EnsurePauseArt();
            if (!UsingSuppliedPauseArt || !UsesSuppliedPauseFont) { DrawLegacyPause(game); return; }
            Panel(new Rect(0, 0, Width, Height), .78f);
            pauseArt.DrawText(new Rect(300, 42, 1000, 63), "PAUSA", 43, Color.white, TextAnchor.MiddleCenter);
            pauseArt.DrawText(new Rect(300, 107, 1000, 40), "FUNDICION BAJO LA TORMENTA", 20, new Color(.66f,.84f,1), TextAnchor.MiddleCenter);
            DrawPauseHero(game, new Rect(210, 160, 540, 405), HeroForm.Blue);
            DrawPauseHero(game, new Rect(850, 160, 540, 405), HeroForm.Red);
            pauseArt.DrawText(new Rect(230, 604, 530, 46), "PUNTOS  " + game.Score.ToString("000000"), 28, new Color(.72f,.9f,1));
            pauseArt.DrawText(new Rect(900, 604, 500, 46), "TIEMPO  " + Clock(game.Elapsed), 28, new Color(1,.76f,.38f));
            pauseArt.DrawText(new Rect(230, 658, 530, 42), "ENEMIGOS  " + game.DefeatedEnemies, 23, Color.white);
            pauseArt.DrawText(new Rect(830, 658, 580, 42), "CONTROL  " + game.CheckpointName, 18, Color.white);
            string[] labels = { "CONTINUAR", "REINICIAR", "MENU PRINCIPAL" };
            for (int i = 0; i < labels.Length; i++)
            {
                var rect = new Rect(220 + i * 420, 758, 320, 62);
                if (Event.current.type == EventType.MouseMove && rect.Contains(Event.current.mousePosition) && pauseSelection != i)
                { pauseSelection = i; RainLevelAudio.Play("menuMove"); }
                bool selected = pauseSelection == i;
                if (selected && pauseArt.cursor != null) RainFrontEnd.DrawSprite(new Rect(rect.x + 4, rect.y + 19, 26, 26), pauseArt.cursor);
                pauseArt.DrawText(rect, labels[i], 25, selected ? new Color(.45f,1,.65f) : Color.white, TextAnchor.MiddleCenter);
                if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) { ActivatePauseOption(game, i); GUIUtility.ExitGUI(); }
            }
            string pauseKey = RainInputBindings.KeyLabel(RainInputAction.Pause);
            pauseArt.DrawText(new Rect(200, 838, 1200, 32), "FLECHAS PARA ELEGIR   ENTER ACEPTAR   " + pauseKey + " ESC CONTINUAR", 16, new Color(.75f,.83f,.9f), TextAnchor.MiddleCenter);
        }

        private void DrawPauseHero(RainLevelGame game, Rect panel, HeroForm form)
        {
            bool zero = form == HeroForm.Red;
            var player = game.Player;
            var health = zero ? player.RedHealth : player.BlueHealth;
            Color ink = zero ? new Color(1,.68f,.22f) : new Color(.6f,.78f,1);
            RainFrontEnd.DrawSprite(panel, zero ? pauseArt.zeroPanel : pauseArt.xPanel);
            var portrait = zero ? pauseArt.zeroPortrait : pauseArt.xPortrait;
            if (portrait != null) GUI.DrawTexture(new Rect(panel.x + 22, panel.y + 78, 165, 215), portrait, ScaleMode.ScaleToFit);
            pauseArt.DrawText(new Rect(panel.x + 22, panel.y + 34, 165, 42), zero ? "ZERO" : "X", 30, ink, TextAnchor.MiddleCenter);
            bool active = player.CurrentForm == form;
            pauseArt.DrawText(new Rect(panel.x + 210, panel.y + 49, 290, 36), active ? "ACTIVO" : "RESERVA", 22, active ? Color.white : new Color(.6f,.66f,.72f));
            pauseArt.DrawText(new Rect(panel.x + 210, panel.y + 101, 290, 36), "VIDA", 23, ink);
            var bar = new Rect(panel.x + 210, panel.y + 144, 290, 32);
            RainFrontEnd.DrawSprite(bar, pauseArt.healthFrame);
            float fraction = health != null ? health.Normalized : 0;
            Gauge(new Rect(bar.x + 10, bar.y + 5, bar.width - 21, bar.height - 10), fraction, fraction > .5f ? new Color(.1f,.9f,.25f) : fraction > .25f ? new Color(1,.8f,.15f) : new Color(1,.18f,.1f));
            string energy = health != null ? health.CurrentHealth + " DE " + health.MaxHealth + " PV" : "0 PV";
            pauseArt.DrawText(new Rect(panel.x + 210, panel.y + 187, 290, 36), energy, 21, Color.white);
            pauseArt.DrawText(new Rect(panel.x + 210, panel.y + 252, 290, 36), "VIDAS COMPARTIDAS", 18, ink);
            var life = zero ? pauseArt.zeroLifeIcon : pauseArt.xLifeIcon;
            if (life != null) RainFrontEnd.DrawSprite(new Rect(panel.x + 212, panel.y + 300, 40, 40), life);
            pauseArt.DrawText(new Rect(panel.x + 270, panel.y + 293, 200, 52), game.Lives.ToString(), 35, Color.white);
        }

        private void DrawLegacyPause(RainLevelGame game)
        {
            Panel(new Rect(380, 135, 840, 650), .94f);
            GUI.Label(new Rect(410, 152, 780, 52), "PAUSA", title);
            DrawHeader(game);
            GUI.Label(new Rect(430, 398, 740, 55), "Cambia entre X y Zero con " + RainInputBindings.KeyLabel(RainInputAction.SwitchHero) + ". Las vidas pertenecen a la reserva compartida.", center);
            if (GUI.Button(new Rect(460, 477, 680, 62), "Continuar", button)) game.SetPaused(false);
            if (GUI.Button(new Rect(460, 556, 680, 62), "Reiniciar nivel", button))
            { game.RestartLevel(); GUIUtility.ExitGUI(); }
            if (GUI.Button(new Rect(460, 635, 680, 62), "Volver al menú principal", button))
            { game.ReturnToMainMenu(); GUIUtility.ExitGUI(); }
            GUI.Label(new Rect(430, 720, 740, 30), RainInputBindings.KeyLabel(RainInputAction.Pause) + ": continuar", center);
        }

        private void DrawResults(RainLevelGame game)
        {
            if (frontEndArt == null) frontEndArt = RainFrontEndAssets.Load();
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, Width, Height), Texture2D.blackTexture);
            if (frontEndArt != null && frontEndArt.cover != null)
                GUI.DrawTexture(new Rect(0, 0, Width, Height), frontEndArt.cover, ScaleMode.ScaleAndCrop);
            GUI.color = new Color(0, .015f, .04f, .64f);
            GUI.DrawTexture(new Rect(0, 0, Width, Height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var resultTitle = new GUIStyle(title) { fontSize = 58, fontStyle = FontStyle.Normal, wordWrap = false };
            var resultHeading = new GUIStyle(heading) { fontSize = 29 };
            var resultScore = new GUIStyle(title) { fontSize = 54, wordWrap = false };
            var resultDetail = new GUIStyle(center) { fontSize = 27 };
            RainOutlinedGUI.Label(new Rect(180, 162, 1240, 100), "MISIÓN COMPLETADA", resultTitle, new Color(.55f, .95f, 1), 3);
            RainOutlinedGUI.Label(new Rect(280, 274, 1040, 55), "CENTRAL HIGHWAY", resultHeading, Color.white, 3);
            RainOutlinedGUI.Label(new Rect(400, 373, 800, 48), "PUNTUACIÓN", resultHeading, Color.white, 3);
            RainOutlinedGUI.Label(new Rect(400, 425, 800, 78), game.Score.ToString("000000"), resultScore, new Color(1, .9f, .45f), 3);
            RainOutlinedGUI.Label(new Rect(320, 537, 960, 52), "TIEMPO  " + Clock(game.Elapsed), resultDetail, Color.white, 3);
            RainOutlinedGUI.Label(new Rect(320, 598, 960, 52), "ENEMIGOS DERROTADOS  " + game.DefeatedEnemies, resultDetail, Color.white, 3);
            float pulse = .88f + .12f * Mathf.Sin(Time.unscaledTime * 3.5f);
            RainOutlinedGUI.Label(new Rect(350, 752, 900, 65), "Presiona Enter", new GUIStyle(resultHeading) { fontSize = 34 }, new Color(1, 1, 1, pulse), 3);
        }

        private void DrawDefeat(RainLevelGame game)
        {
            Panel(new Rect(410, 235, 780, 395), .95f);
            GUI.Label(new Rect(440, 260, 720, 75), game.IsGameOver ? "GAME OVER" : "VIDA PERDIDA", title);
            GUI.Label(new Rect(460, 345, 680, 75),
                game.IsGameOver ? "Se agotaron las tres vidas compartidas.\nPuedes comenzar una partida nueva o volver al menú." :
                "Ambos héroes comparten la reserva de vidas.\nQuedan " + game.Lives + " · reentrada automática al checkpoint.", center);
            if (GUI.Button(new Rect(490, 458, 620, 62), game.IsGameOver ? "Empezar otra partida" : "Reintentar desde el checkpoint", button))
            {
                if (game.IsGameOver) game.RestartLevel(); else game.Respawn();
                GUIUtility.ExitGUI();
            }
            if (GUI.Button(new Rect(490, 541, 620, 49), "Menú principal", button))
            { game.ReturnToMainMenu(); GUIUtility.ExitGUI(); }
            GUI.Label(new Rect(470, 596, 660, 30), "También puedes pulsar " + RainInputBindings.KeyLabel(RainInputAction.Retry) + ".", center);
        }

        private void DrawWarning(RainLevelGame game)
        {
            Panel(new Rect(0, 298, Width, 252), .48f);
            Color previous = GUI.color;
            Matrix4x4 matrix = GUI.matrix;
            float pulse = .78f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 7)) * .22f;
            GUI.color = new Color(.7f, .025f, .02f, pulse);
            GUI.DrawTexture(new Rect(0, 298, Width, 3), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 547, Width, 3), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var warningStyle = new GUIStyle(title)
            {
                fontSize = 138, fontStyle = FontStyle.BoldAndItalic,
                alignment = TextAnchor.MiddleCenter, wordWrap = false
            };
            warningStyle.normal.textColor = new Color(.7f, .025f, .02f, pulse);
            float nativeWidth = Mathf.Max(1, warningStyle.CalcSize(new GUIContent("WARNING")).x);
            // Stretch the native font horizontally while preserving the logical screen-space layout.
            GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(100, 310, 0), Quaternion.identity,
                new Vector3(1400 / nativeWidth, 1, 1));
            GUI.Label(new Rect(0, 0, nativeWidth, 170), "WARNING", warningStyle);
            GUI.matrix = matrix;
            var detail = new GUIStyle(heading) { fontSize = 21 };
            detail.normal.textColor = new Color(.94f, .34f, .29f);
            string bossName = string.IsNullOrEmpty(game.ActiveBossName) ? "JEFE" : game.ActiveBossName.ToUpperInvariant();
            GUI.Label(new Rect(130, 488, 1340, 38), bossName + " · SEÑAL DE JEFE DETECTADA", detail);
            GUI.color = previous;
        }

        private static string Clock(float seconds)
        {
            int whole = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (whole / 60).ToString("00") + ":" + (whole % 60).ToString("00");
        }
        private static void Panel(Rect rect, float alpha)
        {
            Color previous = GUI.color;
            GUI.color = new Color(.02f, .045f, .075f, alpha); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
        private static void Gauge(Rect rect, float normalized, Color color)
        {
            Color previous = GUI.color;
            GUI.color = new Color(.06f, .09f, .14f, .95f); GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color; GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(normalized), rect.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
