using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Runtime.InteropServices;

namespace DualWield
{
    public static class FPV
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("DismembermentASI.asi", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void AddBoneDraw(int handle, int start, int end);

        [DllImport("DismembermentASI.asi", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Auto)]
        private static extern void RemoveBoneDraw(int handle);

        private static bool IsDismembermentLoaded => GetModuleHandle("DismembermentASI.asi") != IntPtr.Zero;

        //Borrowed DismembermentASI to hide player head - thanks CamxxCore!
        private static void SafeAddBoneDraw(int handle, int start, int end)
        {
            if (IsDismembermentLoaded)
            {
                AddBoneDraw(handle, start, end);
            }
            else
            {
                Notification.PostTicker("~r~DismembermentASI.asi could not be found, ~w~~n~check Dual Wield Reboot installation. First-Person cam requires this to work properly", true);
            }
        }

        private static void SafeRemoveBoneDraw(int handle)
        {
            if (IsDismembermentLoaded)
            {
                RemoveBoneDraw(handle);
            }
        }

        public static Camera Camera;
        public static DateTime Timer;
        public static bool Active = false;

        public static void Create()
        {
            Camera = Camera.Create(ScriptedCameraNameHash.DefaultScriptedCamera,
                Main.MC.Bones[Bone.SkelHead].Position + Main.MC.ForwardVector + Main.MC.UpVector, Main.MC.Rotation, Config.fov, true);

            if (Main.MC.Bones[Bone.FacialTongueA].IsValid) //Attaching on tongue = better POV, not all addonpeds have tongue
                Camera.AttachTo(Main.MC.Bones[Bone.FacialTongueA], new Vector3(0f, -0.05f, 0.15f));
            else Camera.AttachTo(Main.MC.Bones[Bone.SkelHead], new Vector3(0f, 0f, 0.15f));

            SafeAddBoneDraw(Main.MC.Handle, (int)Bone.SkelHead, -1);
            // want to add SET_ENTITY_FLAG_SUPPRESS_SHADOW to remove headless shadow caused by dismemberment
            // but disabling the weapon object shadows doesn't include its attachments, why R* whhyyy???

            Function.Call(Hash.SET_CAM_NEAR_CLIP, Camera.Handle, 0.1f);
            ScriptCameraDirector.StartRendering();
            Timer = DateTime.Now;
            Active = true;
        }

        public static void Destroy()
        {
            if (!Active) return;
            
            if (Camera != null)
            {
                Camera.IsActive = false;
                Camera.Delete();
                Camera = null;
            }
            ScriptCameraDirector.StopRendering();
            SafeRemoveBoneDraw(Main.MC.Handle);
            Active = false;
        }

        public static void Update()
        {
            if (Camera == null)
                return;

            float pitch = GameplayCamera.Rotation.X;
            float yaw = GameplayCamera.Rotation.Z;

            // Clamp pitch (X axis)
            pitch = Utils.Clamp(pitch, -45f, 40f);

            float playerYaw = Main.MC.Rotation.Z;
            float deltaYaw = NormalizeAngle(yaw - playerYaw);
            float clampedDeltaYaw = Utils.Clamp(deltaYaw, -60f, 60f);
            float finalYaw = playerYaw + clampedDeltaYaw;

            Camera.Rotation = new Vector3(pitch, 0f, finalYaw);

            Game.EnableAllControlsThisFrame();
            Function.Call(Hash.DISABLE_ON_FOOT_FIRST_PERSON_VIEW_THIS_UPDATE);

            if (Game.GetControlValueNormalized(Control.ScriptLeftAxisX) == 0)
                Main.MC.Heading = GameplayCamera.Rotation.Z;

            if (Main.MC.IsAiming && ScriptCameraDirector.RenderingCam == Camera)
            {
                Hud.ShowComponentThisFrame(HudComponent.Reticle);
                Function.Call(Hash.SET_HUD_COMPONENT_POSITION, 14, 0f, 0f);
            }
        }

        public static float NormalizeAngle(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }
    }
}
