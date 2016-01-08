using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;
using System.Drawing;

namespace MathFizz
{
    class Program
    {
        #region Variables
        public const string ChampionName = "Fizz";
        public static Obj_AI_Hero Player = ObjectManager.Player;
        public static List<Obj_AI_Base> MinionList;
        public static Orbwalking.Orbwalker Orbwalker;
        //Menu
        public static Menu Menu;
        //Spells
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell F;

        private static Items.Item tiamat;
        private static Items.Item hydra;
        private static Items.Item cutlass;
        private static Items.Item botrk;
        private static Items.Item hextech;
        private static Items.Item zhonya;
        public static string hitchanceR = string.Empty;
        public static string debugText = string.Empty;
        public static string debugText2 = string.Empty;
        private static bool orbwalkToTarget = false;
        private static bool orbwalkToTarget2 = false;
        private static bool orbwalkToTarget3 = false;
        private static bool isEProcessed = false;
        private static int lastMovementTick = 0;
        private static float lastRCastTick = 0;
        private static float RCooldownTimer = 0;
        private static string lastSpell = string.Empty;
        private static bool doOnce = true;
        private static SharpDX.Vector3 castPosition = new SharpDX.Vector3();
        private static bool enoughManaEWQ = false;
        private static bool enoughManaEQ = false;
        private static bool canCastZhonyaOnDash = false;
        private static SharpDX.Vector3 startPos = new SharpDX.Vector3();
        private static Obj_AI_Hero SelectedTarget;
        private static Geometry.Polygon.Rectangle RRectangle;
        #endregion

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        #region OnGameLoad
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Fizz") return;
            Q = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W, Orbwalking.GetRealAutoAttackRange(Player));
            E = new Spell(SpellSlot.E, 400);
            R = new Spell(SpellSlot.R, 1300);
            F = new Spell(Player.GetSpellSlot("summonerflash"), 425);

            E.SetSkillshot(0.25f, 330, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 80, 600, true, SkillshotType.SkillshotLine);

            RRectangle = new Geometry.Polygon.Rectangle(Player.Position, Player.Position, 300);

