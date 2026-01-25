using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using DualWield.Enums;


//MC_Wpn InfiniteAmmoClip to fix rpgs reload visually

namespace DualWield
{
    public class Main : Script
    {
        public static bool DualWielding;
        private bool wasDualWielding;
        private bool isMinigun;
        private bool imReloading;
        private bool isDummyLoaded;
        private bool unlimitedAmmo;
        private bool gunSwapped;
        private bool noScoping;
        private bool aiming;

        private int? dualWieldEndTime = null;
        private int? aimStartTime = null;
        private int? aimReleaseTime = null;

        private int LeftMag;
        private int RightMag;
        private ReloadingPhase imReloadingPhase = ReloadingPhase.NotReloading;
        private int snipingEndTime = 0;

        private int lastRecoilTime = 0;

        private float animPitchSource;
        private float recoilValue;
        private float reloadTimer = 0f;

        private float padButtonTimer = 0f;
        private float padButtonTimer2 = 0f;

        private Model dummy = PedHash.Famdnf01GMY;
        public static Ped ShooterL;
        public static Ped ShooterR;

        public static Entity GunL;
        public static Entity GunR;
        public static Ped MC = Game.Player.Character;

        public static readonly List<WeaponGroup> AllowedGuns = new List<WeaponGroup>()
        { WeaponGroup.Pistol, WeaponGroup.SMG, WeaponGroup.AssaultRifle, WeaponGroup.MG, WeaponGroup.Shotgun,
            WeaponGroup.Heavy, WeaponGroup.Sniper };

        private static readonly ClipSet MoveClipset = new ClipSet("weapons@pistol@");
        private static readonly ClipSet WalkClipset = new ClipSet("move_m@brave");
        private static readonly CrClipAsset ShootdodgeClipset = new CrClipAsset("amb@world_human_sunbathe@female@front@base", "base");
        private static readonly CrClipAsset SeparatedHandAnim = new CrClipAsset("move_fall@weapons@jerrycan", "land_walk_arms");

        private static readonly Vector3 AimPosL = new Vector3(0.045f, 0f, 0.015f);
        private static readonly Vector3 AimRotL = new Vector3(80f, 180f, 180f);

        private static readonly Vector3 AimPosR = new Vector3(0.045f, 0f, -0.015f);
        private static readonly Vector3 AimRotR = new Vector3(90f, 180f, 180f);

        public static int Shootdodge;
        public static Type DodgeType;
        public static FieldInfo DodgeField;

        public static Weapon MC_Wpn;
        private Weapon lastWpn;

        public static bool Conflict = false;
        public static bool Notified = false;
        public static float ikRecoil = 1f;

        
        private const float RELOAD_DELAY = 0.25f;
        private const int RECOIL_COOLDOWN = 180;
        
        private const int SHOOT_READY_DELAY = 600; //wait for aiming anim to finish pointing gun
        private const int AIM_EXIT_DELAY = 800; //hold crosshair for this long after done shooting
        public Main()
        {
            Utils.Logger.Empty();
            Utils.ConflictGetter();
            Utils.WeaponComponentsHashCache = WeaponComponent.GetAllHashes();
            
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Aborted += OnAbort;
        }
        
