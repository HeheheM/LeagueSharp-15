﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace Sehuewani
{
    static class Program
    {
        private const string ChampionName = "Sejuani";

        private static List<Spell> SpellList = new List<Spell>();
        private static Spell _q;
        private static Spell _w;
        private static Spell _e;
        private static Spell _r;

        private static Menu _config;

        private static Obj_AI_Hero _player;

        private static Orbwalking.Orbwalker _orbwalker;

        static void Main(string[] args)
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
            Game.PrintChat("Sehuewani by Aureus Loaded!");

        }

        private static void CreateSpells()
        {
            _q = new Spell(SpellSlot.Q, 650f);
            _w = new Spell(SpellSlot.W, 350f);
            _e = new Spell(SpellSlot.E, 1000f);
            _r = new Spell(SpellSlot.R, 1175f);

            _q.SetSkillshot(0f, 70f, 1600f, false, SkillshotType.SkillshotLine);
            _r.SetSkillshot(0.25f, 110f, 1600f, false, SkillshotType.SkillshotLine);

            SpellList.Add(_q);
            SpellList.Add(_w);
            SpellList.Add(_e);
            SpellList.Add(_r);
        }

        private static void Config()
        {
            _config = new Menu("Aureus' Sejuani", "Sejuani", true);

            var targetSelectorMenu = new Menu("TargetSelector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            _config.AddSubMenu(new Menu("Orbwalker", "Orbwalker"));
            _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalker"));

            // Combo
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("useQ", "Use Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useW", "Use W")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useE", "Use E")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useR", "Use Ult")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("minHit", "Minimum Hit")).SetValue(new Slider(2, 1, 5));

            // Packets
            _config.AddSubMenu(new Menu("Packets", "Packets"));
            _config.SubMenu("Packets").AddItem(new MenuItem("usePackets", "Use Packets")).SetValue(true);

            _config.AddToMainMenu();
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead) return;

            _orbwalker.SetAttack(true);

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                var target = TargetSelector.GetTarget(_r.Range, TargetSelector.DamageType.Magical);
                if (target == null) return;
                if (_config.Item("useQ").GetValue<bool>() && target.Distance(_player) <= _q.Range && _q.IsReady())
                {
                    _q.Cast(target, _config.Item("usePackets").GetValue<bool>());
                }

                if (_config.Item("useW").GetValue<bool>() && target.Distance(_player) <= _w.Range && _w.IsReady())
                {
                    _w.Cast();
                }

                if (_config.Item("useE").GetValue<bool>() && target.Distance(_player) <= _e.Range && _e.IsReady())
                {
                    CastE();
                }

                if (_config.Item("useR").GetValue<bool>() && target.Distance(_player) <= _r.Range && _r.IsReady())
                {
                    AutoR();
                }
            }
        }

        private static void AutoR()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && !enemy.IsDead && CountChampsAtArea(enemy, 350f) >= _config.Item("minHit").GetValue<Slider>().Value))
            {
                _r.Cast(enemy);
            }
        }

        private static int CountChampsAtArea(Obj_AI_Hero unit, float range)
        {
            return ObjectManager.Get<Obj_AI_Hero>().Count(enemy => enemy.IsEnemy && !enemy.IsDead && enemy.Distance(unit) < range);
        }

        private static void CastE()
        {
            foreach (var enemy in ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.IsEnemy && !enemy.IsDead))
            {
                foreach (BuffInstance buff in enemy.Buffs)
                {
                    if (buff.Name == "SejuaniFrost")
                    {
                        _e.Cast();
                    }
                }
            }
        }
    }
}
