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

        private static Obj_AI_Hero DrawTarget;
        private static Geometry.Polygon.Rectangle RRectangle;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (Player.ChampionName != "Fizz") return;
            Q = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W, Orbwalking.GetRealAutoAttackRange(Player));
            E = new Spell(SpellSlot.E, 400);
            R = new Spell(SpellSlot.R, 1300);
            F = new Spell(Player.GetSpellSlot("summonerflash"), 425);

            E.SetSkillshot(0.25f, 330, float.MaxValue, false, SkillshotType.SkillshotCircle);
            R.SetSkillshot(0.25f, 80, 1200, false, SkillshotType.SkillshotLine);

            RRectangle = new Geometry.Polygon.Rectangle(Player.Position, Player.Position, 300);

            Menu = new Menu(Player.ChampionName, Player.ChampionName, true);
            var orbwalkerMenu = Menu.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            Orbwalker = new Orbwalking.Orbwalker(orbwalkerMenu);
            //Combo Menu
            var combo = new Menu("Combo", "Combo");
            Menu.AddSubMenu(combo);
            combo.AddItem(new MenuItem("ComboMode", "ComboMode").SetValue(new StringList(new[] { "R after Dash", "R on Dash", "R to gapclose" })));
            combo.AddItem(new MenuItem("Combo", "Combo"));
            combo.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("useW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("useE", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("useR", "Use R").SetValue(true));
            //Harass Menu
            var harass = new Menu("Harass", "Harass");
            Menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("useharassQ", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("useharassW", "Use W").SetValue(true));
            harass.AddItem(new MenuItem("useharassE", "Use E").SetValue(true));
            harass.AddItem(new MenuItem("harassmana", "Min Harass Mana").SetValue(new Slider(30)));
            //LaneClear Menu
            var lc = new Menu("Laneclear", "Laneclear");
            Menu.AddSubMenu(lc);
            lc.AddItem(new MenuItem("laneclearQ", "Use Q to LaneClear").SetValue(true));
            lc.AddItem(new MenuItem("laneclearW", "Use W to LaneClear").SetValue(true));
            lc.AddItem(new MenuItem("laneclearE", "Use E to LaneClear").SetValue(true));
            lc.AddItem(new MenuItem("lanemana", "Min Farm Mana").SetValue(new Slider(30)));
            //JungleClear Menu
            var jungle = new Menu("JungleClear", "JungleClear");
            Menu.AddSubMenu(jungle);
            jungle.AddItem(new MenuItem("jungleclearQ", "Use Q to JungleClear").SetValue(true));
            jungle.AddItem(new MenuItem("jungleclearW", "Use W to JungleClear").SetValue(true));
            jungle.AddItem(new MenuItem("jungleclearE", "Use E to JungleClear").SetValue(true));
            jungle.AddItem(new MenuItem("junglemana", "Min Jungle Mana").SetValue(new Slider(30)));
            //CustomCombo Menu
            var customCombo = new Menu("CustomCombo (require a selected target!)", "CustomCombo").SetFontStyle(FontStyle.Bold, fontColor: SharpDX.Color.Yellow);
            Menu.AddSubMenu(customCombo);
            customCombo.AddItem(new MenuItem("lateGameZhonyaCombo", "E to gapclose RWQ zhonya").SetValue(new KeyBind("G".ToCharArray()[0], KeyBindType.Press)));
            customCombo.AddItem(new MenuItem("QminionREWCombo", "Q minion to gapclose REW").SetValue(new KeyBind("H".ToCharArray()[0], KeyBindType.Press)));
            customCombo.AddItem(new MenuItem("EFlashCombo", "EE Flash On Target RWQ").SetValue(new KeyBind("J".ToCharArray()[0], KeyBindType.Press)));
            //Misc Menu
            var miscMenu = new Menu("Misc", "Misc");
            Menu.AddSubMenu(miscMenu);
            miscMenu.AddItem(new MenuItem("drawQ", "Draw Q range").SetValue(false));
            miscMenu.AddItem(new MenuItem("drawAa", "Draw Autoattack range").SetValue(false));
            miscMenu.AddItem(new MenuItem("drawMinionQCombo", "Draw QminionREWCombo helper").SetValue(true));
            miscMenu.AddItem(new MenuItem("Flee", "Flee Key").SetValue(new KeyBind("Q".ToCharArray()[0], KeyBindType.Press)));
            miscMenu.AddItem(new MenuItem("useFleeE", "Use E to Flee").SetValue(true));
            //Author Menu
            var about = new Menu("About", "About").SetFontStyle(FontStyle.Regular, fontColor: SharpDX.Color.Gray);
            Menu.AddSubMenu(about);
            about.AddItem(new MenuItem("Author", "Author: mathieu002"));
            about.AddItem(new MenuItem("Credits", "ChewyMoon,1Shinigamix3,jQuery,Kurisu"));

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
            //Orbwalking.AfterAttack += AfterAa;
            Drawing.OnDraw += OnDraw;
        }
        private static void OnDoCast()
        {
            Obj_AI_Base.OnDoCast += (sender, args) =>
            {
                if (sender.IsMe && args.SData.IsAutoAttack())
                {
                    var useE = (Menu.Item("useE").GetValue<bool>() && E.IsReady());
                    var useR = (Menu.Item("useR").GetValue<bool>() && R.IsReady());
                    var ondash = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 1);
                    var afterdash = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 0);
                    var gapclose = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 2);
                    var target = (Obj_AI_Hero)args.Target;
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                    {
                        if (ondash)
                        {
                            if (useE && !R.IsReady() && E.Instance.Name == "FizzJump" && Player.Distance(target.Position) < E.Range) E.Cast(E.GetPrediction(target, false, 0.5f).CastPosition);
                        }
                        if (afterdash)
                        {
                            if (useE && !R.IsReady() && E.Instance.Name == "FizzJump" && Player.Distance(target.Position) < E.Range) E.Cast(E.GetPrediction(target, false, 0.5f).CastPosition);
                        }
                        if (gapclose)
                        {
                            if (useE && !R.IsReady() && E.Instance.Name == "FizzJump" && Player.Distance(target.Position) < E.Range) E.Cast(E.GetPrediction(target, false, 0.5f).CastPosition);
                        }
                    }
                    if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                    {
                        if ((Menu.Item("useharassE").GetValue<bool>() && E.IsReady()) && !W.IsReady() && !Q.IsReady()) E.Cast(Game.CursorPos);
                    }
                }
            };
        }

        private static void OnDraw(EventArgs args)
        {
            if (Menu.Item("drawQ").GetValue<bool>())
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, System.Drawing.Color.DarkRed, 3);
            }
            if (Menu.Item("drawAa").GetValue<bool>())
            {
                Render.Circle.DrawCircle(Player.Position, Orbwalking.GetRealAutoAttackRange(Player), System.Drawing.Color.Blue);
            }
            if (Menu.Item("drawMinionQCombo").GetValue<bool>() && DrawTarget.IsValidTarget())
            {
                if (Player.Distance(DrawTarget) <= R.Range + Q.Range)
                {
                    RRectangle.Draw(Color.CornflowerBlue, 3);
                }
            }
        }
        private static void OnUpdate(EventArgs args)
        {
            DrawTarget = TargetSelector.GetSelectedTarget();

            if (DrawTarget.IsValidTarget())
            {
                if (Player.Distance(DrawTarget) <= R.Range + Q.Range+100) 
                {
                    RRectangle.Start = Player.Position.Shorten(DrawTarget.Position, -250).To2D();
                    RRectangle.End = R.GetPrediction(DrawTarget,false,1f).CastPosition.To2D();
                    RRectangle.UpdatePolygon();
                }
            }
            if (Player.IsDead || Player.IsRecalling())
            {
                return;
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
            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                Harass();
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
        private static void Flee()
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (Menu.Item("useFleeE").GetValue<bool>() && E.IsReady())
            {
                E.Cast(Game.CursorPos);
            }
        }
         
        //R usage
        public static void CastRSmart(Obj_AI_Hero target)
        {
            var castStartPosition = R.GetPrediction(target, false, 0.1f).CastPosition;
            var castPosition = Player.ServerPosition.Extend(castStartPosition, 800);

            R.Cast(castPosition);
        }
        //Lane&JungleClear
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
                    Q.CastOnUnit(minion);
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
                Q.CastOnUnit(mob);
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
        private static void Harass()
        {
            var useQ = (Menu.Item("useharassQ").GetValue<bool>() && Q.IsReady());
            var useW = (Menu.Item("useharassW").GetValue<bool>() && W.IsReady());
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            if (ObjectManager.Player.ManaPercent < Menu.Item("harassmana").GetValue<Slider>().Value)
            {
                return;
            }
            if (useW && (Player.Distance(target.Position) < Q.Range)) W.Cast();
            if (useQ && Player.Distance(target.Position) > 175) Q.CastOnUnit(target);
                      
        }
        private static void Combo()
        {
            var useQ = (Menu.Item("useQ").GetValue<bool>() && Q.IsReady());
            var useW = (Menu.Item("useW").GetValue<bool>() && W.IsReady());
            var useE = (Menu.Item("useE").GetValue<bool>() && E.IsReady());
            var useR = (Menu.Item("useR").GetValue<bool>() && R.IsReady());
            var gapclose = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 2);
            var ondash = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 1);
            var afterdash = (Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex == 0);
            var m = TargetSelector.GetSelectedTarget();
            var target = TargetSelector.GetSelectedTarget();
            var spellName = Player.LastCastedSpellName();

            if (!m.IsValidTarget()) 
            {
                m = TargetSelector.GetTarget(800, TargetSelector.DamageType.Magical);
            }
            if (!target.IsValidTarget()) 
            {
                target = TargetSelector.GetTarget(1250, TargetSelector.DamageType.Magical);
            }
            if (ondash && !m.IsZombie)
            {
                if (useW && Player.Distance(m.Position) <= 595)
                {
                    W.Cast();
                }
                if (useQ)
                {
                    Q.Cast(m);
                }
                if (useR && spellName == "FizzPiercingStrike" && m.HealthPercent >= 35)
                {
                    //Prevent R to late
                    if (Player.Distance(m.Position) >= 425)
                    {
                        CastRSmart(m);
                    }
                    if (Player.Distance(m.Position) < 425)
                    {
                        Utility.DelayAction.Add(500, () => CastRSmart(m));
                    }
                }
                if (hydra.IsOwned() && Player.Distance(m) < hydra.Range && hydra.IsReady() && !E.IsReady()) hydra.Cast();
                if (tiamat.IsOwned() && Player.Distance(m) < tiamat.Range && tiamat.IsReady() && !E.IsReady()) tiamat.Cast();
            }
            if (afterdash && !m.IsZombie)
            {

                if (useW && Player.Distance(m.Position) <= 595) W.Cast();
                if (useQ) Q.Cast(m);
                if (useR && spellName == "FizzPiercingStrike" && m.HealthPercent >= 35)
                {
                    Utility.DelayAction.Add(550, () => CastRSmart(m));
                }
                if (hydra.IsOwned() && Player.Distance(m) < hydra.Range && hydra.IsReady() && !E.IsReady()) hydra.Cast();
                if (tiamat.IsOwned() && Player.Distance(m) < tiamat.Range && tiamat.IsReady() && !E.IsReady()) tiamat.Cast();
            }
            if (gapclose && !target.IsZombie)
            {
                if (useR && (Player.Distance(target.Position) <= (R.Range - 350)) && target.HealthPercent >= 35)
                {
                    CastRSmart(target);
                }
                if (useQ) Q.Cast(target);
                if (useW && Player.Distance(target.Position) < 595) W.Cast();
                if (hydra.IsOwned() && Player.Distance(target) < hydra.Range && hydra.IsReady() && !E.IsReady()) hydra.Cast();
                if (tiamat.IsOwned() && Player.Distance(target) < tiamat.Range && tiamat.IsReady() && !E.IsReady()) tiamat.Cast();
            }
        }
        private static void lateGameZhonyaCombo()
        {
            var m = TargetSelector.SelectedTarget;

            if (m.IsValidTarget())
            {
                Orbwalking.Orbwalk(m ?? null, Game.CursorPos);
                var distance = Player.Distance(m.Position);
                //Check distance
                if (distance <= (E.Range + Q.Range + E.Range))
                {
                    if (E.IsReady())
                    {
                       //Use E1
                       E.Cast(E.GetPrediction(m, false, 0.5f).CastPosition);
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
                        Q.CastOnUnit(m);
                    }
                    var spellName = Player.LastCastedSpellName();
                    if (spellName == "FizzPiercingStrike")
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
            var m = TargetSelector.SelectedTarget;

            if (m.IsValidTarget()) 
            {
                Orbwalking.Orbwalk(m ?? null, Game.CursorPos);
                var distance = Player.Distance(m.Position);
                if (distance <= ((Q.Range + R.Range) - 600) && distance >= Q.Range) 
                {
                    var spellName = Player.LastCastedSpellName();
                    if (Q.IsReady()) 
                    {
                        //Check if HeroTarget is in Q.Range then Q 
                        if (Player.Distance(m.Position) < Q.Range)
                        {
                           Q.CastOnUnit(m);
                        }
                        //Check if minions in rectangle is in Q.Range then Q
                        var mins = MinionManager.GetMinions(Q.Range);
                        foreach (Obj_AI_Base min in mins){
                            if (RRectangle.IsInside(min.Position) && min.Distance(m.Position) > 300) 
                            {
                                Q.CastOnUnit(min);
                            }
                        }
                    }
                    if (R.IsReady() && spellName == "FizzPiercingStrike")
                    {
                        //Use R
                        CastRSmart(m);
                    }
                    if (E.IsReady() && spellName == "FizzMarinerDoom")
                    {
                        if (E.Instance.Name == "FizzJump")
                        {
                            //Use E1
                            E.Cast(E.GetPrediction(m, false, 0.5f).CastPosition);
                        }
                        if (E.Instance.Name == "fizzjumptwo" && Player.Distance(m.Position) > E.Range)
                        {
                            //Use E2 if target not in range
                            E.Cast(E.GetPrediction(m, false, 0.5f).CastPosition);
                        }
                    }
                    //Use W
                    if (W.IsReady() && spellName == "FizzJump")
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
            var m = TargetSelector.SelectedTarget;

            if (m.IsValidTarget())
            {
                Orbwalking.Orbwalk(m ?? null, Game.CursorPos);
                var distance = Player.Distance(m.Position);
                if (distance <= (E.Range + F.Range + 350))
                {
                    var spellName = Player.LastCastedSpellName();

                    //E
                    if (E.IsReady() && E.Instance.Name == "FizzJump")
                    {
                        //Use E1
                        E.Cast(E.GetPrediction(m, false, 0.5f).CastPosition);
                    }
                    //Flash
                    if (F.IsReady() && Player.Distance(m.Position) <= F.Range+500 && Player.Distance(m.Position) >= 350)
                    {
                        Utility.DelayAction.Add(25, () => F.Cast(Player.Position.Extend(m.Position, F.Range)));
                    }
                    if (R.IsReady() && !E.IsReady() && !F.IsReady()) 
                    {
                        CastRSmart(m);
                    }
                    if (W.IsReady() && !E.IsReady() && !F.IsReady() && spellName == "FizzMarinerDoom" )
                    {
                        W.Cast();
                    }
                    if (Q.IsReady() && !E.IsReady() && !F.IsReady() && spellName == "FizzSeastonePassive")
                    {
                        Q.CastOnUnit(m);
                    }
                    if (spellName == "FizzPiercingStrike")
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



    }
}