        private void OnTick(object sender, EventArgs e)
        {
            const int SNIPING_TIME = 1200;
            const int WIDOWMAKER_SNIPING_TIME = 450;
            
            MC = Game.Player.Character;
            MC_Wpn = MC.Weapons.Current;

            // Load model OnTick! Ped simply not ready fast enough
            isDummyLoaded = Function.Call<bool>(Hash.HAS_MODEL_LOADED, dummy);
            if (!isDummyLoaded)
            {
                Function.Call(Hash.REQUEST_MODEL, dummy);
                Utils.Logger.Log("Loading dummy model, returning...");
                return;
            }

            Utils.CheckConflict();
            CheckController();

            DrawHUD();

            //Destroy FPV Cam
            if (Config.useFPCam && !DualWielding && FPV.Camera != null)
            {
                FPV.Destroy();
            }

            //Immediate End
            if (DualWielding && !gunSwapped && (
                Utils.IsPedEnteringVehicle(MC) || MC.IsDead || Game.IsCutsceneActive || !Game.Player.IsPlaying || MC.IsSwimming ||
                (
                    (
                        (!Utils.IsMinigunType(MC_Wpn) && MC_Wpn.Ammo - MC_Wpn.AmmoInClip <= MC_Wpn.MaxAmmoInClip && MC_Wpn.MaxAmmoInClip != 1)
                        ||
                        (!Utils.IsMinigunType(MC_Wpn) && MC_Wpn.Ammo - MC_Wpn.AmmoInClip <= (MC_Wpn.MaxAmmoInClip * 2) && MC_Wpn.MaxAmmoInClip == 1)
                        ||
                        (Utils.IsMinigunType(MC_Wpn) && MC_Wpn.AmmoInClip <= 1)
                    ) &&
                    ((LeftMag < 1 && RightMag < 1) || Game.IsControlJustPressed(GTA.Control.Reload))
                )
                ||
                !AllowedGuns.Contains(MC_Wpn.Group)
            ))
                EndDualWield();

            if (!DualWielding)
                return;

            //FPS Camera - because vanilla fpv butchers the mod
            if (Config.useFPCam)
            {
                if (GameplayCamera.FollowPedCamViewMode == CamViewMode.FirstPerson)
                {
                    if (FPV.Camera == null)
                        FPV.Create();
                }
                else FPV.Destroy();
                FPV.Update();
            }

            animPitchSource = GameplayCamera.RelativePitch + recoilValue;
            if (FPV.Active)
                animPitchSource = FPV.Camera.Rotation.X + recoilValue;

            Utils.IsSuppressorActive = MC_Wpn.Components.GetSuppressorComponent().Active;
            Utils.SortPtfx();

            //Prevent ped distraction
            foreach (Ped ped in World.GetAllPeds())
            {
                if (ped.IsInCombatAgainst(ShooterL) || ped.IsInCombatAgainst(ShooterR))
                {
                    ped.Task.ClearAll();
                    ped.Task.Combat(MC);
                }
            }

            //OnTick prop attachment + Ped flags (force set every tick and prevent lost gun prop)
            if (ShooterL != null && ShooterL.Exists())
                PedKeeper(ShooterL);
            if (ShooterR != null && ShooterR.Exists())
                PedKeeper(ShooterR);

            if (!MC.IsInVehicle() && AllowedGuns.Contains(MC_Wpn.Group))
            {
                GunL = ShooterL.Weapons.CurrentWeaponObject;
                GunR = ShooterR.Weapons.CurrentWeaponObject;
                ShooterL.PositionNoOffset = MC.Position + new Vector3(0f, 0f, 5f);
                ShooterR.PositionNoOffset = MC.Position + new Vector3(0f, 0f, 5f);

                //Disable Roll & Weapon swapping when reloading
                if (MC.IsAiming)
                    Game.DisableControlThisFrame(GTA.Control.Jump);
                if (MC.IsReloading)
                {
                    Game.DisableControlThisFrame(GTA.Control.Reload);
                    Game.DisableControlThisFrame(GTA.Control.SelectWeapon); Game.DisableControlThisFrame(GTA.Control.SelectPrevWeapon); Game.DisableControlThisFrame(GTA.Control.SelectNextWeapon);
                    Hud.HideComponentThisFrame(HudComponent.WeaponWheel);
                }

                //New Weapon Animation System
                AnimationSystemV2();

                //Weapon change when aiming in FirstPersonView broke the script. Fuck It!
                if (MC.IsAiming && GameplayCamera.FollowPedCamViewMode == CamViewMode.FirstPerson)
                {
                    Utils.SetInverseKinematics(false);
                    Game.DisableControlThisFrame(GTA.Control.SelectWeapon); Game.DisableControlThisFrame(GTA.Control.SelectPrevWeapon); Game.DisableControlThisFrame(GTA.Control.SelectNextWeapon);
                    Hud.HideComponentThisFrame(HudComponent.WeaponWheel);
                }

                //ShootdodgeSequence
                if (Shootdodge != 0)
                {
                    if (!MC.IsPlayingAnimation(ShootdodgeClipset))
                        MC.Task.PlayAnimation(ShootdodgeClipset, AnimationBlendDelta.NormalBlendIn, AnimationBlendDelta.NormalBlendOut, -1, (AnimationFlags)49, 0f);
                }
                else if (MC.IsPlayingAnimation(ShootdodgeClipset))
                    MC.Task.StopScriptedAnimationTask(ShootdodgeClipset);


                //ReloadSequence
                if (Shootdodge == 0)
                {
                    bool leftNotFull = LeftMag < MC_Wpn.MaxAmmoInClip;
                    bool rightNotFull = RightMag < MC_Wpn.MaxAmmoInClip;

                    while (MC.IsReloading || MC.IsRagdoll)
                        Yield();

                    if (!imReloading)
                    {
                        if (Game.IsControlJustPressed(GTA.Control.Reload) && (leftNotFull || rightNotFull))
                        {
                            Game.DisableControlThisFrame(GTA.Control.MeleeAttackLight);
                            Game.DisableControlThisFrame(GTA.Control.MeleeAttackAlternate);
                            Game.DisableControlThisFrame(GTA.Control.MeleeAttack1);
                            Game.DisableControlThisFrame(GTA.Control.MeleeAttack2);

                            if (leftNotFull)
                            {
                                ReloadLeft();
                                imReloadingPhase = ReloadingPhase.ReloadingLeft;
                            }
                            else //If statement can be removed because it's already checked in the parent if-statement
                            {
                                ReloadRight();
                                imReloadingPhase = ReloadingPhase.ReloadingRight;
                            }
                            
                            imReloading = true;
                            reloadTimer = 0f;
                        }
                    }
                    else
                    {
                        reloadTimer += Game.LastFrameTime;

                        if (reloadTimer >= RELOAD_DELAY && !MC.IsReloading)
                        {
                            if (imReloadingPhase == ReloadingPhase.ReloadingLeft && rightNotFull)
                            {
                                ReloadRight();
                                imReloadingPhase = ReloadingPhase.ReloadingRight;
                                reloadTimer = 0f;
                            }
                            else
                            {
                                imReloading = false;
                                imReloadingPhase = 0;
                            }
                        }
                    }
                }
                else
                {
                    Game.DisableControlThisFrame(GTA.Control.Reload);

                    if (LeftMag == 0)
                    {
                        MC_Wpn.Ammo -= LeftMag;
                        MC_Wpn.AmmoInClip = 0;
                        Game.TimeScale = 1f;
                        Game.Player.DisableFiringThisFrame();
                    }

                    if (RightMag == 0)
                    {
                        MC_Wpn.Ammo -= RightMag;
                        MC_Wpn.AmmoInClip = 0;
                        Game.TimeScale = 1f;
                        Game.Player.DisableFiringThisFrame();
                    }
                }
                //Prevent Normal Reload
                if (MC_Wpn.AmmoInClip < MC_Wpn.MaxAmmoInClip)
                {
                    Game.DisableControlThisFrame(GTA.Control.Reload);
                    if (Game.IsControlJustPressed(GTA.Control.Reload))
                        MC_Wpn.AmmoInClip = MC_Wpn.MaxAmmoInClip;
                }

                //WeaponVisibility
                if (!MC.IsVisible || !MC.IsRendered)
                {
                    if (GunL.IsVisible)
                        GunL.IsVisible = false;

                    if (GunR.IsVisible)
                        GunR.IsVisible = false;
                }
                else
                {
                    if (imReloading)
                    {
                        GunL.IsVisible = imReloadingPhase == ReloadingPhase.ReloadingRight;
                        GunR.IsVisible = false;
                        Utils.ShowPlayerWpn(true);
                    }
                    else
                    {
                        GunL.IsVisible = true;
                        GunR.IsVisible = true;
                        Utils.ShowPlayerWpn(false);
                    }
                }


                //ShootingSequence
                bool leftFire;
                bool rightFire;
                bool semiLeft;
                bool semiRight;

                if (Game.LastInputMethod == InputMethod.GamePad)
                {
                    leftFire = Game.IsControlPressed(GTA.Control.Aim);
                    rightFire = Game.IsControlPressed(GTA.Control.Attack);
                    semiLeft = Game.IsControlJustPressed(GTA.Control.Aim);
                    semiRight = Game.IsControlJustPressed(GTA.Control.Attack);
                }
                else
                {
                    leftFire = Game.IsControlPressed(GTA.Control.Attack);
                    rightFire = Game.IsControlPressed(GTA.Control.Aim);
                    semiLeft = Game.IsControlJustPressed(GTA.Control.Attack);
                    semiRight = Game.IsControlJustPressed(GTA.Control.Aim);
                }
                
                if (Game.WasCheatStringJustEntered("RAMBO"))
                {
                    if (unlimitedAmmo == false)
                    {
                        Notification.PostTicker("Congrats!! You've activated ~g~Unlimited Ammo ~w~cheat on ~b~Dual Wield Reboot. ~w~~n~Type the cheat again to deactivate on this session", false);
                        unlimitedAmmo = true;
                    }
                    else unlimitedAmmo = false;
                }

                Game.DisableControlThisFrame(GTA.Control.Aim);
                Game.DisableControlThisFrame(GTA.Control.Attack);
                Game.DisableControlThisFrame(GTA.Control.Attack2);

                //Force remove particle-fx
                MC.Weapons.CurrentWeaponObject.RemoveParticleEffects();

                if (Utils.WeaponDamageType(MC_Wpn) != 5 && MC_Wpn.Group != WeaponGroup.Sniper && MC_Wpn != WeaponHash.Widowmaker)
                {
                    if ((semiLeft || semiRight) && !aiming)
                    {
                        Game.Player.ForcedAim = true;
                        aiming = true;
                        aimStartTime = Game.GameTime;
                        aimReleaseTime = null;
                    }

                    //SHOOTING
                    if (aiming && aimStartTime.HasValue && Game.GameTime - aimStartTime.Value >= SHOOT_READY_DELAY)
                    {
                        if (MC.IsReloading && imReloading)
                            return;

                        if (MC.IsAiming)
                        {
                            MC.SetNotDamagedByRelGroup(MC.RelationshipGroup);
                            MC.SetNotDamagedByRelGroup(ShooterL.RelationshipGroup);
                            MC.SetNotDamagedByRelGroup(ShooterR.RelationshipGroup);

                            if ((leftFire && LeftMag > 0) || (rightFire && RightMag > 0))
                            {
                                Vector3 coord = MC.Position - MC.ForwardVector * 9999f;
                                if (Function.Call<int>(Hash.GET_PED_ORIGINAL_AMMO_TYPE_FROM_WEAPON, MC, MC_Wpn.Hash) == Function.Call<int>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON, MC, MC_Wpn.Hash))
                                    Function.Call(Hash.SET_PED_SHOOTS_AT_COORD, MC, coord.X, coord.Y, coord.Z, true);
                                else
                                {
                                    Vector3 crosshair = World.GetCrosshairCoordinates().HitPosition;
                                    //For Mk II Weapon Special Ammo, revert to the old v1 way 
                                    Function.Call(Hash.SET_PED_SHOOTS_AT_COORD, MC, crosshair.X, crosshair.Y, crosshair.Z, false);
                                }
                            }

                            if (MC.IsShooting)
                            {
                                if (leftFire && LeftMag > 0)
                                {
                                    Utils.ShootAt(ShooterL, GunL);
                                    if (unlimitedAmmo == false)
                                        LeftMag--;
                                    PlayRecoilAnim();
                                }

                                if (rightFire && RightMag > 0)
                                {
                                    Utils.ShootAt(ShooterR, GunR);
                                    if (unlimitedAmmo == false)
                                        RightMag--;
                                    PlayRecoilAnim();
                                }
                            }
                        }
                        else MC.ClearNotDamagedByRelGroup();
                    }

                    //RELEASE & EXIT AIM AFTER DELAY
                    bool bothReleased = !leftFire && !rightFire;

                    if (aiming)
                    {
                        if (bothReleased)
                        {
                            if (!aimReleaseTime.HasValue)
                                aimReleaseTime = Game.GameTime;

                            if (Game.GameTime - aimReleaseTime.Value >= AIM_EXIT_DELAY)
                            {
                                Game.Player.ForcedAim = false;
                                aiming = false;
                                aimStartTime = null;
                                aimReleaseTime = null;

                                if (Utils.WeaponDamageType(MC_Wpn) == 5)
                                    MC.ClearOnlyDamagedByRelGroup();
                            }
                        }
                        else
                        {
                            aimReleaseTime = null;
                        }
                    }
                }
                // Separated Explosive Weapon - prevent early explosion, ammo bug etc
                else if (Utils.WeaponDamageType(MC_Wpn) == 5)
                {
                    ShooterL.Weapons.Current.InfiniteAmmoClip = false;
                    ShooterR.Weapons.Current.InfiniteAmmoClip = false;


                    if (semiLeft && !semiRight && LeftMag != 0)
                    {
                        noScoping = true;
                        if (Utils.GunReadyToShoot(ShooterL) && MC.IsPlayingAnimation(Utils.AimAnim))
                        {
                            Game.Player.DisableFiringThisFrame();
                            Utils.ShootAt(ShooterL, GunL);
                            if (unlimitedAmmo == false)
                                LeftMag--;
                            PlayRecoilAnim();
                            ShooterL.Weapons.Current.AmmoInClip -= 1;
                        }
                        snipingEndTime = Game.GameTime + SNIPING_TIME;
                    }
                    if (semiRight && !semiLeft && RightMag != 0)
                    {
                        noScoping = true;
                        if (Utils.GunReadyToShoot(ShooterR) && MC.IsPlayingAnimation(Utils.AimAnim))
                        {
                            Game.Player.DisableFiringThisFrame();
                            Utils.ShootAt(ShooterR, GunR);
                            if (unlimitedAmmo == false)
                                RightMag--;
                            PlayRecoilAnim();
                            ShooterR.Weapons.Current.AmmoInClip -= 1;
                        }
                        snipingEndTime = Game.GameTime + SNIPING_TIME;
                    }
                    if (semiLeft && LeftMag != 0 && semiRight && RightMag != 0)
                    {
                        noScoping = true;
                        if (Utils.GunReadyToShoot(ShooterL) && Utils.GunReadyToShoot(ShooterR) && MC.IsPlayingAnimation(Utils.AimAnim))
                        {
                            Game.Player.DisableFiringThisFrame();
                            Utils.ShootAt(ShooterL, GunL);
                            if (unlimitedAmmo == false)
                                LeftMag--;
                            ShooterL.Weapons.Current.AmmoInClip -= 1;
                            Utils.ShootAt(ShooterR, GunR);
                            if (unlimitedAmmo == false)
                                RightMag--;
                            ShooterR.Weapons.Current.AmmoInClip -= 1;
                            PlayRecoilAnim();
                        }
                        snipingEndTime = Game.GameTime + SNIPING_TIME;
                    }
                }
                // Separated sniping to prevent scope overlay
                else if (MC_Wpn.Group == WeaponGroup.Sniper)
                {
                    if (semiLeft && !semiRight && LeftMag != 0)
                    {
                        noScoping = true;
                        if (Utils.GunReadyToShoot(ShooterL) && MC.IsPlayingAnimation(Utils.AimAnim))
                        {
                            Game.Player.DisableFiringThisFrame();
                            Utils.ShootAt(ShooterL, GunL);
                            if (unlimitedAmmo == false)
                                LeftMag--;
                            PlayRecoilAnim();
                        }
                        snipingEndTime = Game.GameTime + SNIPING_TIME;

                    }
                    if (semiRight && !semiLeft && RightMag != 0)
                    {
                        noScoping = true;
                        if (Utils.GunReadyToShoot(ShooterR) && MC.IsPlayingAnimation(Utils.AimAnim))
                        {
                            Game.Player.DisableFiringThisFrame();
                            Utils.ShootAt(ShooterR, GunR);
                            if (unlimitedAmmo == false)
                                RightMag--;
                            PlayRecoilAnim();
                        }
                        snipingEndTime = Game.GameTime + 1200;
                    }
                    if (semiLeft && LeftMag != 0 && semiRight && RightMag != 0)
                    {
                        noScoping = true;
                        if (Utils.GunReadyToShoot(ShooterL) && Utils.GunReadyToShoot(ShooterR) && MC.IsPlayingAnimation(Utils.AimAnim))
                        {
                            Game.Player.DisableFiringThisFrame();
                            Utils.ShootAt(ShooterL, GunL);
                            if (unlimitedAmmo == false)
                                LeftMag--;
                            Utils.ShootAt(ShooterR, GunR);
                            if (unlimitedAmmo == false)
                                RightMag--;
                            PlayRecoilAnim();
                        }
                        snipingEndTime = Game.GameTime + 1200;
                    }
                }

                // Fuck Widowmaker, GTFO!
                else if (MC_Wpn == WeaponHash.Widowmaker)
                {
                    if (leftFire && LeftMag != 0 && rightFire && RightMag != 0)
                    {
                        noScoping = true;
                        if (Utils.GunReadyToShoot(ShooterL) && Utils.GunReadyToShoot(ShooterR))
                        {
                            Game.Player.DisableFiringThisFrame();
                            Utils.ShootAt(ShooterL, GunL);
                            if (unlimitedAmmo == false)
                                LeftMag--;
                            Utils.ShootAt(ShooterR, GunR);
                            if (unlimitedAmmo == false)
                                RightMag--;
                            PlayRecoilAnim();
                        }


                        snipingEndTime = Game.GameTime + WIDOWMAKER_SNIPING_TIME;
                    }
                }

                if (snipingEndTime != 0 && Game.GameTime >= snipingEndTime)
                {
                    noScoping = false;
                    snipingEndTime = 0;
                }
                if (noScoping && GameplayCamera.FollowPedCamViewMode != CamViewMode.FirstPerson && !FPV.Active)
                {
                    MC.Rotation = new Vector3(MC.Rotation.X, MC.Rotation.Y, GameplayCamera.Rotation.Z);
                }

                //Prevent projectile damaging player - early collision
                Projectile[] projectiles = World.GetNearbyProjectiles(MC.Position, 5f);
                foreach (Projectile proj in projectiles)
                {
                    if (proj.OwnerEntity == MC || proj.OwnerEntity == ShooterL || proj.OwnerEntity == ShooterR)
                    {
                        if (Vector3.Distance(proj.Position, MC.Position) <= 3f)
                        {
                            proj.SetNoCollision(MC, true);
                            if (Utils.WeaponDamageType(MC_Wpn) != 5)
                            {
                                proj.SetNoCollision(GunR, true);
                                proj.SetNoCollision(GunL, true);
                            }
                        }
                        else if (Vector3.Distance(proj.Position, MC.Position) > 5f)
                        {
                            proj.SetNoCollision(MC, false);
                            if (Utils.WeaponDamageType(MC_Wpn) != 5)
                            {
                                proj.SetNoCollision(GunR, false);
                                proj.SetNoCollision(GunL, false);
                            }
                        }
                    }
                }
            }

