/*
 * ii's Stupid Menu  Patches/PatchHandler.cs
 * A mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Goldentrophy Software
 * https://github.com/iiDk-the-actual/iis.Stupid.Menu
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using HarmonyLib;
using iiMenu.Managers;
using System;
using System.Linq;
using System.Reflection;

namespace iiMenu.Patches
{
    public class PatchHandler
    {
        public static bool IsPatched { get; private set; }
        public static int PatchErrors { get; private set; }

        public static void PatchAll()
        {
            if (IsPatched) return;
            instance ??= new Harmony(PluginInfo.GUID);

            // Enumerating types can itself fail once the game drifts; keep whatever
            // loaded rather than losing the whole menu.
            Type[] types;
            try
            {
                types = Assembly.GetExecutingAssembly().GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
                LogManager.LogError($"Some types failed to load: {ex}");
            }

            // The HarmonyPatch attribute probe must stay *inside* the try. Reading the
            // attribute forces resolution of its typeof(...) target, so a game type that
            // was renamed or removed throws here -- and if that throw escapes the loop,
            // PatchAll() dies before the menu UI is ever created and the menu vanishes
            // with no in-game indication. Guarded, a missing type costs one patch.
            foreach (var type in types)
            {
                try
                {
                    if (!type.IsClass || type.GetCustomAttribute<HarmonyPatch>() == null)
                        continue;

                    instance.CreateClassProcessor(type).Patch();
                }
                catch (Exception ex)
                {
                    PatchErrors++;
                    LogManager.LogError($"Failed to patch {type.FullName}: {ex}");
                }
            }

            LogManager.Log($"Patched with {PatchErrors} errors");

            IsPatched = true;
        }

        public static void UnpatchAll()
        {
            if (instance == null || !IsPatched) return;
            instance.UnpatchSelf();
            IsPatched = false;
            instance = null;
        }

        public static void ApplyPatch(Type targetClass, string methodName, MethodInfo prefix = null, MethodInfo postfix = null, Type[] parameterTypes = null)
        {
            var original =
                (parameterTypes == null ?
                targetClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) :
                targetClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, parameterTypes, null)) ?? throw new Exception($"Method '{methodName}' not found on {targetClass.FullName}");
            instance.Patch(original,
                prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                postfix: postfix != null ? new HarmonyMethod(postfix) : null);
        }

        public static void RemovePatch(Type targetClass, string methodName, Type[] parameterTypes = null)
        {
            var original =
                (parameterTypes == null ?
                targetClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static) :
                targetClass.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, parameterTypes, null)) ?? throw new Exception($"Method '{methodName}' not found on {targetClass.FullName}");
            instance.Unpatch(original, HarmonyPatchType.All, instance.Id);
        }

        private static Harmony instance;
        public const string InstanceId = PluginInfo.GUID;
    }
}