            Menu = new Menu(Player.ChampionName, Player.ChampionName, true);
            var orbwalkerMenu = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            //Combo Menu
            var combo = new Menu("Combo", "Combo");
            Menu.AddSubMenu(combo);
            combo.AddItem(new MenuItem("ComboMode", "ComboMode").SetValue(new StringList(new[] { "R to gapclose", "R on Dash", "R after Dash" })));
            combo.AddItem(new MenuItem("HitChancewR", "R Hitchance").SetValue(new StringList(new[] { "Medium", "High", "Very High" })));
            combo.AddItem(new MenuItem("targetMinHPforR", "Minimum Enemy HP(in %) to use R").SetValue(new Slider(35)));
            combo.AddItem(new MenuItem("useZhonya", "Use Zhonya in combo (Recommended for lategame)").SetValue(true));
            //Harass Menu
            var harass = new Menu("Harass", "Harass");
            Menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("texttt", "Harass WQ AA E"));
            harass.AddItem(new MenuItem("useharassQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("useharassW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("useharassE", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("harassmana", "Min Harass Mana").SetValue(new Slider(0)));
            harass.AddItem(new MenuItem("useEWQ", "Harass with E(W)Q Combo").SetValue(false));
            harass.AddItem(new MenuItem("recom", "Recommended to disable 'Priorize farm to harass' in Orbwalker > Misc"));
            //LaneClear Menu
            var lc = new Menu("Laneclear", "Laneclear");
            Menu.AddSubMenu(lc);
            lc.AddItem(new MenuItem("laneclearQ", "Use Q to LaneClear").SetValue(false));
            lc.AddItem(new MenuItem("laneclearW", "Use W to LaneClear").SetValue(false));
            lc.AddItem(new MenuItem("laneclearE", "Use E to LaneClear").SetValue(false));
            lc.AddItem(new MenuItem("lanemana", "Min Farm Mana").SetValue(new Slider(0)));
            //JungleClear Menu
            var jungle = new Menu("JungleClear", "JungleClear");
            Menu.AddSubMenu(jungle);
            jungle.AddItem(new MenuItem("jungleclearQ", "Use Q to JungleClear").SetValue(false));
            jungle.AddItem(new MenuItem("jungleclearW", "Use W to JungleClear").SetValue(false));
            jungle.AddItem(new MenuItem("jungleclearE", "Use E to JungleClear").SetValue(false));
            jungle.AddItem(new MenuItem("junglemana", "Min Jungle Mana").SetValue(new Slider(0)));
            //CustomCombo Menu
            var customCombo = new Menu("CustomCombo (require a selected target!)", "CustomCombo").SetFontStyle(FontStyle.Bold, fontColor: SharpDX.Color.Yellow);
            Menu.AddSubMenu(customCombo);
            customCombo.AddItem(new MenuItem("info","How to use CustomCombo's :").SetFontStyle(FontStyle.Bold, fontColor: SharpDX.Color.Red));
            customCombo.AddItem(new MenuItem("info1", "1) Make sure every spells used in the combo are up.").SetFontStyle(FontStyle.Bold, fontColor: SharpDX.Color.Red));
            customCombo.AddItem(new MenuItem("info2", "2) Select your Target.").SetFontStyle(FontStyle.Bold, fontColor: SharpDX.Color.Red));
            customCombo.AddItem(new MenuItem("info3", "3) Press combo key until every spells are used.").SetFontStyle(FontStyle.Bold, fontColor: SharpDX.Color.Red));
            customCombo.AddItem(new MenuItem("info4", "4) Press space key afterwards for ideal follow up.").SetFontStyle(FontStyle.Bold, fontColor: SharpDX.Color.Red));
            customCombo.AddItem(new MenuItem("lateGameZhonyaCombo", "EE to gapclose RWQ zhonya").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            customCombo.AddItem(new MenuItem("QminionREWCombo", "Q minion to gapclose REW").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Press)));
            customCombo.AddItem(new MenuItem("EFlashCombo", "E Flash on target RWQ zhonya").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Press)));
            customCombo.AddItem(new MenuItem("Flee", "Flee Key (Flee does not require a target)").SetValue(new KeyBind("Q".ToCharArray()[0], KeyBindType.Press)));
            //Misc Menu
            var miscMenu = new Menu("Misc", "Misc");
            Menu.AddSubMenu(miscMenu);
            miscMenu.AddItem(new MenuItem("drawQ", "Draw Q range").SetValue(false));
            miscMenu.AddItem(new MenuItem("drawMinionQCombo", "Draw QminionREWCombo helper").SetValue(false));
            miscMenu.AddItem(new MenuItem("drawR", "Draw R Prediction (Selected Target Only)").SetValue(false));
            miscMenu.AddItem(new MenuItem("drawRHitChance", "Draw Hitchance status of R (Selected Target Only)").SetValue(false));
            miscMenu.AddItem(new MenuItem("antiAfk", "Anti-AFK").SetValue(false));
            //Author Menu
            var about = new Menu("About", "About").SetFontStyle(FontStyle.Regular, fontColor: SharpDX.Color.Gray);
            Menu.AddSubMenu(about);
            about.AddItem(new MenuItem("Author", "Author: mathieu002"));
            about.AddItem(new MenuItem("Credits", "Credits: ChewyMoon,1Shinigamix3,jQuery,Kurisu"));

            hydra = new Items.Item(3074, 185);
            tiamat = new Items.Item(3077, 185);
            cutlass = new Items.Item(3144, 450);
            botrk = new Items.Item(3153, 450);
            hextech = new Items.Item(3146, 700);
            zhonya = new Items.Item(3157);

            Menu.AddToMainMenu();
            Game.PrintChat("<font color='#2CCACE'>Fizz by</font> <font color='#B000FF'>mathieu002</font> <font color='##FFD93B'>Loaded</font>");
            OnDoCast();
            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
        }
        #endregion

        #region OnDoCast
        private static void OnDoCast()
        {
            Obj_AI_Base.OnDoCast += (sender, args) =>
            {
                if (sender.IsMe && args.SData.IsAutoAttack())
                {
                    var useE = (E.IsReady());
                    var useR = (R.IsReady());
                    var useZhonya = (Menu.Item("useZhonya").GetValue<bool>() && zhonya.IsReady());
                    var ondash = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 1);
                    var afterdash = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 2);
                    var gapclose = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 0);
                    var target = (Obj_AI_Hero)args.Target;
                    #region Orbwalking Combo
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    {
                        if (E.IsReady() && E.Instance.Name == "FizzJump" && Player.Distance(target.Position) <= E.Range) 
                        {
                            SharpDX.Vector3 castPosition1 = E.GetPrediction(target, false, 1).CastPosition.Extend(Player.Position, -165);
                            E.Cast(castPosition1);
                            if (useZhonya)
                            {
                                if (ondash && canCastZhonyaOnDash)
                                {
                                    //debugText2 = "E from OnDoCast: " + Game.Time;
                                    Utility.DelayAction.Add(1700, () =>
                                    {
                                        zhonya.Cast();
                                    });
                                }
                                if (afterdash && canCastZhonyaOnDash)
                                {
                                    //debugText2 = "E from OnDoCast: " + Game.Time;
                                    Utility.DelayAction.Add(1700, () =>
                                    {
                                        zhonya.Cast();
                                    });
                                }
                                if (gapclose && canCastZhonyaOnDash)
                                {
                                    //debugText2 = "E from OnDoCast: " + Game.Time;
                                    Utility.DelayAction.Add(1700, () =>
                                    {
                                        zhonya.Cast();
                                    });
                                }
                            }
                            Utility.DelayAction.Add(775, () =>
                            {
                                    if (!W.IsReady() && !Q.IsReady() && Player.Distance(target.Position) > 330 && Player.Distance(target.Position) <= 400 + 270)
                                    {
                                        E.Cast(E.GetPrediction(target, false, 1).CastPosition);
                                    }
                            });
                        }
                    }
                    #endregion
                    #region Orbwalking Harass
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                    {
                        var useQ = (Menu.Item("useharassQ").GetValue<bool>() && Q.IsReady());
                        var useE2 = (Menu.Item("useharassE").GetValue<bool>() && E.IsReady());
                        if (Menu.Item("useEWQ").GetValue<bool>())
                        {
                            if (useQ && !E.IsReady() && Player.Distance(target.Position) <= Q.Range)
                            {
                                Q.Cast(target);
                            }
                        }
                        if (!Menu.Item("useEWQ").GetValue<bool>())
                        {
                            if (useE2 && !Q.IsReady() && Player.Distance(target.Position) <= 550)
                            {
                                SharpDX.Vector3 castPosition = E.GetPrediction(target, false, 1).CastPosition;
                                E.Cast(castPosition);
                                //Player.IssueOrder(GameObjectOrder.AttackUnit, target);
                                //debugText2 = "E from harass orbwalker" + Game.Time;
                                Utility.DelayAction.Add(775, () => 
                                {
                                    if (!W.IsReady() && !Q.IsReady() && Player.Distance(target.Position) > 330 && Player.Distance(target.Position) <= 400 + 270)
                                    {
                                        E.Cast(E.GetPrediction(target, false, 1).CastPosition);
                                    }
                                });
                            }
                        }
                    }
                    #endregion
                }
            };
        }
        #endregion

        #region OnDraw
        private static void OnDraw(EventArgs args)
        {
            if (Menu.Item("drawQ").GetValue<bool>())
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.DarkRed, 3);
            }
            if (Menu.Item("drawMinionQCombo").GetValue<bool>() && SelectedTarget.IsValidTarget())
            {
                if (Player.Distance(SelectedTarget) <= R.Range + Q.Range)
                {
                    RRectangle.Draw(Color.CornflowerBlue, 3);
                }
            }
            if (hitchanceR != "" && Menu.Item("drawRHitChance").GetValue<bool>())
            {
                Drawing.DrawText(400, 400, Color.DarkTurquoise, "Hitchance: " + hitchanceR);
            }
            if (debugText != "")
            {
                Drawing.DrawText(400, 600, Color.DarkTurquoise, "Debug: " + debugText);
            }
            if (debugText2 != "")
            {
                Drawing.DrawText(400, 800, Color.DarkTurquoise, "Debug: " + debugText2);
            }
            if (Menu.Item("drawR").GetValue<bool>() && SelectedTarget.IsValidTarget())
            {
                CollisionableObjects[] collisionCheck = { CollisionableObjects.YasuoWall};
                Render.Circle.DrawCircle(R.GetPrediction(SelectedTarget, false, 1, collisionCheck).CastPosition.Extend(Player.Position, -330), 250, Color.Blue);
            }
        }
        #endregion

        #region OnUpdate
        private static void OnUpdate(EventArgs args)
        {
            if (Player.IsDead || Player.IsRecalling())
            {
                return;
            }

            SelectedTarget = TargetSelector.SelectedTarget;

            if (SelectedTarget.IsValidTarget())
            {
                if (Player.Distance(SelectedTarget) <= R.Range + Q.Range + 100)
                {
                    CollisionableObjects[] collisionCheck = { CollisionableObjects.YasuoWall };
                    RRectangle.Start = Player.Position.Shorten(SelectedTarget.Position, -250).To2D();
                    RRectangle.End = R.GetPrediction(SelectedTarget, false, 1, collisionCheck).CastPosition.Extend(Player.Position, -330).To2D();
                    RRectangle.UpdatePolygon();
                }
            }
            if (Menu.Item("useEWQ").GetValue<bool>() == true)
            {
                if (!Menu.Item("useharassQ").GetValue<bool>())
                {
                    Menu.Item("useharassQ").SetValue<bool>(true);
                }
                if (!Menu.Item("useharassE").GetValue<bool>())
                {
                    Menu.Item("useharassE").SetValue<bool>(true);
                }
                if (!Menu.Item("useharassW").GetValue<bool>())
                {
                    Menu.Item("useharassW").SetValue<bool>(true);
                }
                if (Menu.Item("harassmana").GetValue<Slider>().Value != 0)
                {
                    Menu.Item("harassmana").SetValue<Slider>(new Slider(0));
                }
            }
            if (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 1)
            {
                //ondash need to have r hitchance to medium
                Menu.Item("HitChancewR").SetValue<StringList>(new StringList(new[] { "Medium", "High", "Very High" }));
            }
            if (!Menu.Item("lateGameZhonyaCombo").GetValue<KeyBind>().Active && orbwalkToTarget2)
            {
                orbwalkToTarget2 = false;
            }
            if (!Menu.Item("QminionREWCombo").GetValue<KeyBind>().Active && orbwalkToTarget) 
            {
                orbwalkToTarget = false;
            }
            if (!Menu.Item("EFlashCombo").GetValue<KeyBind>().Active && orbwalkToTarget3)
            {
                orbwalkToTarget3 = false;
            }
            if (!Menu.Item("EFlashCombo").GetValue<KeyBind>().Active && isEProcessed)
            {
                isEProcessed = false;
            }
            if (Menu.Item("drawRHitChance").GetValue<bool>() && SelectedTarget.IsValidTarget())
            {
                CollisionableObjects[] collisionCheck = new CollisionableObjects[1];
                collisionCheck[0] = CollisionableObjects.YasuoWall;
                HitChance test = R.GetPrediction(SelectedTarget, false, -1, collisionCheck).Hitchance;
                if (test == HitChance.Collision) hitchanceR = "Collision Detected";
                if (test == HitChance.Dashing) hitchanceR = "Is Dashing";
                if (test == HitChance.Immobile) hitchanceR = "Immobile";
                if (test == HitChance.Medium) hitchanceR = "Medium Chance";
                if (test == HitChance.VeryHigh) hitchanceR = "VeryHigh Chance";
                if (test == HitChance.Low) hitchanceR = "Low  Chance";
                if (test == HitChance.High) hitchanceR = "High Chance";
                if (test == HitChance.OutOfRange) hitchanceR = "OutOfRange";
                if (test == HitChance.Impossible) hitchanceR = "Impossible";
            }
            //Anti AFK
            if (Menu.Item("antiAfk").GetValue<bool>())
            {
                if (Environment.TickCount - lastMovementTick > 140000)
                {
                    ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo,
                        ObjectManager.Player.Position.Randomize(-200, 200));
                    lastMovementTick = Environment.TickCount;
                }
            }
            if(R.IsReady())
            {
                doOnce = true;
            }
            if (doOnce && Player.LastCastedspell().Name == "FizzMarinerDoom")
            {
                RCooldownTimer = Game.Time;
                doOnce = false;
            }
            //R cast tick lastRCastTick
            if (Game.Time - RCooldownTimer <= 7.0f)
            {
               canCastZhonyaOnDash = true;
            }
            else
            {
               canCastZhonyaOnDash = false;
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                Harass();
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                Lane();
                Jungle();
            }
            if (Menu.Item("Flee").GetValue<KeyBind>().Active)
            {
                Flee();
            }
            if (Menu.Item("lateGameZhonyaCombo").GetValue<KeyBind>().Active)
            {
                lateGameZhonyaCombo();
            }
            if (Menu.Item("QminionREWCombo").GetValue<KeyBind>().Active)
            {
                QminionREWCombo();
            }
            if (Menu.Item("EFlashCombo").GetValue<KeyBind>().Active)
            {
                EFlashCombo();
            }
        }
        #endregion

        #region Flee
        private static void Flee()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (E.IsReady())
            {
                E.Cast(Game.CursorPos);
            }
        }
        #endregion

        #region R smartCast
        //R usage
        public static void CastRSmart(Obj_AI_Hero target)
        {
            var veryhigh = (Menu.Item("HitChancewR").GetValue<StringList>().SelectedIndex == 2);
            var medium = (Menu.Item("HitChancewR").GetValue<StringList>().SelectedIndex == 0);
            var high = (Menu.Item("HitChancewR").GetValue<StringList>().SelectedIndex == 1);

            CollisionableObjects[] collisionCheck = new CollisionableObjects[1];
            collisionCheck[0] = CollisionableObjects.YasuoWall;
            HitChance hitChance = R.GetPrediction(target, false, -1, collisionCheck).Hitchance;
            SharpDX.Vector3 endPosition = R.GetPrediction(target, false, 1, collisionCheck).CastPosition.Extend(Player.Position, -330);
            if (medium && hitChance >= HitChance.Medium)
            {
                R.Cast(endPosition);
            }
            if (high && hitChance >= HitChance.High)
            {
                R.Cast(endPosition);
            }
            if (veryhigh && hitChance >= HitChance.VeryHigh)
            {
                R.Cast(endPosition);
            }
        }
        #endregion

        #region Lane
        private static void Lane()
        {
            if (ObjectManager.Player.ManaPercent < Menu.Item("lanemana").GetValue<Slider>().Value)
            {
                return; 
            }
            if (Menu.Item("laneclearQ").GetValue<bool>() && Q.IsReady())
            {
                MinionList = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
                foreach (var minion in MinionList)
                {
                    Q.Cast(minion);
                }              
            }
            if (Menu.Item("laneclearW").GetValue<bool>() && W.IsReady())
            {
                var allMinionsW = MinionManager.GetMinions(Player.Position, W.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).ToList();
                foreach (var minion in allMinionsW)
                {
                    W.Cast(minion);
                }
            }
            if (Menu.Item("laneclearE").GetValue<bool>() && E.Instance.Name == "FizzJump" && E.IsReady())
            {
                var allMinionsE = MinionManager.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health).ToList();
                foreach (var minion in allMinionsE)
                {
                    E.Cast(minion);
                }
            }
        }
        #endregion

        #region Jungle
        private static void Jungle()
        {
            if (ObjectManager.Player.ManaPercent < Menu.Item("junglemana").GetValue<Slider>().Value)
            {
                return;
            }
            var mobs = MinionManager.GetMinions(Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            if (!mobs.Any())
                return;
            var mob = mobs.First();

            if (Menu.Item("jungleclearQ").GetValue<bool>() && Q.IsReady() && mob.IsValidTarget(Q.Range))
            {
                Q.Cast(mob);
            }
            if (Menu.Item("jungleclearW").GetValue<bool>() && W.IsReady() && mob.IsValidTarget(W.Range))
            {
                W.Cast(mob);
            }
            if (Menu.Item("jungleclearE").GetValue<bool>() && E.IsReady() && mob.IsValidTarget(E.Range))
            {
                E.Cast(mob.ServerPosition);
            }
        }
        #endregion

        #region Harass
        private static void Harass()
        {
            var useQ = (Menu.Item("useharassQ").GetValue<bool>() && Q.IsReady());
            var useW = (Menu.Item("useharassW").GetValue<bool>() && W.IsReady());
            var useE = (Menu.Item("useharassE").GetValue<bool>() && E.IsReady());
            var m = SelectedTarget;
            if (!m.IsValidTarget())
            {
                m = TargetSelector.GetTarget(530, TargetSelector.DamageType.Magical);
            }
            if (ObjectManager.Player.ManaPercent < Menu.Item("harassmana").GetValue<Slider>().Value)
            {
                return;
            }
            #region EWQ Combo
            //EWQ Combo
            if (Menu.Item("useEWQ").GetValue<bool>())
            {
                if (Q.IsReady())
                {
                    //Do EWQ
                    if (Player.Mana >= Q.ManaCost + E.ManaCost + W.ManaCost || enoughManaEWQ)
                    {
                        if (useE && E.Instance.Name == "FizzJump" && Player.Distance(m.Position) <= 530)
                        {
                            enoughManaEWQ = true;
                            startPos = Player.Position;
                            SharpDX.Vector3 harassEcastPosition = E.GetPrediction(m, false, 1).CastPosition;
                            E.Cast(harassEcastPosition);
                            //Delay for fizzjumptwo
                            Utility.DelayAction.Add(180, () => E.Cast(E.GetPrediction(m, false, 1).CastPosition.Extend(startPos, -135)));
                        }
                        if (useW && (Player.Distance(m.Position) <= 175))
                        {
                            W.Cast();
                            enoughManaEWQ = false;
                        }
                    }
                    //Do EQ
                    if (Player.Mana >= Q.ManaCost + E.ManaCost || enoughManaEQ)
                    {
                        if (useE && E.Instance.Name == "FizzJump" && Player.Distance(m.Position) <= 530)
                        {
                            enoughManaEQ = true;
                            startPos = Player.Position;
                            SharpDX.Vector3 harassEcastPosition3 = E.GetPrediction(m, false, 1).CastPosition;
                            E.Cast(harassEcastPosition3);
                            //Delay for fizzjumptwo
                            Utility.DelayAction.Add(180, () => 
                            {
                                E.Cast(E.GetPrediction(m, false, 1).CastPosition.Extend(startPos, -135));
                                enoughManaEQ = false;
                            });
                        }
                    }
                }
            }
            #endregion
            //Basic Harass WQ AA E
            else
            {
                if (useW && (Player.Distance(m.Position) <= Q.Range)) W.Cast();
                if (useQ && (Player.Distance(m.Position) <= Q.Range))
                {
                    Q.Cast(m);
                }
            }
        }
        #endregion

        #region Combo
        private static void Combo()
        {
            var useQ = (Q.IsReady());
            var useW = (W.IsReady());
            var useE = (E.IsReady());
            var useR = (R.IsReady());
            var useZhonya = (Menu.Item("useZhonya").GetValue<bool>() && zhonya.IsReady() && zhonya.IsOwned());
            var gapclose = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 0);
            var ondash = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 1);
            var afterdash = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 2);
            var m = SelectedTarget;
            if (!m.IsValidTarget()) 
            {
                m = TargetSelector.GetTarget(1275, TargetSelector.DamageType.Magical);
            }
            //Only use when R is Ready & Q is Ready
            if (ondash && !m.IsZombie && useR && useQ && Player.Distance(m.Position) <= 555)
            {
                if (useQ && Player.Distance(m.Position) <= Q.Range)
                {
                    if (useR && m.HealthPercent >= Menu.Item("targetMinHPforR").GetValue<Slider>().Value)
                    {
                        CastRSmart(m);
                        lastRCastTick = Game.Time;
                    }
                    Q.Cast(m);
                }
                if (useW && Player.Distance(m.Position) <= 540)
                {
                    W.Cast();
                }
                if (hydra.IsOwned() && Player.Distance(m) < hydra.Range && hydra.IsReady() && !E.IsReady()) hydra.Cast();
                if (tiamat.IsOwned() && Player.Distance(m) < tiamat.Range && tiamat.IsReady() && !E.IsReady()) tiamat.Cast();
            }
            //Only use when R is Ready & Q is Ready
            if (afterdash && !m.IsZombie && useR && useQ)
            {

                if (useW && Player.Distance(m.Position) <= 540) W.Cast();
                if (useQ && Player.Distance(m.Position) <= 550){
                    Q.Cast(m);
                    Utility.DelayAction.Add(500, () => {
                        if (useR && m.HealthPercent >= Menu.Item("targetMinHPforR").GetValue<Slider>().Value)
                        {
                            CastRSmart(m);
                            lastRCastTick = Game.Time;
                        }
                    });
                }
                if (hydra.IsOwned() && Player.Distance(m) < hydra.Range && hydra.IsReady() && !E.IsReady()) hydra.Cast();
                if (tiamat.IsOwned() && Player.Distance(m) < tiamat.Range && tiamat.IsReady() && !E.IsReady()) tiamat.Cast();
            }
            if (gapclose && !m.IsZombie && useR && useQ)
            {
                if (useR && m.HealthPercent >= Menu.Item("targetMinHPforR").GetValue<Slider>().Value)
                {
                    CastRSmart(m);
                    lastRCastTick = Game.Time;
                }
                if (useQ) Q.Cast(m);
                if (useW && Player.Distance(m.Position) <= 540) W.Cast();
                if (hydra.IsOwned() && Player.Distance(m) < hydra.Range && hydra.IsReady() && !E.IsReady()) hydra.Cast();
                if (tiamat.IsOwned() && Player.Distance(m) < tiamat.Range && tiamat.IsReady() && !E.IsReady()) tiamat.Cast();
            }
            if (useW && Player.Distance(m.Position) <= 540) W.Cast();
            if (useQ && Player.Distance(m.Position) <= Q.Range) Q.Cast(m);
            if (E.Instance.Name == "FizzJump" && useE && Player.Distance(m.Position) > 180 && Player.Distance(m.Position) <= E.Range && !W.IsReady() && !Q.IsReady() && !R.IsReady())
            {
                castPosition = E.GetPrediction(m, false, 1).CastPosition;
                E.Cast(castPosition);
                Utility.DelayAction.Add(775, () =>
                {
                    if (!W.IsReady() && !Q.IsReady() && Player.Distance(m.Position) > 330 && Player.Distance(m.Position) <= 400 + 270)
                    {
                        E.Cast(E.GetPrediction(m, false, 1).CastPosition);
                    }
                });
                if (ondash && useZhonya && canCastZhonyaOnDash)
                {
                    //debugText2 = "E from Combo()" + Game.Time;
                    Utility.DelayAction.Add(2365, () => {
                        zhonya.Cast();
                    });
                }
                if (gapclose && useZhonya && canCastZhonyaOnDash)
                {
                    //debugText2 = "E from Combo()" + Game.Time;
                    Utility.DelayAction.Add(2365, () =>
                    {
                        zhonya.Cast();
                    });
                }
                if (afterdash && useZhonya && canCastZhonyaOnDash)
                {
                    //debugText2 = "E from Combo()" + Game.Time;
                    Utility.DelayAction.Add(2365, () =>
                    {
                        zhonya.Cast();
                    });
                }
            }
        }
        #endregion

        #region Custom Combos
        private static void lateGameZhonyaCombo()
        {
            var m = SelectedTarget;
            if (!orbwalkToTarget2 && Player.LastCastedSpellName() == "FizzJump")
            {
                orbwalkToTarget2 = true;
            }
            if (m.IsValidTarget())
            {
                if (!orbwalkToTarget2)
                {
                    Orbwalking.Orbwalk(null, Game.CursorPos);
                }
                else
                {
                    Orbwalking.Orbwalk(m ?? null, Game.CursorPos);
                }
                var distance = Player.Distance(m.Position);
                //Check distance
                if (distance <= (E.Range + Q.Range + E.Range))
                {
                    if (E.IsReady())
                    {
                       //Use E1
                        castPosition = E.GetPrediction(m, false, 1).CastPosition.Extend(Player.Position, -135);
                        E.Cast(castPosition);
                    }
                    if (R.IsReady() && !E.IsReady())
                    {
                        //Use R
                        CastRSmart(m);
                    }
                    //Use W
                    if (W.IsReady() && !E.IsReady())
                    {
                        W.Cast();
                    }
                    //Use Q
                    if (Q.IsReady() && !E.IsReady())
                    {
                        Q.Cast(m);
                    }
                    if (Player.LastCastedSpellName() == "FizzPiercingStrike")
                    {
                        //Check if zhonya is active
                        if (zhonya.IsOwned() && zhonya.IsReady())
                        {
                            zhonya.Cast();
                        }
                    }
                }
            }
            else
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
        }

        private static void QminionREWCombo() 
        {
            var m = SelectedTarget;
            if (!orbwalkToTarget && Player.LastCastedSpellName() == "FizzPiercingStrike") 
            {
                orbwalkToTarget = true;
            }
            if (m.IsValidTarget()) 
            {
                if (!orbwalkToTarget)
                {
                    Orbwalking.Orbwalk(null, Game.CursorPos);
                }
                else 
                {
                    Orbwalking.Orbwalk(m ?? null, Game.CursorPos);
                }
                var distance = Player.Distance(m.Position);
                if (distance <= ((Q.Range + R.Range) - 600)) 
                {
                    if (Q.IsReady()) 
                    {
                        //Check if HeroTarget is in Q.Range then Q 
                        if (Player.Distance(m.Position) <= Q.Range)
                        {
                            Q.Cast(m);
                        }
                        else
                        {
                            //Check if minions in rectangle is in Q.Range then Q
                            var mins = MinionManager.GetMinions(Q.Range);
                            foreach (Obj_AI_Base min in mins)
                            {
                                if (RRectangle.IsInside(min.Position) && min.Distance(m.Position) > 300)
                                {
                                    Q.Cast(min);
                                }
                            }
                        }
                    }
                    if (R.IsReady() && !Q.IsReady())
                    {
                        //Use R
                        Utility.DelayAction.Add(540, () => CastRSmart(m));
                    }
                    if (E.IsReady() && Player.LastCastedSpellName() == "FizzMarinerDoom")
                    {
                        if (E.Instance.Name == "FizzJump")
                        {
                            //Use E1
                            castPosition = E.GetPrediction(m, false, 1).CastPosition.Extend(Player.Position, -165);
                            E.Cast(castPosition);
                        }
                        if (E.Instance.Name == "fizzjumptwo" && Player.Distance(m.Position) > 330)
                        {
                            //Use E2 if target not in range
                            castPosition = E.GetPrediction(m, false, 1).CastPosition.Extend(Player.Position, -135);
                            E.Cast(castPosition);
                        }
                    }
                    //Use W
                    if (W.IsReady() && Player.LastCastedSpellName() == "FizzJump")
                    {
                        W.Cast();
                    }
                }
            }
            else
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
        }

        private static void EFlashCombo()
        {
            //E Flash RWQ Combo
            var m = SelectedTarget;
            if (!orbwalkToTarget3 && Player.LastCastedSpellName() == "FizzJump")
            {
                orbwalkToTarget3 = true;
            }
            if (m.IsValidTarget())
            {
                if (!orbwalkToTarget3)
                {
                    Orbwalking.Orbwalk(null, Game.CursorPos);
                }
                else
                {
                    Orbwalking.Orbwalk(m ?? null, Game.CursorPos);
                }
                var distance = Player.Distance(m.Position);
                if (distance <= (E.Range + F.Range + 330))
                {
                    //E
                    if (E.IsReady() && E.Instance.Name == "FizzJump")
                    {
                        //Use E1
                        castPosition = E.GetPrediction(m, false, 1).CastPosition.Extend(Player.Position, -165);
                        E.Cast(castPosition);
                        Utility.DelayAction.Add(950, () => isEProcessed = true);
                    }
                    //Flash
                    if (F.IsReady() && !isEProcessed && Player.LastCastedSpellName() == "FizzJump" && Player.Distance(m.Position) <= F.Range + 530 && Player.Distance(m.Position) >= 330)
                    {
                        SharpDX.Vector3 endPosition = F.GetPrediction(m, false, 1).CastPosition.Extend(Player.Position, -135);
                        F.Cast(endPosition);
                    }
                    if (R.IsReady() && !F.IsReady()) 
                    {
                        CastRSmart(m);
                    }
                    if (W.IsReady() && !F.IsReady() && Player.LastCastedSpellName() == "FizzMarinerDoom")
                    {
                        W.Cast();
                    }
                    if (Q.IsReady() && !E.IsReady() && !F.IsReady() && Player.LastCastedSpellName() == "FizzSeastonePassive")
                    {
                        Q.Cast(m);
                    }
                    if (Player.LastCastedSpellName() == "FizzPiercingStrike")
                    {
                        //Check if zhonya is active
                        if (zhonya.IsOwned() && zhonya.IsReady())
                        {
                            zhonya.Cast();
                        }
                    }

                }
            }
            else
            {
                Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
        }
        #endregion
    }
}