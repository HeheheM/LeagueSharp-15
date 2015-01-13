using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace Nasus
{
    class Program
    {
        public const string ChampionName = "Nasus";

        private static Obj_AI_Hero _player;

        private static List<Spell> SpellList = new List<Spell>();
        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;

        private static Menu _config;

        private static Orbwalking.Orbwalker _orbwalker;


        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            _player = ObjectManager.Player;
            if (_player.BaseSkinName != ChampionName) return;

            Config();
            CreateSpells();
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Game_OnDraw;
            //Obj_AI_Hero.OnProcessSpellCast += Obj_AI_Hero_OnProcessSpellCast;
            Orbwalking.AfterAttack += Orbwalking_AfterAttack;
            

            Game.PrintChat("Nasus by Aureus Loaded!");
        }

        static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (!unit.IsMe) return;
            if (target.IsDead) return;

            if (_config.Item("useQ").GetValue<bool>() && _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo && Q.IsReady())
            {
                Q.Cast();
                _player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }

            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit ||
                _orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
        }

        private static void Game_OnDraw(EventArgs args)
        {
            var drawing = _config.Item("drawW").GetValue<Circle>().Color;

            if (_config.Item("drawW").GetValue<Circle>().Active)
            {
                Render.Circle.DrawCircle(_player.Position, W.Range, drawing);
            }

            if (_config.Item("drawE").GetValue<Circle>().Active)
            {
                Render.Circle.DrawCircle(_player.Position, E.Range, drawing);
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (_player.IsDead) return;
            _orbwalker.SetAttack(true);

            // Combo
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }

            // LastHit
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                LastHit();
            }

            // LaneClear
            if (_orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
            }

        }

        private static void LaneClear()
        {
            if (_config.Item("clearE").GetValue<bool>())
            {
                var farmlocation =
                    MinionManager.GetBestCircularFarmLocation(
                        MinionManager.GetMinions(_player.Position, E.Range).Where(x => x.IsEnemy && !x.IsDead)
                            .Select(minion => minion.ServerPosition.To2D())
                            .ToList(), E.Width, E.Range);

                if (_config.Item("clearMinE").GetValue<Slider>().Value >= farmlocation.MinionsHit && _player.Distance(farmlocation.Position) <= E.Range)
                {
                    E.Cast(farmlocation.Position);
                }
            }

            LastHit();
        }

        private static void LastHit()
        {
            var minion = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.NotAlly);
            foreach (var qtarget in minion.Where(x => !x.IsDead && !x.IsInvulnerable && _player.Distance(x) <= Q.Range))
            {
                if (qtarget.Health < (Q.GetDamage(qtarget) + _player.BaseAttackDamage + _player.FlatPhysicalDamageMod) && (Q.IsReady() || _player.HasBuff("SiphoningStrike")))
                {
                    Q.Cast();
                    _player.IssueOrder(GameObjectOrder.AttackUnit, qtarget);
                    _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }
//                Game.PrintChat((Q.GetDamage(qtarget) + _player.BaseAttackDamage + _player.FlatPhysicalDamageMod).ToString());
            }
        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(_player, W.Range, TargetSelector.DamageType.Physical);

            if (target == null) return;

            if (_config.Item("useW").GetValue<bool>())
            {
                if (_player.Distance(target) <= W.Range)
                {
                    W.Cast(target, _config.Item("usePackets").GetValue<bool>());
                }
            }

            if (_config.Item("useE").GetValue<bool>())
            {
                if (_player.Distance(target) <= E.Range)
                {
                    E.CastIfHitchanceEquals(target,
                        HitChance.Medium, 
                        _config.Item("usePackets").GetValue<bool>());
                }
            }

            if (_config.Item("useR").GetValue<bool>())
            {
                var count = 0;

                foreach (
                    var enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(x => x.IsEnemy && !x.IsDead && _player.Distance(x) < R.Range))
                {
                    count++;
                }

                if (count >= _config.Item("minUlt").GetValue<Slider>().Value)
                {
                    R.Cast();
                }
            }
        }

        private static void CreateSpells()
        {
            Q = new Spell(SpellSlot.Q, 225f);
            W = new Spell(SpellSlot.W, 600f);
            E = new Spell(SpellSlot.E, 650f);
            R = new Spell(SpellSlot.R, 225f);

            E.SetSkillshot(E.Instance.SData.SpellCastTime, 400f, E.Instance.SData.MissileSpeed,
                false, SkillshotType.SkillshotCircle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }

        private static void Config()
        {
            _config = new Menu(ChampionName, ChampionName, true);

            var targetSelectorMenu = new Menu("TargeSelector", "TargetSelector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            _config.AddSubMenu(targetSelectorMenu);

            _orbwalker = new Orbwalking.Orbwalker(_config.AddSubMenu(new Menu("Orbwalker", "Orbwalker")));

            // Combo
            _config.AddSubMenu(new Menu("Combo", "Combo"));
            _config.SubMenu("Combo").AddItem(new MenuItem("useQ", "Use Q")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useW", "Use W")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useE", "Use E")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("useR", "Use Ult")).SetValue(true);
            _config.SubMenu("Combo").AddItem(new MenuItem("minUlt", "Min Enemies to Ult")).SetValue(new Slider(3, 1, 5));

            // LastHit
            _config.AddSubMenu(new Menu("LastHit", "LastHit"));
            _config.SubMenu("LastHit").AddItem(new MenuItem("stackQ", "Stack Q on LastHit")).SetValue(true);

            // LaneClear
            _config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            _config.SubMenu("LaneClear").AddItem(new MenuItem("clearE", "Lane Clear with E")).SetValue(true);
            _config.SubMenu("LaneClear").AddItem(new MenuItem("clearMinE", "Min minions to hit")).SetValue(new Slider(3, 1, 7));

            // Packets
            _config.AddSubMenu(new Menu("Packets", "Packets"));
            _config.SubMenu("Packets").AddItem(new MenuItem("usePackets", "Use Packets")).SetValue(true);

            // Drawing
            _config.AddSubMenu(new Menu("Drawing", "Drawing"));
            _config.SubMenu("Drawing").AddItem(new MenuItem("drawW", "Draw W range"))
                .SetValue(new Circle(true, Color.FromArgb(255, 255, 0, 0)));
            _config.SubMenu("Drawing").AddItem(new MenuItem("drawE", "Draw E Range"))
                .SetValue(new Circle(true, Color.FromArgb(255, 255, 0, 0)));

            _config.AddToMainMenu();
        }
    }
}