            //WeaponSwitching
            if (Game.IsControlJustPressed(GTA.Control.SelectWeapon))
                lastWpn = MC_Wpn;
            if (Utils.IsSwitchingGun(MC))
            {
                gunSwapped = true;
                EndDualWield();
            }
            if (gunSwapped && AllowedGuns.Contains(MC_Wpn.Group))
            {
                gunSwapped = false;
                Wait(500);
                StartDualWield();
            }
        }
        private void OnAbort(object sender, EventArgs e) => EndDualWield();
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Config.toggleKey) return;
                
            //MARK: What if the game is paused, should this still be called?
            if (Utils.AnimsLoaded() && isDummyLoaded)
            {
                if (!DualWielding)
                {
                    StartDualWield();
                }
                else
                    EndOnPressed();
            }
        }

        private void StartDualWield()
        {
            isMinigun = Utils.IsMinigunType(MC_Wpn);

            if (DualWielding)
                return;

            if (MC_Wpn == null || !MC_Wpn.IsPresent)
                return;

            if (
                MC.IsInVehicle() ||
                MC.IsRagdoll ||
                !MC.IsAlive ||
                MC.IsReloading ||
                MC.IsFalling ||
                MC.IsSwimming ||
                Shootdodge != 0
            )
                return;

            if (!AllowedGuns.Contains(MC_Wpn.Group))
                return;

            int ammo = MC_Wpn.Ammo;
            int ammoInClip = MC_Wpn.AmmoInClip;
            int maxAmmoInClip = MC_Wpn.MaxAmmoInClip;

            if (ammoInClip == 0)
                return;

            if (!isMinigun)
            {
                int diff = ammo - ammoInClip;

                if (maxAmmoInClip == 1)
                {
                    if (diff < 2) // maxAmmoInClip * 2 ->  1 * 2 -> 2
                        return;
                }
                else
                {
                    if (diff < maxAmmoInClip)
                        return;
                }
            }
            else
            {
                if (ammoInClip <= 1)
                    return;
            }

            if (isMinigun)
            {
                // Minigun have MaxAmmo = MaxAmmoInClip behavior
                int total = MC_Wpn.Ammo;
                LeftMag = total / 2;
                RightMag = total - LeftMag;
                MC_Wpn.Ammo = MC_Wpn.MaxAmmo;
                MC_Wpn.AmmoInClip = MC_Wpn.MaxAmmoInClip;
            }
            else
            {
                LeftMag = MC_Wpn.AmmoInClip;
                RightMag = MC_Wpn.AmmoInClip;
                MC_Wpn.Ammo -= LeftMag + RightMag;
            }

            if (Conflict)
                Notified = false;

            imReloading = false;

            ShooterL = CreateFakeShooter();
            ShooterR = CreateFakeShooter();

            if (ShooterL == null || ShooterR == null)
                return;
            Utils.SortAnim(MC.Weapons.Current);
            Utils.ShowPlayerWpn(false);
            MC_Wpn.InfiniteAmmoClip = true;
            ShooterL.Weapons.Current.InfiniteAmmoClip = true;
            ShooterL.Weapons.Current.InfiniteAmmo = true;
            ShooterR.Weapons.Current.InfiniteAmmoClip = true;
            ShooterR.Weapons.Current.InfiniteAmmo = true;
            Utils.AlterWeaponClipSet(true, MoveClipset);

            lastWpn = MC_Wpn;
            DualWielding = true;
            wasDualWielding = false;
            dualWieldEndTime = null;
        }

        private Ped CreateFakeShooter()
        {
            Ped ped = (Ped)Entity.FromHandle(Function.Call<int>(Hash.CREATE_PED, 26, dummy, MC.Position.X, MC.Position.Y, MC.Position.Z + 5f, MC.Heading, false, false));
            ped.IsVisible = false;
            ped.Weapons.Give(MC_Wpn.Hash, MC_Wpn.MaxAmmo, true, true);
            Utils.GetAttachments(ped);
            ped.Weapons.Select(MC_Wpn.Hash, true);
            if (Utils.DoesExists(ped))
                return ped;
            else
            {
                Utils.Logger.Log($"Dummy ped creation returns null");
                return null;
            }
        }

        private void ReloadLeft()
        {
            int lastMag = MC_Wpn.AmmoInClip; // needed to replace Task.ReloadWeapon technique;
            MC.BlocksAnyDamageButHasReactions = true;
            MC.Task.StopScriptedAnimationTask(SeparatedHandAnim, AnimationBlendDelta.InstantBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.WalkingAnim, AnimationBlendDelta.NormalBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.RunningAnim, AnimationBlendDelta.NormalBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.IdleAnim, AnimationBlendDelta.NormalBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.AimAnim, AnimationBlendDelta.NormalBlendOut);
            MC_Wpn.AmmoInClip = 0; // needed to replace Task.ReloadWeapon
            Yield();

            if (!MC.IsPlayingAnimation(new CrClipAsset("melee@holster", "holster")))
            {
                MC.Task.PlayAnimation(new CrClipAsset("melee@holster", "holster"), AnimationBlendDelta.NormalBlendIn, AnimationBlendDelta.VerySlowBlendOut, 800, (AnimationFlags)48, 0f);
                Wait(800);
            }

            MC_Wpn.Ammo += lastMag;
            Function.Call(Hash.MAKE_PED_RELOAD, MC);
            Hud.HideComponentThisFrame(HudComponent.WeaponIcon);
            //MC.Task.ReloadWeapon();

            int needed = MC_Wpn.MaxAmmoInClip - LeftMag;
            if (MC_Wpn.Ammo >= needed)
            {
                LeftMag += needed;
                MC_Wpn.Ammo -= needed;
            }
            else
            {
                LeftMag += MC_Wpn.Ammo;
                MC_Wpn.Ammo = 0;
            }

            if (Utils.WeaponDamageType(MC_Wpn) == 5 && MC_Wpn.Group == WeaponGroup.Heavy && ShooterL.Weapons.Current.AmmoInClip == 0)
            {
                ShooterL.Task.ReloadWeapon();
            } //Required for rocket removal from weapon object upon shooting

            MC.BlocksAnyDamageButHasReactions = false;
        }

        private void ReloadRight()
        {
            // Simply a copy with change in ammo
            int lastMag = MC_Wpn.AmmoInClip;
            MC.BlocksAnyDamageButHasReactions = true;
            MC.Task.StopScriptedAnimationTask(SeparatedHandAnim, AnimationBlendDelta.InstantBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.WalkingAnim, AnimationBlendDelta.NormalBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.RunningAnim, AnimationBlendDelta.NormalBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.IdleAnim, AnimationBlendDelta.NormalBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.AimAnim, AnimationBlendDelta.NormalBlendOut);
            MC_Wpn.AmmoInClip = 0;
            Yield();
            MC_Wpn.Ammo += lastMag;
            Function.Call(Hash.MAKE_PED_RELOAD, MC);
            Hud.HideComponentThisFrame(HudComponent.WeaponIcon);
            //MC.Task.ReloadWeapon();

            int needed = MC_Wpn.MaxAmmoInClip - RightMag;
            if (MC_Wpn.Ammo >= needed)
            {
                RightMag += needed;
                MC_Wpn.Ammo -= needed;
            }
            else
            {
                RightMag += MC_Wpn.Ammo;
                MC_Wpn.Ammo = 0;
            }

            if (Utils.WeaponDamageType(MC_Wpn) == 5 && MC_Wpn.Group == WeaponGroup.Heavy && ShooterR.Weapons.Current.AmmoInClip == 0)
            {
                ShooterR.Task.ReloadWeapon();
            }

            MC.BlocksAnyDamageButHasReactions = false;
        }

        private void EndDualWield()
        {
            Game.Player.ForcedAim = false;
            Utils.SetInverseKinematics(false);
            Utils.AlterWeaponClipSet(false, MoveClipset);
            MC.Task.StopScriptedAnimationTask(Utils.AimAnim, AnimationBlendDelta.SlowBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.IdleAnim, AnimationBlendDelta.VerySlowBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.WalkingAnim, AnimationBlendDelta.WalkBlendOut);
            MC.Task.StopScriptedAnimationTask(Utils.RunningAnim, AnimationBlendDelta.WalkBlendOut);
            ShooterL.Delete();
            ShooterR.Delete();
            Utils.ShowPlayerWpn(true);
            MC_Wpn.InfiniteAmmoClip = false;

            if (!gunSwapped)
            {
                if (isMinigun)
                {
                    MC_Wpn.Ammo = LeftMag + RightMag;
                }
                else if (MC_Wpn.Group == WeaponGroup.Melee || MC_Wpn.Group == WeaponGroup.Unarmed)
                {
                    lastWpn.Ammo += LeftMag + RightMag;
                }
                else
                {
                    if (LeftMag > 0 || RightMag > 0)
                        MC_Wpn.AmmoInClip = 0;

                    MC_Wpn.Ammo += LeftMag + (RightMag * 2);
                }

            }
            else
            {
                if (isMinigun)
                {
                    lastWpn.Ammo = LeftMag + RightMag;
                }
                else
                {
                    lastWpn.Ammo += LeftMag + RightMag;
                    //Notification.PostTicker("Gun Swap Called", true);
                }
            }

            if (MC.IsAiming && Utils.WeaponDamageType(MC_Wpn) != 5)
                MC.ClearNotDamagedByRelGroup();

            Utils.ReleaseAnims();
            DualWielding = false;
            wasDualWielding = true;
            noScoping = false;
            dualWieldEndTime = Game.GameTime;
        }

        private void EndOnPressed()
        {
            if (DualWielding)
            {
                MC.Task.PlayAnimation("melee@holster", "holster", 8f, -1, (AnimationFlags)48);
                if (MC.GetAnimationCurrentTime(new CrClipAsset("melee@holster", "holster")) < 1.0f)
                    Yield();
                EndDualWield();
            }
        }

        private void DrawHUD()
        {
            if ((!wasDualWielding && !DualWielding) || !AllowedGuns.Contains(MC_Wpn.Group) || !Config.useHUD)
                return;

            float scaleHud = 1f;

            bool blinkDraw = ((Game.GameTime / 700) & 1 ) == 0;

            if (MC_Wpn.Ammo - MC_Wpn.AmmoInClip <= MC_Wpn.MaxAmmoInClip)
            {
                if (wasDualWielding && dualWieldEndTime.HasValue)
                {
                    if (blinkDraw)
                        Utils.DrawIcon("mplastgunslingershud", "type_prompt_no_ammo", Color.White, 76f * scaleHud, 24.5f * scaleHud, 1183f, 100f, 0f);

                    if (Game.GameTime - dualWieldEndTime.Value >= 3000)
                    {
                        wasDualWielding = false;
                        dualWieldEndTime = null;
                    }
                }
            }

            if (!DualWielding)
                return;

            Hud.HideComponentThisFrame(HudComponent.WeaponIcon);

            (string textureDict, string textureName)? iconData = Utils.WeaponGroupIcon(MC_Wpn.Group);

            if ((LeftMag != 0 || RightMag != 0) && !MC.IsReloading && Config.useCrosshair)
                Hud.ShowComponentThisFrame(HudComponent.Reticle);

            //new TextElement("~y~ Test ~w~v2.0 Dual Wield", new PointF(50f, 38f), 0.5f).ScaledDraw();
            //new TextElement("Aim Anim: " + Utils.AimAnim.ClipName, new PointF(50f, 58f), 0.25f).ScaledDraw();

            if (!MC.IsReloading && !imReloading)
            {
                if (iconData != null || (LeftMag != 0 && RightMag != 0))
                {
                    string dict = iconData.Value.textureDict;
                    string name = iconData.Value.textureName;

                    Utils.DrawIcon(dict, name, Utils.GetAmmoColor(LeftMag, MC_Wpn.MaxAmmoInClip), 32f * scaleHud, 64f * scaleHud, 1185f, 80f, -90f);
                    Utils.DrawIcon(dict, name, Utils.GetAmmoColor(RightMag, MC_Wpn.MaxAmmoInClip), 32f * scaleHud, 64f * scaleHud, 1225f, 80f, -270f);
                }
                new TextElement($"{LeftMag}", new PointF(1195f, 146f), 0.35f * scaleHud, Color.WhiteSmoke, GTA.UI.Font.Pricedown, Alignment.Center, false, true).Draw();
                new TextElement($"{RightMag}", new PointF(1245f, 146f), 0.35f * scaleHud, Color.WhiteSmoke, GTA.UI.Font.Pricedown, Alignment.Center, false, true).Draw();
            }

            if (LeftMag == 0 && RightMag == 0 && blinkDraw)
                Utils.DrawIcon("mplastgunslingershud", "type_prompt_reload", Color.White, 76f * scaleHud, 24.5f * scaleHud, 1183f, 100f, 0f);

        }

        private void CheckController()
        {
            if (Game.LastInputMethod == InputMethod.GamePad && MC.IsAiming)
            {
                if (padButtonTimer2 > 0)
                {
                    padButtonTimer2 -= Game.LastFrameTime * 1000;
                    return;
                }

                if (Game.IsControlPressed(GTA.Control.PhoneRight))
                {
                    const int HOLDING_TIME = 1000;
                    padButtonTimer += Game.LastFrameTime * HOLDING_TIME;
                    if (padButtonTimer >= HOLDING_TIME) // Holding timer
                    {
                        if (!DualWielding)
                        {
                            StartDualWield();
                        }
                        else
                        {
                            EndOnPressed();
                        }
                        padButtonTimer2 = 3000f;// Cooldown after gamepad hold button
                    }
                }
                else
                {
                    padButtonTimer = 0f;
                }
            }
        }

        private void AnimationSystemV2() //fuck clean code, this is easier for me EDIT: Lets not "fuck" clean code pls
        {
            bool doesCWeaponShootRockets = MC_Wpn == WeaponHash.RPG || MC_Wpn == WeaponHash.HomingLauncher || MC_Wpn == WeaponHash.Firework;

            bool baseCheck = !isMinigun && !doesCWeaponShootRockets;

            bool noWalkAnim;
            bool noRunAnim;
            bool noIdleAnim;
            
            if (baseCheck)
            {
                noWalkAnim = Config.noWalkAnim;
                noIdleAnim = Config.noIdleAnim;
                noRunAnim = Config.noRunAnim;
            }
            else
            {
                noWalkAnim = false;
                noIdleAnim = false;
                noRunAnim = false;
            }


            //In-Cover & Jumping Pose
            if (!MC.IsAiming && (MC.IsJumping || Utils.IsLanding(MC)))
            {
                if (Utils.AimAnim == Utils.RPG)
                    return;
                if (!MC.IsPlayingAnimation(SeparatedHandAnim))
                    MC.Task.PlayAnimation(SeparatedHandAnim, AnimationBlendDelta.FastBlendIn, AnimationBlendDelta.WalkBlendOut, -1, (AnimationFlags)48, 0f);
            }
            else if ((MC.IsInCover || MC.IsGoingIntoCover) && !Utils.IsSwitchingGun(MC) && !MC.IsAiming)
            {
                Utils.SetInverseKinematics(false);
                Utils.Request(SeparatedHandAnim.ClipDictionary, 800);
                if (!MC.IsPlayingAnimation(SeparatedHandAnim))
                    MC.Task.PlayAnimation(SeparatedHandAnim, AnimationBlendDelta.VerySlowBlendIn, AnimationBlendDelta.WalkBlendOut, -1, (AnimationFlags)48, 0f);
                else if (MC.GetAnimationCurrentTime(SeparatedHandAnim) > 0.2f)
                    MC.SetAnimationSpeed(SeparatedHandAnim, 0f);
                //GTA.UI.Screen.ShowSubtitle("In-Cover Called" + MC.IsPlayingAnimation(separatedHandAnim), 500);
            }
            else MC.Task.StopScriptedAnimationTask(SeparatedHandAnim, AnimationBlendDelta.WalkBlendOut);

            //AIM
            if ((MC.IsAiming || noScoping) && !MC.IsReloading && !imReloading && !MC.IsFalling && !MC.IsInAir && !MC.IsGettingUp && !MC.IsVaulting && !MC.IsDucking && !MC.IsPerformingMeleeAction && Shootdodge == 0)
            {
                if (!MC.IsPlayingAnimation(Utils.AimAnim))
                    MC.Task.PlayAnimation(Utils.AimAnim, new AnimationBlendDelta(10f), new AnimationBlendDelta(-1f), -1, AnimationFlags.UpperBodyOnly | (AnimationFlags)49 | AnimationFlags.Loop, 0f);

                Utils.SetInverseKinematics(false);
            }
            else if (MC.IsPlayingAnimation(Utils.AimAnim))
            {
                MC.Task.StopScriptedAnimationTask(Utils.AimAnim, AnimationBlendDelta.SlowBlendOut);
            }
            UpdateAiming(); //Update Anim to Cam pitch

            //WALK
            if (!noWalkAnim)
            {
                if (!Game.IsControlPressed(GTA.Control.Sprint) && !MC.IsAiming && !noScoping && !MC.IsJumping && MC.IsWalking && !MC.IsRunning && !MC.IsSprinting && !MC.IsReloading && !imReloading && !MC.IsFalling && Utils.WalkingAnim != Utils.NormalWalk
                    && !MC.IsInAir && !MC.IsGettingUp && !MC.IsVaulting && !MC.IsDucking && !MC.IsInStealthMode && !MC.IsPerformingMeleeAction && !MC.IsInCover && !MC.IsGoingIntoCover && !Utils.IsLanding(MC) && Shootdodge == 0)
                //Because the sassy walk bug on Normal_Walk, we fall back to default
                {
                    if (!MC.IsPlayingAnimation(Utils.WalkingAnim))
                    {
                        if (!Config.useSecondary)
                            MC.Task.PlayAnimation(Utils.WalkingAnim, AnimationBlendDelta.WalkBlendIn, AnimationBlendDelta.WalkBlendOut, -1, (AnimationFlags)48 | AnimationFlags.Loop, 0f);
                        else
                            MC.Task.PlayAnimation(Utils.WalkingAnim, new AnimationBlendDelta(1f), new AnimationBlendDelta(-1f), -1, AnimationFlags.Secondary | AnimationFlags.Loop, 0f);
                    }
                    if (MC.IsPlayingAnimation(Utils.WalkingAnim) && !Config.useSecondary)
                    {
                        if (Utils.WalkingAnim == Utils.GangWalk || Utils.WalkingAnim == Utils.MG_Walk || Utils.WalkingAnim == Utils.LongGunWalk) // don't merge if, this is to fix sassy walking anim because half-body mode
                        {
                            MC.SetAnimationSpeed(Utils.WalkingAnim, 0.90f);

                            if (!WalkClipset.IsLoaded)
                                Utils.Request(WalkClipset, 800);
                            else
                                MC.SetMovementClipSet(WalkClipset);
                        }
                    }

                    //GTA.UI.Screen.ShowSubtitle("Walk Anim Called", 500);
                }
                else if (MC.IsPlayingAnimation(Utils.WalkingAnim))
                {
                    MC.Task.StopScriptedAnimationTask(Utils.WalkingAnim, AnimationBlendDelta.WalkBlendOut);
                    MC.ResetMovementClipSet();
                }
            }

            //RUN
            if (!noRunAnim)
            {
                if (!MC.IsAiming && !noScoping && !MC.IsJumping && !MC.IsWalking && (MC.IsRunning || MC.IsSprinting) && !MC.IsReloading && !imReloading && !MC.IsFalling && !MC.IsInAir && !MC.IsGettingUp
                && !MC.IsVaulting && !MC.IsDucking && !MC.IsInStealthMode && !MC.IsPerformingMeleeAction && !MC.IsInCover && !MC.IsInCover && !MC.IsGoingIntoCover && !Utils.IsLanding(MC) && Shootdodge == 0)
                {
                    if (!MC.IsPlayingAnimation(Utils.RunningAnim))
                    {
                        if (!Config.useSecondary)
                            MC.Task.PlayAnimation(Utils.RunningAnim, AnimationBlendDelta.WalkBlendIn, AnimationBlendDelta.WalkBlendOut, -1, (AnimationFlags)48 | AnimationFlags.Loop, 0f);
                        else
                            MC.Task.PlayAnimation(Utils.RunningAnim, new AnimationBlendDelta(1f), new AnimationBlendDelta(-1f), -1, AnimationFlags.Secondary | AnimationFlags.Loop, 0f);
                    }
                    //GTA.UI.Screen.ShowSubtitle("Run Anim Called", 500);
                }
                else if (MC.IsPlayingAnimation(Utils.RunningAnim))
                    MC.Task.StopScriptedAnimationTask(Utils.RunningAnim, AnimationBlendDelta.WalkBlendOut);
            }

            //IDLE
            if (!noIdleAnim)
            {
                if (!Game.IsControlPressed(GTA.Control.Attack) && !Game.IsControlPressed(GTA.Control.Attack2) && !Game.IsControlPressed(GTA.Control.Aim) && !Utils.IsSwitchingGun(MC) && !MC.IsShooting
                    && !MC.IsAiming && !noScoping && !MC.IsJumping && !MC.IsWalking && !MC.IsRunning && !MC.IsSprinting && !MC.IsReloading && !imReloading && !MC.IsFalling && !MC.IsInAir
                    && !MC.IsGettingUp && !MC.IsVaulting && !MC.IsDucking && !MC.IsInStealthMode && !MC.IsPerformingMeleeAction && !MC.IsInCover && !MC.IsGoingIntoCover && Shootdodge == 0)
                {
                    if (!MC.IsPlayingAnimation(Utils.IdleAnim))
                        MC.Task.PlayAnimation(Utils.IdleAnim, AnimationBlendDelta.WalkBlendIn, AnimationBlendDelta.WalkBlendOut, -1, AnimationFlags.Loop | AnimationFlags.Secondary, 0f);
                    //GTA.UI.Screen.ShowSubtitle("Idle Anim Called", 500);
                }
                else if (MC.IsPlayingAnimation(Utils.IdleAnim))
                    MC.Task.StopScriptedAnimationTask(Utils.IdleAnim, AnimationBlendDelta.WalkBlendOut);
            }

            if (MC.IsJumping || MC.IsVaulting)
            {
                if (!noIdleAnim)
                    MC.Task.StopScriptedAnimationTask(Utils.IdleAnim, new AnimationBlendDelta(-1f));
                if (!noWalkAnim)
                    MC.Task.StopScriptedAnimationTask(Utils.WalkingAnim, new AnimationBlendDelta(-1f));
                if (!noRunAnim)
                    MC.Task.StopScriptedAnimationTask(Utils.RunningAnim, new AnimationBlendDelta(-1f));
            }
        }

        private void UpdateAiming()
        {
            if (MC.IsPlayingAnimation(Utils.AimAnim))
            {
                if (!MC.IsShooting && !ShooterL.IsShooting && !ShooterR.IsShooting)
                {
                    MC.SetAnimationCurrentTime(Utils.AimAnim, Utils.MapPitchToPhase(GameplayCamera.RelativePitch, 0f, Utils.GetAimAnimSpeedByClipAsset()));
                }
                else
                {
                    float currentTime = MC.GetAnimationCurrentTime(Utils.AimAnim);
                    float increment = Game.LastFrameTime * 1.5f; // Adjust speed here
                    float newTime = Utils.Clamp(currentTime + increment, 0f, 1f);
                    MC.SetAnimationCurrentTime(Utils.AimAnim, newTime);
                }

                if (Utils.AimAnim == Utils.LongGun)
                    GameplayCamera.SetThirdPersonCameraRelativePitchLimitsThisUpdate(-50f, 34f);
                else if (Utils.AimAnim == Utils.Normal)
                    GameplayCamera.SetThirdPersonCameraRelativePitchLimitsThisUpdate(-55f, 35f);
                else if (Utils.AimAnim == Utils.Gang)
                    GameplayCamera.SetThirdPersonCameraRelativePitchLimitsThisUpdate(-58f, 38f);
                else if (Utils.AimAnim == Utils.MG)
                    GameplayCamera.SetThirdPersonCameraRelativePitchLimitsThisUpdate(-56f, 37f);
            }
        }

        // very experimental and stupid way to force recoil anim (will easily break <30fps)
        private void PlayRecoilAnim()
        {
            if (Game.FPS <= 15f)
                return;

            bool isPressed = Game.IsControlPressed(GTA.Control.Attack) || Game.IsControlPressed(GTA.Control.Aim);
            
            float fpsFactor = Game.FPS < 30f ? Game.FPS / 15f : 1f; //15f Represents the FPS-Target

            // Adjusted cooldown
            int adjustedCooldown = (int)(RECOIL_COOLDOWN * fpsFactor);

            if (isPressed && Game.GameTime - lastRecoilTime >= adjustedCooldown && GameplayCamera.RelativePitch < 25.6f)
            {
                recoilValue = Utils.RecoilVal;
                if (!isMinigun)
                {
                    MC.Task.PlayAnimation(Utils.AimAnim, AnimationBlendDelta.SlowBlendIn, new AnimationBlendDelta(-1f), -1, AnimationFlags.Secondary | (AnimationFlags)48 | AnimationFlags.Loop, Utils.MapPitchToPhase(animPitchSource, 0f, Utils.GetAimAnimSpeedByClipAsset()));
                }

                lastRecoilTime = Game.GameTime;
            }
        }

        private void PedKeeper(Ped ped)
        {
            if (!Utils.DoesExists(ped))
                return;
            Vector3 gunGrip = ped.Weapons.CurrentWeaponObject.Bones["gun_gripr"].GetPositionOffset(ped.Weapons.CurrentWeaponObject.Position);
            if (ped == ShooterL && ped.Weapons.CurrentWeaponObject.IsAttachedTo(ped))
            {
                ped.Weapons.CurrentWeaponObject.AttachTo(MC.Bones[Bone.SkelLeftHand], gunGrip + AimPosL, AimRotL, false, false, false, true, default);
            }
            else if (ped == ShooterR && ped.Weapons.CurrentWeaponObject.IsAttachedTo(ped))
            {
                ped.Weapons.CurrentWeaponObject.AttachTo(MC.Bones[Bone.SkelRightHand], gunGrip + AimPosR, AimRotR, false, false, false, true, default);
            }

            if (ped.BlockPermanentEvents == false)
                ped.BlockPermanentEvents = true;
            if (!ped.IsBulletProof || ped.IsCollisionProof)
                Function.Call(Hash.SET_ENTITY_PROOFS, ped, true, true, true, true, true, true, true, true);
            if (ped.IsAmbientSpeechEnabled)
                Function.Call(Hash.STOP_PED_SPEAKING, ped, true);
            if (ped.GetCombatFloatAttribute(CombatFloatAttributes.WeaponDamageModifier) != Config.dmg)
                ped.SetCombatFloatAttribute(CombatFloatAttributes.WeaponDamageModifier, Config.dmg);
            if (ped.CanRagdoll)
                ped.CanRagdoll = false;
            if (ped.IsCollisionEnabled)
                ped.IsCollisionEnabled = false;
            if (!ped.IsPositionFrozen)
                ped.IsPositionFrozen = true;

            Utils.RequestGunHD(GunL);
            Utils.RequestGunHD(GunR);
        }
    }
}
