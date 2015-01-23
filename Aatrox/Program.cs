using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.Drawing;
using SharpDX;
using Color = System.Drawing.Color;

namespace Aatrox
{
    static class Program
    {
        private const String ChampionName = "Aatrox";

        private static Obj_AI_Hero _player;

        private static Menu _config;
        private static Orbwalking.Orbwalker _orbwalker;

        public static Obj_AI_Hero target;

        private static List<Spell> SpellList = new List<Spell>();
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;
        private static SpellDataInst Ignite;


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
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;

            Game.PrintChat("Aatrox by Aureus Loaded!");
        }

        static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;

            if ((_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo || _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear) &&
                _config.Item("useItems").GetValue<bool>() &&
                _config.Item("HYDRA").GetValue<bool>())
            {
                var hydra = Items.HasItem(LeagueSharp.Common.Data.ItemData.Ravenous_Hydra_Melee_Only.Id);
                var tiamat = Items.HasItem(LeagueSharp.Common.Data.ItemData.Tiamat_Melee_Only.Id);


                if (hydra)
                {
                    if (Items.CanUseItem(LeagueSharp.Common.Data.ItemData.Ravenous_Hydra_Melee_Only.Id))
                    {
                        Items.UseItem(LeagueSharp.Common.Data.ItemData.Ravenous_Hydra_Melee_Only.Id);
                    }
                }

                if (tiamat)
                {
                    if (Items.CanUseItem(LeagueSharp.Common.Data.ItemData.Tiamat_Melee_Only.Id))
                    {
                        Items.UseItem(LeagueSharp.Common.Data.ItemData.Tiamat_Melee_Only.Id);
                    }
                }
            }
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            var circle = _config.Item("drawQ").GetValue<Circle>().Color;

            if (_config.Item("drawQ").GetValue<bool>())
            {
                Drawing.DrawCircle(_player.Position, Q.Range, circle);
            }

