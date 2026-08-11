/*
 * ii's Stupid Menu  PluginInfo.cs
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

namespace iiMenu
{
    public class PluginInfo
    {
        public const string GUID = "org.maz3n.gorillatag.maz3nmenu";
        public const string Name = "MAZ3N Menu";
        public const string Description = "MAZ3N Menu -- a fork of ii's Stupid Menu by @crimsoncauldron";
        public const string BuildTimestamp = "2026-08-11T14:27:15Z";
        // Bumped from 8.2.4 to stop the false "update to 8.3.0" nag. ServerData compares
        // this against the discontinued upstream server, which still reports 8.3.0; its
        // self-updater pulls from the deleted upstream repo and would overwrite this
        // working build with one that no longer runs. This fork is past that release.
        public const string Version = "8.3.0";

        public const string BaseDirectory = "MAZ3N Menu";
        public const string ClientResourcePath = "iiMenu.Resources.Client";
        // Repointed from iiDk-the-actual/iis.Stupid.Menu (deleted, every fetch 404'd)
        // to this fork, which carries its own copy of Resources/Server. All ~100
        // runtime resource fetches -- menu/notification/achievement audio, icons,
        // PluginLibrary.txt, soundboard list -- resolve from here.
        public const string ServerResourcePath = "https://raw.githubusercontent.com/owenobriencodes/maz3n-menu/master/Resources/Server";
        public const string ServerAPI = "https://iidk.online"; // Server now closed source due to bad actors :( For any questions, please make an issue on the GitHub repository.
        
        public const string Logo = @"███╗   ███╗ █████╗ ███████╗██████╗ ███╗   ██╗
████╗ ████║██╔══██╗╚══███╔╝╚════██╗████╗  ██║
██╔████╔██║███████║  ███╔╝  █████╔╝██╔██╗ ██║
██║╚██╔╝██║██╔══██║ ███╔╝   ╚═══██╗██║╚██╗██║
██║ ╚═╝ ██║██║  ██║███████╗██████╔╝██║ ╚████║
╚═╝     ╚═╝╚═╝  ╚═╝╚══════╝╚═════╝ ╚═╝  ╚═══╝";

#if DEBUG
        public static bool BetaBuild = true;
#else
        public static bool BetaBuild = false;
#endif
    }
}
