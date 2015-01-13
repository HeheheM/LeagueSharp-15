using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using System.Drawing;

namespace ChoGath
{
    class Program
    {
        private const string ChampionName = "Chogath";

        private static Obj_AI_Hero _player;
        
        private static Menu _config;

        private static Orbwalking.Orbwalker _orbwalker;

        private static List<Spell> SpellList = new List<Spell>();
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;

//        private static List<HitChance> HitChances = new List<HitChance>(); 


        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad+=Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (_player.BaseSkinName != ChampionName) return;

            CreateSpells();
            Config();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            Game.PrintChat("Cho'Gath by Aureus Loaded!");
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead) return;

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (_config.Item("useQ").GetValue<bool>())
            {
                if (_config.Item("smartQ").GetValue<bool>())
                {
                    if (_player.CountEnemysInRange(Q.Range) >= 2)
                    {
                        var qpred = Q.GetPrediction(target, true);
                        Q.Cast(qpred.CastPosition, _config.Item("usePackets").GetValue<bool>());
                    }
                }
                else
                {
                    Q.CastIfHitchanceEquals(target, _config.Item("qHitC").GetValue<HitChance>(), 
                        _config.Item("usePackets").GetValue<bool>());
                }
            }

            if (_config.Item("useW").GetValue<bool>())
            {
                if (_config.Item("smartW").GetValue<bool>())
                {
                    if (_player.CountEnemysInRange(W.Range) >= 2)
                    {
                        var wpred = W.GetPrediction(target, true);
                        W.Cast(wpred.CastPosition, _config.Item("usePackets").GetValue<bool>());
                    }
                }
                else
                {
                    W.CastIfHitchanceEquals(target, _config.Item("qHitC").GetValue<HitChance>(),
                        _config.Item("usePackets").GetValue<bool>());
                }
            }

            if (_config.Item("useR").GetValue<bool>())
            {
                foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && !enemy.IsDead && _player.Distance(enemy) <= R.Range))
                {
                    if (enemy.Health <=
                        _player.GetSpellDamage(enemy, SpellSlot.R))
                    {
                        R.Cast(enemy);
                    }
                }
            }
        }

        private static void LaneClear()
        {
            if (_config.Item("laneQ").GetValue<bool>() && Q.IsReady())
            {
                var castpos = Q.GetCircularFarmLocation(
                    MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .Select(minion => minion.ServerPosition.To2D())
                        .ToList(), Q.Width);

                if (castpos.MinionsHit >= _config.Item("minHit").GetValue<Slider>().Value)
                {
                    Q.Cast(castpos.Position, _config.Item("usePackets").GetValue<bool>());
                }
            }
            if (_config.Item("laneW").GetValue<bool>() && W.IsReady())
            {
                var castpos =
                    W.GetCircularFarmLocation(
                        MinionManager.GetMinions(W.Range, MinionTypes.All, MinionTeam.NotAlly)
                            .Select(x => x.ServerPosition.To2D())
                            .ToList(), W.Width);

                if (castpos.MinionsHit >= _config.Item("minHit").GetValue<Slider>().Value)
                {
                    W.Cast(castpos.Position, _config.Item("usePackets").GetValue<bool>());
                }
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var item = _config.Item("draw" + spell.Slot).GetValue<Circle>();
                if (item.Active)
                {
                    Render.Circle.DrawCircle(_player.Position, spell.Range, item.Color);
                }
            }
        }


        static void CreateSpells()
        {
            Q = new Spell(SpellSlot.Q, 950f);
            W = new Spell(SpellSlot.W, 300f);
            R = new Spell(SpellSlot.R, 175f);

            Q.SetSkillshot(Q.Instance.SData.SpellCastTime, 250f, Q.Instance.SData.MissileSpeed, false,
                SkillshotType.SkillshotCircle);
            W.SetSkillshot(W.Instance.SData.SpellCastTime, 210f, W.Instance.SData.MissileSpeed, false,
                SkillshotType.SkillshotCone);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(R);
        }

        static void Config()
        {
            _config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("TargetSelector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            _orbwalker = new Orbwalking.Orbwalker(_config.AddSubMenu(new Menu("Orbwalker", "Orbwalker")));

//            HitChances.Add(HitChance.Low);
//            HitChances.Add(HitChance.Medium);
//            HitChances.Add(HitChance.High);
//            HitChances.Add(HitChance.VeryHigh);

            // Combo
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("useQ", "Use Q")).SetValue(true);
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("qHitC", "Q HitChance"))
//                .SetValue(HitChances);
                .SetValue(new List<HitChance>(new[] {HitChance.Low, HitChance.Medium, HitChance.High}));
            _config.SubMenu("Combo").AddItem(new MenuItem("smartQ", "Smart Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useW", "Use W")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("smartW", "Smart W")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useE", "Use R on Killable")).SetValue(true);

            // Lane Clear
            _config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            _config.SubMenu("LaneClear").AddItem(new MenuItem("laneQ", "Use Q")).SetValue(true);
            _config.SubMenu("LaneClear").AddItem(new MenuItem("laneW", "Use W")).SetValue(true);
            _config.SubMenu("LaneClear").AddItem(new MenuItem("minHit", "Min Hit")).SetValue(new Slider(3, 1, 7));

            // Packets
            _config.AddSubMenu(new Menu("Packets", "Packets"));
            _config.SubMenu("Packets").AddItem(new MenuItem("usePackets", "Use Packets")).SetValue(true);

            // Drawing
            _config.AddSubMenu(new Menu("Drawing", "Drawing"));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("drawQ", "Draw Q Range"))
                .SetValue(new Circle(true, Color.Red, Q.Range));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("drawW", "Draw W Range"))
                .SetValue(new Circle(true, Color.Red, W.Range));
            _config.SubMenu("Drawing").AddItem(new MenuItem("DamageIndicator", "DamageIndicator")).SetValue(true);
            _config.Item("DamageIndicator").ValueChanged += Program_ValueChanged;

            _config.AddToMainMenu();

            Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
            Utility.HpBarDamageIndicator.Enabled = _config.Item("DamageIndicator").GetValue<bool>();
        }

        private static float GetComboDamage(Obj_AI_Hero hero)
        {
            var damage = 0d;

            if (Q.IsReady())
            {
                damage += _player.GetSpellDamage(hero, SpellSlot.Q);
            }

            if (W.IsReady())
            {
                damage += _player.GetSpellDamage(hero, SpellSlot.W);
            }

            if (R.IsReady())
            {
                damage += _player.GetSpellDamage(hero, SpellSlot.R);
            }

            return (float) damage;
        }

        static void Program_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            Utility.HpBarDamageIndicator.Enabled = e.GetNewValue<bool>();
        }
    }
}
