using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Xml;
using static DualWield.Main;

namespace DualWield
{
    public static class Utils
    {
        private static string muzzleFx;
        public static bool IsSuppressorActive = false;
        public static Vector3 RecoilOffset = Vector3.Zero;

        //Animations
        public static readonly CrClipAsset Normal = new CrClipAsset("wfire_mbahdokek_dualwield@small_gun_core", "wfire_mbahdokek_dualwield_small_gun_sweep");
        public static readonly CrClipAsset NormalIdle = new CrClipAsset("wfire_mbahdokek_dualwield@small_gun_core", "wfire_mbahdokek_dualwield_small_gun_idle");
        public static readonly CrClipAsset NormalWalk = new CrClipAsset("wfire_mbahdokek_dualwield@small_gun_core", "wfire_mbahdokek_dualwield_small_gun_walk");
        public static readonly CrClipAsset NormalRun = new CrClipAsset("wfire_mbahdokek_dualwield@small_gun_core", "wfire_mbahdokek_dualwield_small_gun_run");

        public static readonly CrClipAsset Gang = new CrClipAsset("wfire_mbahdokek_dualwield@gangsta_gun_core", "wfire_mbahdokek_dualwield_gangsta_gun_sweep");
        public static readonly CrClipAsset GangIdle = new CrClipAsset("wfire_mbahdokek_dualwield@gangsta_gun_core", "wfire_mbahdokek_dualwield_gangsta_gun_idle");
        public static readonly CrClipAsset GangWalk = new CrClipAsset("wfire_mbahdokek_dualwield@gangsta_gun_core", "wfire_mbahdokek_dualwield_gangsta_gun_walk");
        public static readonly CrClipAsset GangRun = new CrClipAsset("wfire_mbahdokek_dualwield@gangsta_gun_core", "wfire_mbahdokek_dualwield_gangsta_gun_run");

        public static readonly CrClipAsset MG = new CrClipAsset("wfire_mbahdokek_dualwield@mg_core_rm", "wfire_mbahdokek_dualwield_mg_sweep");
        public static readonly CrClipAsset MG_Idle = new CrClipAsset("wfire_mbahdokek_dualwield@mg_core_rm", "wfire_mbahdokek_dualwield_mg_idle");
        public static readonly CrClipAsset MG_Walk = new CrClipAsset("wfire_mbahdokek_dualwield@mg_core_rm", "wfire_mbahdokek_dualwield_mg_walk");
        public static readonly CrClipAsset MG_Run = new CrClipAsset("wfire_mbahdokek_dualwield@mg_core_rm", "wfire_mbahdokek_dualwield_mg_run");

        public static readonly CrClipAsset MiniG = new CrClipAsset("wfire_mbahdokek_dualwield@minigun_core_rm", "wfire_mbahdokek_dualwield_minigun_sweep");
        public static readonly CrClipAsset MiniGunIdle = new CrClipAsset("wfire_mbahdokek_dualwield@minigun_core_rm", "wfire_mbahdokek_dualwield_minigun_idle");
        public static readonly CrClipAsset MiniGunWalk = new CrClipAsset("wfire_mbahdokek_dualwield@minigun_core_rm", "wfire_mbahdokek_dualwield_minigun_walk");
        public static readonly CrClipAsset MiniGunRun = new CrClipAsset("wfire_mbahdokek_dualwield@minigun_core_rm", "wfire_mbahdokek_dualwield_minigun_run");

        public static readonly CrClipAsset RPG = new CrClipAsset("wfire_mbahdokek_dualwield@rpg_core_rm", "wfire_mbahdokek_dualwield_rpg_sweep");
        public static readonly CrClipAsset RPG_Idle = new CrClipAsset("wfire_mbahdokek_dualwield@rpg_core_rm", "wfire_mbahdokek_dualwield_rpg_idle");
        public static readonly CrClipAsset RPG_Walk = new CrClipAsset("wfire_mbahdokek_dualwield@rpg_core_rm", "wfire_mbahdokek_dualwield_rpg_walk");
        public static readonly CrClipAsset RPG_Run = new CrClipAsset("wfire_mbahdokek_dualwield@rpg_core_rm", "wfire_mbahdokek_dualwield_rpg_run");

