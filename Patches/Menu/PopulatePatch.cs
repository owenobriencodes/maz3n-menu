/*
 * ii's Stupid Menu  Patches/Menu/PopulatePatch.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace iiMenu.Patches.Menu
{
    [HarmonyPatch]
    public class PopulatePatch
    {
        public static bool enabled;

        // The 2026-06-26 VIM update added a Populate(Friend, bool isVimSlot)
        // overload alongside Populate(Friend), which made a name-only patch
        // target ambiguous. Target every overload so another one appearing
        // does not break this again.
        [HarmonyTargetMethods]
        private static IEnumerable<MethodBase> TargetMethods() =>
            AccessTools.GetDeclaredMethods(typeof(FriendCard))
                .Where(method => method.Name == nameof(FriendCard.Populate))
                .Cast<MethodBase>();

        [HarmonyPostfix]
        public static void Postfix(FriendCard __instance, FriendBackendController.Friend friend)
        {
            if (enabled)
            {
                bool custom = friend.Presence.RoomId[0] == '@';

                __instance.SetRoom((custom ? friend.Presence.RoomId[1..] : friend.Presence.RoomId).ToUpper());
                __instance.SetZone((custom ? "CUSTOM" : friend.Presence.Zone).ToUpper());
                __instance.joinable = true;

                __instance.UpdateComponentStates();
            }
        }
    }
}
