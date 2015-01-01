using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using System.Drawing;

namespace Heimerdinger
{
    static class Program
    {
        private const string ChampionName = "Heimerdinger";

        private static Obj_AI_Hero _player;

        private static Menu _config;
        private static Orbwalking.Orbwalker _orbwalker;

        private static List<Spell> SpellList = new List<Spell>();
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;

        private static List<string> enhanceList = new List<string>(); 
        
        public static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (_player.BaseSkinName != ChampionName) return;

            CreateSpells();
            Config();
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            enhanceList.Add("Q");
            enhanceList.Add("W");
            enhanceList.Add("E");

            Game.PrintChat("Heimerdinger by Aureus Loaded!");
        }

        private static void CreateSpells()
        {
            Q = new Spell(SpellSlot.Q, 450f);
            W = new Spell(SpellSlot.W, 1100f);
            E = new Spell(SpellSlot.E, 925f);
            R = new Spell(SpellSlot.R, 0f);

            W.SetSkillshot(W.Instance.SData.SpellCastTime, W.Instance.SData.LineWidth, W.Instance.SData.MissileSpeed,
                true, SkillshotType.SkillshotLine);
            E.SetSkillshot(E.Instance.SData.SpellCastTime, E.Instance.SData.LineWidth, E.Instance.SData.MissileSpeed,
                false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var circle = _config.Item("drawW").GetValue<Circle>().Color;
            
            if (_config.Item("drawW").GetValue<Circle>().Active)
            {
                Utility.DrawCircle(_player.Position, W.Range, circle);
            }

            if (_config.Item("drawE").GetValue<Circle>().Active)
            {
                Utility.DrawCircle(_player.Position, E.Range, circle);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead) return;
            _orbwalker.SetAttack(true);

            // Combo mode
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);

                if (target == null) return;

                if (_config.Item("useE").GetValue<bool>() && E.IsReady() && target.Distance(_player) < E.Range)
                {
                    E.CastIfHitchanceEquals(target, 
                        target.IsMoving ? HitChance.High : HitChance.Medium,
                        _config.Item("usePackets").GetValue<bool>()
                        );
                }

                if (_config.Item("useW").GetValue<bool>() && W.IsReady() &&
                    target.Distance(_player) < W.Range)
                {
                    W.CastIfHitchanceEquals(target, 
                        target.IsMoving ? HitChance.High : HitChance.Medium,
                        _config.Item("usePackets").GetValue<bool>());
                }

                if (_config.Item("useQ").GetValue<bool>() && Q.IsReady() && target.Distance(_player) < Q.Range)
                {
                    Q.Cast(_player.Position);
                }
            }

            // Harass / Mixed
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                var target = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);

                if (target == null) return;
                if (_config.Item("harassW").GetValue<bool>() && W.IsReady() && target.Distance(_player) < W.Range)
                {
                    W.CastIfHitchanceEquals(target, 
                        target.IsMoving ? HitChance.High : HitChance.Medium,
                        _config.Item("usePackets").GetValue<bool>());
                }

                if (_config.Item("harassE").GetValue<bool>() && E.IsReady() && target.Distance(_player) < E.Range)
                {
                    E.CastIfHitchanceEquals(target,
                        target.IsMoving ? HitChance.High : HitChance.Medium,
                        _config.Item("usePackets").GetValue<bool>()
                        );
                }
            }

            // LaneClear
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                if (_config.Item("clearE").GetValue<bool>())
                {
                    var farmLocation =
                        MinionManager.GetBestCircularFarmLocation(
                            MinionManager.GetMinions(_player.Position, E.Range)
                                .Select(minion => minion.ServerPosition.To2D())
                                .ToList(), E.Width, E.Range);

                    if (_config.Item("minLCHit").GetValue<Slider>().Value >= farmLocation.MinionsHit &&
                        _player.Distance(farmLocation.Position) <= E.Range)
                    {
                        E.Cast(farmLocation.Position);
                    }
                }
            }
        }

        private static void Config()
        {
            _config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("TargetSelector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            _config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalker"));

            // Combo
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("useQ", "Use Q")).SetValue(false);
            _config.SubMenu("Combo").AddItem(new MenuItem("useW", "Use W")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useE", "Use E")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useR", "Use R")).SetValue(true);

            // Harass
            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("harassW", "Harass with W")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("harassE", "Harass with E")).SetValue(true);

            // Lane Clear
            _config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            _config.SubMenu("LaneClear").AddItem(new MenuItem("clearE", "LaneClear with E")).SetValue(true);
            _config.SubMenu("LaneClear")
                .AddItem(new MenuItem("minLCHit", "Min minions hit"))
                .SetValue(new Slider(3, 1, 7));

            // Smart R
            //_config.AddSubMenu(new Menu("Smart Ult options", "optSmartR"));
            //_config.SubMenu("optSmartR").AddItem(new MenuItem("rEnhance", "UPGRADE!! on ")).SetValue(enhanceList);

            // Packets
            _config.AddSubMenu(new Menu("Packets", "Packets"));
            _config.SubMenu("Packets").AddItem(new MenuItem("usePackets", "Use Packets")).SetValue(true);

            // Drawing
            _config.AddSubMenu(new Menu("Drawing", "Drawing"));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("drawW", "Draw W Range"))
                .SetValue(new Circle(true, Color.FromArgb(180 ,255, 0, 0)));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("drawE", "Draw E Range"))
                .SetValue(new Circle(true, Color.FromArgb(180, 255, 0, 0)));

            _config.AddToMainMenu();

        }
    }
}