        public static readonly CrClipAsset LongGun = new CrClipAsset("wfire_mbahdokek_dualwield@long_gun_core_rm", "wfire_mbahdokek_dualwield_long_gun_sweep");
        public static readonly CrClipAsset LongGunIdle = new CrClipAsset("wfire_mbahdokek_dualwield@long_gun_core_rm", "wfire_mbahdokek_dualwield_long_gun_idle");
        public static readonly CrClipAsset LongGunWalk = new CrClipAsset("wfire_mbahdokek_dualwield@long_gun_core_rm", "wfire_mbahdokek_dualwield_long_gun_walk");
        public static readonly CrClipAsset LongGunRun = new CrClipAsset("wfire_mbahdokek_dualwield@long_gun_core_rm", "wfire_mbahdokek_dualwield_long_gun_run");

        public static CrClipAsset AimAnim;
        public static CrClipAsset IdleAnim;
        public static CrClipAsset WalkingAnim;
        public static CrClipAsset RunningAnim;
        public static bool BulletTypeWpn;
        public static float RecoilVal;

        public static WeaponComponentHash[] WeaponComponentsHashCache;

        private static readonly Random Rand = new Random();
        
        public static bool AnimsLoaded()
        {
            bool loaded = Request(Normal.ClipDictionary, 800) && Request(Gang.ClipDictionary, 800) && Request(MG.ClipDictionary, 800) && Request(RPG.ClipDictionary, 800) && Request(LongGun.ClipDictionary, 800) && Request(MiniG.ClipDictionary, 800);
            if (!loaded)
            {
                Notification.PostTicker("~b~Dual ~y~Wield Custom Animations ~r~not found! ~n~~w~Please recheck animation files installation in: ~n~x64c.rpf/anim/ingame/clip_melee@.rpf", true);
                Logger.Log($"Dual Wield has failed to start due to animation files not found/loaded");
            }
            return loaded;
        }
        public static void ReleaseAnims()
        {
            bool loaded = Normal.ClipDictionary.IsLoaded || Gang.ClipDictionary.IsLoaded || MG.ClipDictionary.IsLoaded || RPG.ClipDictionary.IsLoaded || LongGun.ClipDictionary.IsLoaded || MiniG.ClipDictionary.IsLoaded;
            if (loaded)
            {
                Normal.ClipDictionary.MarkAsNoLongerNeeded();
                Gang.ClipDictionary.MarkAsNoLongerNeeded();
                MG.ClipDictionary.MarkAsNoLongerNeeded();
                RPG.ClipDictionary.MarkAsNoLongerNeeded();
                LongGun.ClipDictionary.MarkAsNoLongerNeeded();
                MiniG.ClipDictionary.MarkAsNoLongerNeeded();
            }
        }

        public static void SortPtfx()
        {
            BulletTypeWpn = WeaponDamageType(MC_Wpn) == 3 || WeaponDamageType(MC_Wpn) == 4;
            if (!IsSuppressorActive)
            {
                if (IsMinigunType(MC_Wpn))
                {
                    muzzleFx = "muz_minigun";
                    if (MC_Wpn == WeaponHash.Widowmaker || MC_Wpn == WeaponHash.UnholyHellbringer)
                        muzzleFx = "bullet_tracer_xs_sr";
                }
                else if (MC_Wpn.Group == WeaponGroup.MG || MC_Wpn.Group == WeaponGroup.AssaultRifle)
                    muzzleFx = "muz_assault_rifle";
                else if (MC_Wpn.Group == WeaponGroup.Pistol)
                    muzzleFx = "muz_pistol";
                else if (MC_Wpn.Group == WeaponGroup.SMG)
                    muzzleFx = "muz_smg";
                else if (MC_Wpn.Group == WeaponGroup.Shotgun)
                    muzzleFx = "muz_shotgun";
                else
                    muzzleFx = "muz_minigun_alt";
            }
            else muzzleFx = "muz_pistol_silencer";
        }

        public static void ShootAt(Ped ped, Entity gun)
        {
            Vector3 crosshair = GetCrosshairTarget(Config.spread, ped);

            if (MC_Wpn == WeaponHash.Widowmaker || MC_Wpn == WeaponHash.UnholyHellbringer)
                crosshair = GetCrosshairTarget(0f, ped);

            // For Mk II guns special ammo
            if (!(WeaponDamageType(MC_Wpn) != 5 && MC_Wpn.Group != WeaponGroup.Sniper && MC_Wpn != WeaponHash.Widowmaker && (Function.Call<int>(Hash.GET_PED_ORIGINAL_AMMO_TYPE_FROM_WEAPON, MC, MC_Wpn.Hash) != Function.Call<int>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON, MC, MC_Wpn.Hash))))
                Function.Call(Hash.SET_PED_SHOOTS_AT_COORD, ped, crosshair.X, crosshair.Y, crosshair.Z, true);

