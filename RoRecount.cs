using BepInEx;
using RiskOfRecount.Tracking;
using RiskOfRecount.Tracking.Stats;
using RiskOfRecount.Ui;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;

namespace RiskOfRecount {
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.ravioligravioli.RoRecount", "Risk of Recount", "1.2.1")]
    public class RoRecount : BaseUnityPlugin {
        public static List<TrackingDef> TrackingDefs = new List<TrackingDef>();

        public static RecountContainer ui;

        public void OnEnable() {
            TrackingDefs.Add(new TrackingDef("Total Damage", typeof(TrackingStatTotalDamage)));
            TrackingDefs.Add(new TrackingDef("Total Healing", typeof(TrackingStatTotalHealing)));
            TrackingDefs.Add(new TrackingDef("Current DPS", typeof(TrackingStatCurrentDPS), true));
            TrackingDefs.Add(new TrackingDef("Peak DPS", typeof(TrackingStatPeakDPS)));

            On.RoR2.HealthComponent.Heal += (orig, self, amount, procChainMask, nonRegen) => {
                if (self.body && self.body.master && self.body.master.playerCharacterMasterController) {
                    PlayerCharacterMasterController player = self.body.master.playerCharacterMasterController;
                    if (player) {
                        if (Math.Round(self.health + amount) <= self.body.maxHealth) {
                            ui.LogEvent(player, "heal", amount);
                        }
                    }
                }

                return orig(self, amount, procChainMask, nonRegen);
            };
            On.RoR2.GlobalEventManager.OnHitEnemy += (orig, self, info, victim) => {
                orig(self, info, victim);

                if (info.attacker) {
                    CharacterBody body = info.attacker.GetComponent<CharacterBody>();
                    if (body && body.master) {
                        PlayerCharacterMasterController player = null;
                        if (body && body.master) {
                            if (body.master.playerCharacterMasterController) {
                                player = body.master.playerCharacterMasterController;
                            }
                            if (body.master.minionOwnership && body.master.minionOwnership.ownerMaster && body.master.minionOwnership.ownerMaster.playerCharacterMasterController) {
                                player = body.master.minionOwnership.ownerMaster.playerCharacterMasterController;
                            }
                        }
                        if (player) {
                            ui.LogEvent(player, "damage", info.damage);
                        }
                    }
                }
            };
            On.RoR2.GlobalEventManager.OnCharacterBodyStart += (orig, self, user) => {
                orig(self, user);

                if (user && user.master && user.master.playerCharacterMasterController) {
                    StartCoroutine(OnPlayerAdded(user.master.playerCharacterMasterController));
                }
            };
            On.RoR2.Run.OnUserRemoved += (orig, self, user) => {
                orig(self, user);

                if (user && user.masterController) {
                    ui.RemoveBar(user.masterController);
                }
            };
        }

        public IEnumerator OnPlayerAdded(PlayerCharacterMasterController player) {
            while (ui == null) {
                yield return null;
            }
            ui.AddBar(player);
        }

        public void Update() {
            LocalUser localUser = LocalUserManager.GetFirstLocalUser();
            if (localUser != null && localUser.cachedBody != null && ui == null) {
                ui = localUser.cachedBody.gameObject.AddComponent<RecountContainer>();
            }
        }
    }
}