            if (_config.Item("drawE").GetValue<bool>())
            {
                Drawing.DrawCircle(_player.Position, E.Range, circle);
            }
        }

        public static void Game_OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead) return;
            _orbwalker.SetAttack(true);

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);

                UseItems();
                Combo();
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                var target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Physical);
                Harass(target);
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
            }
        }

        private static void UseItems()
        {
            if (target == null) return;
            if (target.IsDead) return;

            if (_config.Item("useItems").GetValue<bool>())
            {
                if (_config.Item("BOTRK").GetValue<bool>())
                {
                    var botrk = Items.HasItem(LeagueSharp.Common.Data.ItemData.Blade_of_the_Ruined_King.Id);
                    var cutlass = Items.HasItem(LeagueSharp.Common.Data.ItemData.Bilgewater_Cutlass.Id);

                    if (botrk || cutlass)
                    {
                        if (botrk)
                        {
                            Items.UseItem(LeagueSharp.Common.Data.ItemData.Blade_of_the_Ruined_King.Id, target);
                        }
                        else
                        {
                            Items.UseItem(LeagueSharp.Common.Data.ItemData.Bilgewater_Cutlass.Id, target);
                        }
                    }
                }

                if (_config.Item("OMEN").GetValue<bool>())
                {
                    var omen = Items.HasItem(LeagueSharp.Common.Data.ItemData.Randuins_Omen.Id);

                    if (omen)
                    {
                        var champcount = 0;

                        foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && !x.IsDead))
                        {
                            champcount++;
                        }

                        if (champcount >= _config.Item("omenCount").GetValue<Slider>().Value)
                        {
                            if (Items.CanUseItem(LeagueSharp.Common.Data.ItemData.Randuins_Omen.Id))
                            {
                                Items.UseItem(LeagueSharp.Common.Data.ItemData.Randuins_Omen.Id);
                            }
                        }
                    }
                }

                if (_config.Item("GBLADE").GetValue<bool>())
                {
                    var gblade = Items.HasItem(LeagueSharp.Common.Data.ItemData.Youmuus_Ghostblade.ToString());

                    if (gblade)
                    {
                        if (Items.CanUseItem(LeagueSharp.Common.Data.ItemData.Youmuus_Ghostblade.ToString()))
                        {
                            Items.UseItem(LeagueSharp.Common.Data.ItemData.Youmuus_Ghostblade.ToString());
                        }
                    }
                }
            }
        }

        private static void Combo()
        {
            if (_config.Item("useW").GetValue<bool>())
            {
                if (_player.Health > (_player.MaxHealth * (_config.Item("useWHealth").GetValue<Slider>().Value / 100f)) &&
                    W.Instance.SData.Name == "AatroxW")
                {
                    W.Cast();
                }
                else if (_player.Health < (_player.MaxHealth * (_config.Item("useWHealth").GetValue<Slider>().Value / 100f)) &&
                         W.Instance.SData.Name == "aatroxw2")
                {
                    W.Cast();
                }
            }



            if (target == null) return;

            if (_config.Item("useE").GetValue<bool>())
            {
                CastE(target);
            }

            if (_config.Item("useQ").GetValue<bool>())
            {
                CastQ(target);
            }

            if (_config.Item("useR").GetValue<bool>())
            {
                CastR(target);
            }

            if (_config.Item("killSteal").GetValue<bool>())
            {
                KillSteal();
            }
        }

        private static void KillSteal()
        {
            foreach(var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsEnemy && !x.IsDead))
            {
                if (enemy.IsValidTarget(E.Range) && enemy.IsVisible)
                {
                    var qDmg = Q.GetDamage(enemy);
                    var eDmg = E.GetDamage(enemy);
//                    var rdmg = R.GetDamage(enemy);

                    if (enemy.Health <= qDmg)
                    {
                        Q.Cast(enemy);
                    }
                    else if (enemy.Health <= eDmg)
                    {
                        E.Cast(enemy);
                    }
                    else if (enemy.Health <= qDmg + eDmg)
                    {
                        E.Cast(enemy);
                        Q.Cast(enemy);
                    }

                    if (_config.Item("autoIgnite").GetValue<bool>())
                    {
                        AutoIgnite(enemy);
                    }
                }
            }
        }

        private static void AutoIgnite(Obj_AI_Hero enemy)
        {
            if (enemy.IsValidTarget(600) &&
                enemy.Health <= _player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite))
            {
                _player.Spellbook.CastSpell(Ignite.Slot, enemy);
            }
        }

        private static void CastR(Obj_AI_Hero target)
        {
            if (R.IsReady() && _player.Distance(target) <= R.Range)
            {
                R.Cast();
            }
        }

        private static void CastE(Obj_AI_Hero target)
        {
            if (_player.Distance(target) <= E.Range && E.IsReady())
            {
                E.Cast(target, _config.Item("usePackets").GetValue<bool>());
            }
        }

        private static void CastQ(Obj_AI_Base target)
        {
            if (_player.Distance(target) <= Q.Range && Q.IsReady())
            {
                Q.Cast(target, _config.Item("usePackets").GetValue<bool>());
            }
        }

        private static void Harass(Obj_AI_Hero target)
        {
            if (target.IsValidTarget(E.Range, true, _player.Position) && target.Type == _player.Type && !IsMyHealthLow("Harass"))
            {
                if (_config.Item("harassMode").GetValue<StringList>().SelectedIndex == 0)
                {
                    if (_config.Item("harassE").GetValue<bool>())
                    {
                        CastE(target);
                    }
                }

                else if (_config.Item("harassMode").GetValue<StringList>().SelectedIndex == 1)
                {
                    if (_config.Item("harassE").GetValue<bool>())
                    {
                        CastE(target);
                    }

                    if (_config.Item("harassQ").GetValue<bool>())
                    {
                        CastQ(target);
                    }
                }
            }
        }

        private static bool IsMyHealthLow(string mode)
        {
            switch (mode)
            {
                case "Harass":
                    return _player.Health < (_player.MaxHealth * (_config.Item("harassHealth").GetValue<Slider>().Value / 100f));
                case "LaneClear":
                    return _player.Health < (_player.MaxHealth * (_config.Item("laneHealth").GetValue<Slider>().Value / 100f));
            }
            return false;
        }

        private static void LaneClear()
        {
            if (!IsMyHealthLow("LaneClear"))
            {
                if (W.IsReady() && W.Instance.SData.Name == "AatroxW")
                    W.Cast();

                if (_config.Item("laneQ").GetValue<bool>())
                {
                    var farmLocation =
                        MinionManager.GetBestCircularFarmLocation(
                            MinionManager.GetMinions(_player.Position, Q.Range)
                                .Select(minion => minion.ServerPosition.To2D())
                                .ToList(), Q.Width, Q.Range);

                    if (farmLocation.MinionsHit >= 3 && _player.Distance(farmLocation.Position) <= Q.Range)
                    {
                        Q.Cast(farmLocation.Position);
                    }
                }

                if (_config.Item("laneE").GetValue<bool>())
                {
                    var farmLocation = MinionManager.GetBestCircularFarmLocation(
                        MinionManager.GetMinions(_player.Position, E.Range, MinionTypes.All, MinionTeam.NotAlly)
                            .Select(minion => minion.ServerPosition.To2D())
                            .ToList(), E.Width, E.Range);

                    if (farmLocation.MinionsHit >= 3 && _player.Distance(farmLocation.Position) <= E.Range)
                    {
                        E.Cast(farmLocation.Position);
                    }
                }
            }
            else
            {
                if (W.Instance.SData.Name == "aatroxw2")
                {
                    W.Cast();
                }
            }
        }

        private static void Config()
        {
            _config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("TargetSelector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            _orbwalker = new Orbwalking.Orbwalker(_config.AddSubMenu(new Menu("Orbwalker", "Orbwalker")));

            // Ignite
            Ignite = _player.Spellbook.GetSpell(_player.GetSpellSlot("summonerdot"));

            // Combo
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("useQ", "Use Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useW", "Use W")).SetValue(true);
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("useWHealth", "Use W Health"))
                .SetValue(new Slider(35, 1, 100));
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("useE", "Use E"))
                .SetValue(true);
            _config.SubMenu("Combo")
                .AddItem(new MenuItem("useR", "Use " + R.Instance.SData.Name + " (R)"))
                .SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useItems", "Use Items")).SetValue(true);

            // Harass
            _config.AddSubMenu(new Menu("Harass", "Harass"));
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("harassMode", "Harass Mode"))
                .SetValue(new StringList(new[] {"E", "E + Q"}, 1));
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("harassQ", "Use " + Q.Instance.SData.Name + " (Q)"))
                .SetValue(true);
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("harassE", "Use " + E.Instance.SData.Name + "(E)"))
                .SetValue(true);
            _config.SubMenu("Harass")
                .AddItem(new MenuItem("harassHealth", "Min. Health Percent"))
                .SetValue(new Slider(50, 1, 100));

            // Items
            _config.AddSubMenu(new Menu("Items", "Items"));
            _config.SubMenu("Items").AddItem(new MenuItem("useItems", "Use Items")).SetValue(true);
            _config.SubMenu("Items").AddItem(new MenuItem("BOTRK", "BOTRK")).SetValue(true);
            _config.SubMenu("Items").AddItem(new MenuItem("OMEN", "Randuins Omen")).SetValue(true);
            _config.SubMenu("Items")
                .AddItem(new MenuItem("omenCount", "Min champs hit for Omen"))
                .SetValue(new Slider(1, 1, 5));
            _config.SubMenu("Items").AddItem(new MenuItem("HYDRA", "Ravenous Hydra")).SetValue(true);
            _config.SubMenu("Items").AddItem(new MenuItem("GBLADE", "Youmuu's Ghostblade")).SetValue(true);

            // Lane Clear / Jungle Clear
            _config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            _config.SubMenu("LaneClear").AddItem(new MenuItem("laneQ", "Use Q")).SetValue(true);
            _config.SubMenu("LaneClear").AddItem(new MenuItem("laneE", "Use E")).SetValue(true);
            _config.SubMenu("LaneClear")
                .AddItem(new MenuItem("laneHealth", "Min Health Percent"))
                .SetValue(new Slider(50, 1, 100));

            // Killsteal
            _config.AddSubMenu(new Menu("Killsteal", "Killsteal"));
            _config.SubMenu("Killsteal").AddItem(new MenuItem("killSteal", "Use Smart Kill Steal")).SetValue(true);
            _config.SubMenu("Killsteal").AddItem(new MenuItem("autoIgnite", "Auto Ignite")).SetValue(true);

            // Misc
            _config.AddSubMenu(new Menu("Packets", "Packets"));
            _config.SubMenu("Packets").AddItem(new MenuItem("usePackets", "Use Packets")).SetValue(true);

            // Drawing
            _config.AddSubMenu(new Menu("Drawing", "Drawing"));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("drawQ", "Draw Q Range"))
                .SetValue(new Circle(true, Color.FromArgb(255, 255, 0, 0)));
            _config.SubMenu("Drawing")
                .AddItem(new MenuItem("drawE", "Draw E Range"))
                .SetValue(new Circle(true, Color.FromArgb(255, 255, 0, 0)));
            _config.SubMenu("Drawing").AddItem(new MenuItem("DamageIndicator", "Damage Indicator").SetValue(true));
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

            if (E.IsReady())
            {
                damage += _player.GetSpellDamage(hero, SpellSlot.E);
            }

            if (W.IsReady() && W.Instance.SData.Name == "aatroxw2")
            {
                damage += _player.GetAutoAttackDamage(hero, true) * 3;
            }

            return (float) damage;
        }

        private static void Program_ValueChanged(object sender, OnValueChangeEventArgs e)
        {
            Utility.HpBarDamageIndicator.Enabled = e.GetNewValue<bool>();
        }

        private static void CreateSpells()
        {
            Q = new Spell(SpellSlot.Q, 650f);
            W = new Spell(SpellSlot.W, _player.AttackRange);
            E = new Spell(SpellSlot.E, 975f);
            R = new Spell(SpellSlot.R, 300f);

            Q.SetSkillshot(Q.Instance.SData.SpellCastTime, 280f, Q.Instance.SData.MissileSpeed,
                false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(E.Instance.SData.SpellCastTime, E.Instance.SData.LineWidth, E.Instance.SData.MissileSpeed,
                false, SkillshotType.SkillshotLine);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }
    }
}