            if (BulletTypeWpn)
            {
                ParticleEffectAsset core = new ParticleEffectAsset("core");
                Vector3 gunRot = Vector3.Zero;

                if (MC_Wpn == WeaponHash.Widowmaker || MC_Wpn == WeaponHash.UnholyHellbringer)
                {
                    core = new ParticleEffectAsset("weap_xs_weapons");
                    if (MC_Wpn == WeaponHash.UnholyHellbringer)
                    {
                        gunRot = new Vector3(0f, 80f, 0f);
                        if (gun == GunL)
                            gunRot = new Vector3(0f, 75f, -5f);
                    }
                    if (MC_Wpn == WeaponHash.Widowmaker)
                    {
                        gunRot = new Vector3(-80f, 190f, 96f);
                        if (gun == GunL)
                            gunRot = new Vector3(90f, 190f, 270f);
                    }
                }

                core.Request(400);
                EntityBone bone = gun.Bones[Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, gun, "gun_muzzle")];
                World.CreateParticleEffectNonLooped(core, muzzleFx, bone, Vector3.Zero, gunRot);
            }
        }

        public static Vector3 GetCrosshairTarget(float spreadAngleDegrees, Ped ped)
        {
            Vector3 forward = GameplayCamera.ForwardVector;

            //AI-made to simulate recoil, because Ped.Accuracy is not working in this case
            float angleRad = spreadAngleDegrees * (float)Math.PI / 180f;

            float randYaw = NextFloat(-angleRad, angleRad);
            float randPitch = NextFloat(-angleRad, angleRad);

            Vector3 right = GameplayCamera.RightVector;
            Vector3 up = GameplayCamera.UpVector;

            Vector3 spreadDir = forward + right * (float)Math.Tan(randYaw) + up * (float)Math.Tan(randPitch);
            spreadDir.Normalize();

            Vector3 target = GameplayCamera.GetOffsetPosition(new Vector3(0f, 1000f, 0f)) + (spreadDir * 1000f);
            float adjLeftRight;
            if (ped == ShooterL)
                adjLeftRight = 0.25f;
            else adjLeftRight = -0.15f;

            RaycastResult ray = World.Raycast(GameplayCamera.Position + new Vector3(0f, adjLeftRight, 0f), target, IntersectFlags.Everything, MC);
            if (ray.DidHit && ray.HitPosition.DistanceTo(MC.Position) > 3f)
                return ray.HitPosition;

            else return GameplayCamera.Position + (GameplayCamera.ForwardVector * 100f);
        }

        public static float NextFloat(float min, float max)
        {
            return (float)(Rand.NextDouble() * (max - min) + min);
        }

        public static void SetInverseKinematics(bool state)
        {
            Function.Call(Hash.SET_PED_CAN_ARM_IK, MC, state);
        }

        public static void ShowPlayerWpn(bool shown)
        {
            if (!AllowedGuns.Contains(MC_Wpn.Group) || MC.IsDead || Game.Player.IsDead)
                return;
            Function.Call(Hash.SET_PED_CURRENT_WEAPON_VISIBLE, MC, shown, false, false, false);
            MC.Weapons.CurrentWeaponObject.IsVisible = shown;
        }

        public static void SortAnim(Weapon reference)
        {
            const float SMG_LONG_WEAPON_THRESHOLD = 0.44f;
            
            if (reference.Group == WeaponGroup.AssaultRifle || reference.Group == WeaponGroup.Shotgun || reference.Group == WeaponGroup.Sniper)
            {
                float weaponLength = reference.Model.Dimensions.frontTopRight.X - reference.Model.Dimensions.rearBottomLeft.X;
                bool longWpn = weaponLength > 0.78f;
                if (longWpn)
                {
                    AimAnim = LongGun;
                    IdleAnim = LongGunIdle;
                    WalkingAnim = LongGunWalk;
                    RunningAnim = LongGunRun;
                    RecoilVal = 17.5f;
                }
                else
                {
                    AimAnim = Normal;
                    IdleAnim = NormalIdle;
                    WalkingAnim = NormalWalk;
                    RunningAnim = NormalRun;
                    RecoilVal = 20f;
                }

            }
            else if (reference.Group == WeaponGroup.SMG)
            {
                float weaponLength = reference.Model.Dimensions.frontTopRight.X - reference.Model.Dimensions.rearBottomLeft.X;
                bool isLongWeapon = weaponLength > SMG_LONG_WEAPON_THRESHOLD;
                if (isLongWeapon)
                {
                    AimAnim = Normal;
                    IdleAnim = NormalIdle;
                    WalkingAnim = NormalWalk;
                    RunningAnim = NormalRun;
                    RecoilVal = 10f;
                }
                else
                {
                    AimAnim = Gang;
                    IdleAnim = GangIdle;
                    WalkingAnim = GangWalk;
                    RunningAnim = GangRun;
                    RecoilVal = 15f;
                }
            }
            else if (reference.Group == WeaponGroup.MG)
            {
                AimAnim = MG;
                IdleAnim = MG_Idle;
                WalkingAnim = MG_Walk;
                RunningAnim = MG_Run;
                RecoilVal = 25f;
            }
            else if (reference.Group == WeaponGroup.Heavy)
            {
                if (reference.Hash == WeaponHash.RPG || reference.Hash == WeaponHash.Firework || reference.Hash == WeaponHash.HomingLauncher)
                {
                    AimAnim = RPG;
                    IdleAnim = RPG_Idle;
                    WalkingAnim = RPG_Walk;
                    RunningAnim = RPG_Run;
                    RecoilVal = 30f;
                }
                else if (IsMinigunType(reference))
                {
                    AimAnim = MiniG;
                    IdleAnim = MiniGunIdle;
                    WalkingAnim = MiniGunWalk;
                    RunningAnim = MiniGunRun;
                    RecoilVal = 0f; // disable to prevent glitchy anim
                }
                else if (reference.Hash == WeaponHash.SnowballLauncher || reference.Hash == WeaponHash.CompactGrenadeLauncher || reference.Hash == WeaponHash.GrenadeLauncher || reference.Hash == WeaponHash.GrenadeLauncherSmoke)
                {
                    AimAnim = Normal;
                    IdleAnim = NormalIdle;
                    WalkingAnim = NormalWalk;
                    RunningAnim = NormalRun;
                    RecoilVal = 10f;
                }
                else
                {
                    AimAnim = LongGun;
                    IdleAnim = LongGunIdle;
                    WalkingAnim = LongGunWalk;
                    RunningAnim = LongGunRun;
                    RecoilVal = 20f;
                }
            }
            else if (reference.Group == WeaponGroup.Pistol)
            {
                if (Config.pistolAnim == 1)
                    AimAnim = Normal;
                else if (Config.pistolAnim == 2)
                    AimAnim = Gang;
                else
                    AimAnim = Rand.Next(2) == 0 ? Normal : Gang;

                if (AimAnim == Normal)
                {
                    IdleAnim = NormalIdle;
                    WalkingAnim = NormalWalk;
                    RunningAnim = NormalRun;
                }
                else
                {
                    IdleAnim = GangIdle;
                    WalkingAnim = GangWalk;
                    RunningAnim = GangRun;
                }
                RecoilVal = 10f;
            }
            else
            {
                AimAnim = new CrClipAsset(string.Empty, string.Empty);
                Logger.Log($"SortAnim has failed to match animation sets to the current weapon");
            }
        }

        public static void GetAttachments(Ped target)
        {
            Entity playerWpnObj = MC.Weapons.CurrentWeaponObject;
            Entity targetWpnObj = target.Weapons.CurrentWeaponObject;
            WeaponHash targetWpn = target.Weapons.Current;
            WeaponHash playerWpn = target.Weapons.Current.Hash;
            foreach (WeaponComponentHash component in WeaponComponentsHashCache) //Thanks to SHVDN NativeMemory
            {
                if (Function.Call<bool>(Hash.HAS_WEAPON_GOT_WEAPON_COMPONENT, playerWpnObj, component))
                {
                    Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, target, targetWpn, component);
                    int tintCompId = Function.Call<int>(Hash.GET_WEAPON_OBJECT_COMPONENT_TINT_INDEX, playerWpnObj, component);
                    Function.Call(Hash.SET_WEAPON_OBJECT_COMPONENT_TINT_INDEX, targetWpnObj, component, tintCompId);
                }
            }

            int tintId = Function.Call<int>(Hash.GET_PED_WEAPON_TINT_INDEX, MC, playerWpn);
            Function.Call(Hash.SET_PED_WEAPON_TINT_INDEX, target, targetWpn, tintId);
            int camoId = Function.Call<int>(Hash.GET_PED_WEAPON_CAMO_INDEX, MC, playerWpnObj);
            Function.Call(Hash.SET_WEAPON_OBJECT_CAMO_INDEX, target, targetWpnObj, camoId);
        }

        public static void ConflictGetter()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                DodgeType = assembly.GetType("Shootdodge.Main");
                if (DodgeType != null)
                {
                    DodgeField = DodgeType.GetField("ScriptStatus", BindingFlags.Static | BindingFlags.Public);
                    Utils.Logger.Log($"Shootdodge installation found!");
                    break;
                }
            }
            Conflict = false;
            if (File.Exists("scripts\\More Gore Settings.xml"))
            {
                XmlDocument moreGore = new XmlDocument();
                moreGore.Load("scripts\\More Gore Settings.xml");
                XmlNode mgHeal = moreGore.SelectSingleNode("/MoreGore/Options/Healing");
                XmlAttribute healingAttrib = mgHeal?.Attributes?["PlayHealingAnimation"];
                if (healingAttrib != null)
                {
                    bool.TryParse(healingAttrib.Value, out Conflict);
                }
            }
        }

        public static void CheckConflict()
        {
            if (Conflict && !Notified)
            {
                Notification.PostTickerForced("Dual Wield ~r~WARNING!~w~: More Gore mod installed. ~n~Disable PlayHealingAnimation in it's xml to fix triple gun bug when dual-wielding", true);
                Script.Wait(0);
                Notified = true;
            }

            Shootdodge = DodgeField != null ? (int)DodgeField.GetValue(null) : 0;
        }

        public static bool IsMinigunType(Weapon weapon)
        {
            return weapon == WeaponHash.Minigun || weapon == WeaponHash.Widowmaker || weapon == WeaponHash.UnholyHellbringer;
        }

        public static float MapPitchToPhase(float pitch, float minTime, float maxTime)
        {
            float fromMin = -70f;
            float fromMax = 42f;

            // Clamp first
            pitch = Clamp(pitch, fromMin, fromMax);

            // Normalize to 0–1
            float t = (pitch - fromMin) / (fromMax - fromMin);

            // Remap to custom output range (0.15 to 0.85)
            float toMin = minTime;
            float toMax = maxTime;

            return toMin + (toMax - toMin) * t;
        }
        public static float GetAimAnimSpeedByClipAsset()
        {
            if (AimAnim == Gang)
            {
                return 0.85f;
            }
            return AimAnim == RPG ? 0.90f : 1f;
        }
        public static (string textureDict, string textureName)? WeaponGroupIcon(WeaponGroup group)
        {
            switch (group)
            {
                case WeaponGroup.Pistol:
                    return ("mpweaponsgang1", "w_pi_pistol_silhouette_final");

                case WeaponGroup.SMG:
                    return ("mpweaponscommon", "w_sb_microsmg_silhouette");

                case WeaponGroup.AssaultRifle:
                    return ("mpweaponsgang0", "w_ar_carbinerifle_silhouette");

                case WeaponGroup.Shotgun:
                    return ("mpweaponsgang1", "w_sg_sawnoff_silhouette");

                case WeaponGroup.MG:
                    return ("mpweaponsgang1", "w_mg_mg_silhouette_final");

                case WeaponGroup.Sniper:
                    return ("mpweaponsgang0", "w_sr_sniperrifle_silhouette_final");

                case WeaponGroup.Heavy:
                    return ("mpweaponscommon", "w_r_grenadelauncher_silhouette");

                default:
                    return null;
            }
        }

        public static void DrawIcon(string dict, string name, Color color, float sizeX, float sizeY, float x, float y, float rotation)
        {
            try
            {
                Sprite hud = new Sprite(dict, name, new SizeF(sizeX, sizeY), new PointF(x, y), color, rotation);
                hud.Draw();
            }
            catch (Exception ex)
            {
                Notification.PostTicker($"Error! textures not found: {ex.Message}", true);
                return;
            };
        }

        public static Color GetAmmoColor(int ammoInClip, int maxAmmoInClip)
        {
            try
            {
                const float BRIGHTNESS_CLAMPED = 0.6f * 255;
                if (maxAmmoInClip <= 0)
                    return Color.Transparent;

                float scaledRatio = ammoInClip / (float)maxAmmoInClip * 2f;

                if (IsMinigunType(MC_Wpn))
                    scaledRatio *= 2f;
                
                if (scaledRatio > 2f)
                    scaledRatio = 2f;
                else if (scaledRatio < 0)
                {
                    scaledRatio = 0f;
                }

                int r, g;

                if (scaledRatio >= 0.5f)
                {
                    r = ClampToByte(BRIGHTNESS_CLAMPED * (2f - scaledRatio));
                    g = ClampToByte(BRIGHTNESS_CLAMPED);
                }
                else
                {
                    r = ClampToByte(BRIGHTNESS_CLAMPED);
                    g = ClampToByte(BRIGHTNESS_CLAMPED * scaledRatio);
                }


                return Color.FromArgb(r, g, 0);
            }
            catch
            {
                return Color.Transparent;
            }
        }

        public static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        private static int ClampToByte(float value) // prevent HUD crash when swapping guns
        {
            return Math.Max(0, Math.Min(255, (int)value));
        }

        public static bool IsLanding(Ped ped)
        {
            return Function.Call<bool>(Hash.IS_PED_LANDING, ped);
        }

        public static bool GunReadyToShoot(Ped ped)
        {
            return Function.Call<bool>(Hash.IS_PED_WEAPON_READY_TO_SHOOT, ped);
        }

        public static bool IsSwitchingGun(Ped ped)
        {
            return Function.Call<bool>(Hash.IS_PED_SWITCHING_WEAPON, ped);
        }

        public static bool RequestGunHD(Entity gun)
        {
            return Function.Call<bool>(Hash.REQUEST_WEAPON_HIGH_DETAIL_MODEL, gun);
        }

        public static int WeaponDamageType(Weapon wpn)
        {
            return Function.Call<int>(Hash.GET_WEAPON_DAMAGE_TYPE, wpn.Hash);
        }

        public static void AlterWeaponClipSet(bool alter, ClipSet moveAnim)
        {
            if (AimAnim == RPG)
                return;
            if (alter)
            {
                Request(moveAnim, 800);
                MC.SetWeaponMovementClipSet(moveAnim);
            }
            else
            {
                MC.ResetWeaponMovementClipSet();
                MC.ResetMovementClipSet(AnimationBlendDuration.Slow);
            }
        }

        public static bool Request(dynamic asset, int timeout = 200) // nightly.26
        {
            asset.Request();

            int startTime = Environment.TickCount;
            int maxElapsed = timeout >= 0 ? timeout : int.MaxValue;

            while (!asset.IsLoaded)
            {
                Script.Yield();
                asset.Request();

                if (Environment.TickCount - startTime >= maxElapsed)
                {
                    Logger.Log($"Dual Wield has failed to load asset: {asset}");
                    return false;
                }
            }

            return true;
        }

        public static bool DoesExists(Entity entity)
        {
            return Function.Call<bool>(Hash.DOES_ENTITY_EXIST, entity);
        }

        public static bool IsPedEnteringVehicle(Ped ped) // SHVDN 3.6.0 nightly compatibility
        {
            Type pedType = typeof(Ped);

            PropertyInfo isEnteringVeh = pedType.GetProperty("IsEnteringVehicle"); //v3.7.0
            if (isEnteringVeh != null)
            {
                return (bool)isEnteringVeh.GetValue(ped);
            }

            PropertyInfo isGettingIntoVeh = pedType.GetProperty("IsGettingIntoVehicle"); //fallback
            if (isGettingIntoVeh != null)
            {
                return (bool)isGettingIntoVeh.GetValue(ped);
            }

            return false;
        }

        public static class Logger
        {
            private static readonly string LogFilePath = Path.Combine(Config.GetPath(), "DualWield_log.txt");

            static Logger()
            {
                if (!Config.debugLog) return;
                
                try
                {
                    using (StreamWriter writer = new StreamWriter(LogFilePath, false))
                    {
                        writer.WriteLine("===== Dual Wield Reboot Log Started =====");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize log: {ex.Message}");
                }
            }

            public static void Empty()
            {
                if (Config.debugLog)
                    File.Create(LogFilePath).Close();
            }

            public static void Log(string message)
            {
                if (!Config.debugLog) return;
                
                try
                {
                    using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                    {
                        writer.WriteLine($"{DateTime.Now}: {message}");
                    }
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Logging error: {ex.Message}");
                }
            }
        }
    }
}
