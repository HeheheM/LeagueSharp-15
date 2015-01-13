using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;
using System.Drawing;
using System.Threading;
using xSLx_Orbwalker;

namespace Swain
{
    class Program
    {
        private const string ChampionName = "Swain";

        private static Obj_AI_Hero _player;

        private static List<Spell> SpellList = new List<Spell>();
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;
        private static SpellDataInst Ignite;

        private static Menu _config;
        private static bool _usePackets;

        private static Obj_AI_Hero target;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (_player.BaseSkinName != ChampionName) return;

            CreateSpells();
            Config();

            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;

            Game.PrintChat("Swain by Aureus Loaded!");
        }

        #region Drawings
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
        #endregion

        public static void Game_OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead) return;

            if (xSLxOrbwalker.CurrentMode == xSLxOrbwalker.Mode.Combo)
            {
                target = TargetSelector.GetTarget(_player, W.Range, TargetSelector.DamageType.Magical);
                UseItems(target);
                Combo();
            }

            if (xSLxOrbwalker.CurrentMode == xSLxOrbwalker.Mode.Harass)
            {
                if (_player.Mana >= (_player.MaxMana * (_config.Item("harassMana").GetValue<Slider>().Value / 100f)))
                {
                    Harass();
                }
            }

            if (xSLxOrbwalker.CurrentMode == xSLxOrbwalker.Mode.LaneClear)
            {
                LaneClear();
            }

            if (_config.Item("autoIgnite").GetValue<bool>())
            {
                #region Auto Ignite
                var target = TargetSelector.GetTarget(_player, 600f, TargetSelector.DamageType.True);

                if (target.Health <= _player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite))
                {
                    _player.Spellbook.CastSpell(Ignite.Slot, target);
                }
                #endregion
            }
        }

        static void UseItems(Obj_AI_Base hero)
        {
            if (_config.Item("useItems").GetValue<bool>())
            {
                if (target.Team != _player.Team)
                {
                    // DFG
                    if (Items.HasItem(ItemData.Deathfire_Grasp.Id, _player) && Items.CanUseItem(ItemData.Deathfire_Grasp.Id))
                    {
                        Items.UseItem(ItemData.Deathfire_Grasp.Id, hero);
                    }

                    // Gunblade
                    if (Items.HasItem(ItemData.Hextech_Gunblade.Id, _player) && Items.CanUseItem(ItemData.Hextech_Gunblade.Id))
                    {
                        Items.UseItem(ItemData.Hextech_Gunblade.Id, hero);
                    }

                    // Seraph
                    if (Items.HasItem(3040, _player) && 
                        Items.CanUseItem(3040) &&
                        _player.CountEnemysInRange(E.Range) >= 1 &&
                        _player.Health <= (_player.MaxHealth * (_config.Item("minSER").GetValue<Slider>().Value / 100f)))
                    {
                        Items.UseItem(3040);
                    }

                     // Zhonya
                    if (Items.HasItem(ItemData.Zhonyas_Hourglass.Id, _player) &&
                        Items.CanUseItem(ItemData.Zhonyas_Hourglass.Id) &&
                        _player.CountEnemysInRange(E.Range) >= 1 &&
                        _player.Health <= (_player.MaxHealth*(_config.Item("minZHG").GetValue<Slider>().Value / 100f)))
                    {
                        Items.UseItem(ItemData.Zhonyas_Hourglass.Id);
                    }
                }
            }
        }

        #region Combo
        static void Combo()
        {
            
            if (target == null) return;

            if (_player.CountEnemysInRange(R.Range) == 0 && _player.HasBuff("SwainMetamorphism") && _config.Item("autoDisableR").GetValue<bool>())
            {
                R.Cast();
            }

            if (_config.Item("useE").GetValue<bool>() && _player.Distance(target) <= E.Range && E.IsReady())
            {
                E.Cast(target, _usePackets);
            }

            if (_config.Item("useQ").GetValue<bool>() && _player.Distance(target) <= Q.Range && Q.IsReady())
            {
                Q.Cast(target, _usePackets);
            }

            if (_config.Item("qBeforeW").GetValue<bool>() && _player.Distance(target) <= Q.Range && Q.IsReady() && W.IsReady())
            {
                Q.Cast(target, _usePackets);
                if (target.HasBuffOfType(BuffType.Slow) || target.HasBuffOfType(BuffType.Stun))
                {
                    var hitc = new HitChance[] { HitChance.Low, HitChance.Medium, HitChance.High, HitChance.VeryHigh };

                    W.CastIfHitchanceEquals(target, hitc[_config.Item("hitW").GetValue<StringList>().SelectedIndex],
                        _usePackets);
                }
            }
            else if (!_config.Item("qBeforeW").GetValue<bool>() && _config.Item("useW").GetValue<bool>() && _player.Distance(target) <= W.Range && W.IsReady())
            {
                var hitc = new HitChance[] {HitChance.Low, HitChance.Medium, HitChance.High, HitChance.VeryHigh};

                W.CastIfHitchanceEquals(target, hitc[_config.Item("hitW").GetValue<StringList>().SelectedIndex],
                    _usePackets);
            }

            if (_config.Item("useR").GetValue<bool>() && _player.Distance(target) <= R.Range && R.IsReady())
            {
                if (_player.CountEnemysInRange(R.Range) >= _config.Item("minR").GetValue<Slider>().Value)
                {
                    if (!_player.HasBuff("SwainMetamorphism"))
                    {
                        R.Cast();
                    }
                }
            }
            
        }
        #endregion

        #region Harass
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(_player, W.Range, TargetSelector.DamageType.Magical);

            if (target == null) return;

            if (_config.Item("harassQ").GetValue<bool>() && Q.IsReady() && _player.Distance(target) <= Q.Range)
            {
                Q.Cast(target, _usePackets);
            }

            if (_config.Item("harassW").GetValue<bool>() && W.IsReady() && _player.Distance(target) <= W.Range)
            {
                W.Cast(target, _usePackets, true);
            }

            if (_config.Item("harassE").GetValue<bool>() && E.IsReady() && _player.Distance(target) <= E.Range)
            {
                E.Cast(target, _usePackets);
            }
        }
        #endregion

        #region LaneClear
        private static void LaneClear()
        {
            var farmlocation =
                MinionManager.GetBestCircularFarmLocation(
                    MinionManager.GetMinions(_player.Position, W.Range, MinionTypes.All, MinionTeam.NotAlly)
                        .Where(minion => minion.IsEnemy && !minion.IsDead)
                        .Select(x => x.ServerPosition.To2D())
                        .ToList(), W.Width, W.Range);

            if (farmlocation.MinionsHit >= 3 && _config.Item("laneW").GetValue<bool>())
            {
                W.Cast(farmlocation.Position);
            }

            if (MinionManager.GetMinions(_player.Position, R.Range, MinionTypes.All, MinionTeam.NotAlly).Count == 0 && _player.HasBuff("SwainMetamorphism"))
            {
                R.Cast();
            }
            if (_config.Item("laneR").GetValue<bool>() &&
                MinionManager.GetMinions(_player.Position, R.Range, MinionTypes.All, MinionTeam.NotAlly).Count(x => x.IsEnemy && !x.IsDead) >= 3 &&
                !_player.HasBuff("SwainMetamorphism"))
            {
                R.Cast();
            }
        }
        #endregion

        #region Configs
        static void Config()
        {
            _config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("TargetSelector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            xSLxOrbwalker.AddToMenu(_config.AddSubMenu(new Menu("Orbwalker", "Orbwalker")));

            // Ignite
            Ignite = _player.Spellbook.GetSpell(_player.GetSpellSlot("summonerdot"));

            // Combo
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("useQ", "Use Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useW", "Use W")).SetValue(true);
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("hitW", "W HitChance"))
                .SetValue(new StringList(new[] {"Low", "Medium", "High", "Very High"}, 2));
            _config.SubMenu("Combo").AddItem(new MenuItem("qBeforeW", "Always Q before W")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useE", "Use E")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useR", "Use Ult")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("minR", "Min enemies to ult")).SetValue(new Slider(2, 1, 5));
            _config.SubMenu("Combo").AddItem(new MenuItem("autoDisableR", "Automatically Disable Ult")).SetValue(true);

            // Harass
            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass").AddItem(new MenuItem("harassQ", "Use Q")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("harassW", "Use W")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("harassE", "Use E")).SetValue(true);
            _config.SubMenu("Harass").AddItem(new MenuItem("harassMana", "Min mana %")).SetValue(new Slider(50, 0, 100));

            // LaneClear
            _config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            _config.SubMenu("LaneClear").AddItem(new MenuItem("laneW", "Use W")).SetValue(true);
            _config.SubMenu("LaneClear").AddItem(new MenuItem("laneR", "Use Ult")).SetValue(true);

            // Items
            _config.AddSubMenu(new Menu("Items", "Items"));
            _config.SubMenu("Items").AddItem(new MenuItem("useItems", "Use Items")).SetValue(true);
            _config.SubMenu("Items").AddItem(new MenuItem("DFG", "Use DFG")).SetValue(true);
            _config.SubMenu("Items").AddItem(new MenuItem("HGB", "Use Gunblade")).SetValue(true);
            _config.SubMenu("Items").AddItem(new MenuItem("ZHG", "Use Zhonya")).SetValue(true);
            _config.SubMenu("Items")
                .AddItem(new MenuItem("minZHG", "Health to Zhonya"))
                .SetValue(new Slider(35, 1, 100));
            _config.SubMenu("Items").AddItem(new MenuItem("SER", "Use Seraphs Embrace")).SetValue(true);
            _config.SubMenu("Items")
                .AddItem(new MenuItem("minSER", "Health to Seraph"))
                .SetValue(new Slider(50, 1, 100));

            // Packets
            _config.AddSubMenu(new Menu("Packets", "Packets"));
            _config.SubMenu("Packets").AddItem(new MenuItem("usePackets", "Use Packets")).SetValue(true);
            _usePackets = _config.SubMenu("Packets").Item("usePackets").GetValue<bool>();

            // Misc
            _config.AddSubMenu(new Menu("Misc", "Misc"));
            _config.SubMenu("Misc").AddItem(new MenuItem("autoIgnite", "Auto Ignite")).SetValue(true);
            new PotionManager(_config.SubMenu("Misc"));

            // Drawings
            _config.AddSubMenu(new Menu("Drawing", "Drawing"));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("drawQ", "Draw Q Range"))
                .SetValue(new Circle(true, Color.FromArgb(150, Color.Red), Q.Range));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("drawW", "Draw W Range"))
                .SetValue(new Circle(true, Color.FromArgb(150, Color.Red), W.Range));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("drawE", "Draw E Range"))
                .SetValue(new Circle(true, Color.FromArgb(150, Color.Red), E.Range));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("drawR", "Draw R Range"))
                .SetValue(new Circle(true, Color.FromArgb(150, Color.Red), R.Range));
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

            if (E.IsReady())
            {
                damage += _player.GetSpellDamage(hero, SpellSlot.E);
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

        static void CreateSpells()
        {
            Q = new Spell(SpellSlot.Q, 625f);
            W = new Spell(SpellSlot.W, 900f);
            E = new Spell(SpellSlot.E, 625f);
            R = new Spell(SpellSlot.R, 600f);

            W.SetSkillshot(0.85f, 125f, float.MaxValue, false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }
        #endregion Configs
    }
}
