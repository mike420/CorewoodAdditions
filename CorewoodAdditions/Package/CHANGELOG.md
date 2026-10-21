<table>
<thead>
  <tr>
    <th>Version</th>
    <th>Changes</th>
  </tr>
</thead>
<tbody>
  <tr>
    <td>1.0.8</td>
    <td>
    <li>Fixed the Beehive, Birdhouse and Compost Bin losing part of a batch, and the items used to make it, when their container had no empty slot and the output stack had room for only some of it. They now wait until the whole batch fits.</li>
    <li>The Compost Bin now carries on making other baits when one bait type has no room left, instead of stopping as "Inventory full".</li>
    <li>Renaming both portals with Tag Both Portals Key is more reliable when the other portal is far away.</li>
    </td>
  </tr>
  <tr>
    <td>1.0.7</td>
    <td>
    <li>Added Magic Mike's Mod Settings: press F7, in game or on the main menu, to open a panel with the settings of all your installed Magic Mike mods. Change them with sliders, toggles, dropdowns and key buttons, search them, and reset them to their defaults; changes are saved when the panel closes. Server-synced settings can only be changed by an admin. The key and scroll speed can be changed in the panel's own Mod Settings Panel tab.</li>
    <li>The HD Smelter can now pull coal and ore lying on the ground near its inputs, one of each per second while it has room (Pull Items From Ground, on by default). Pull Radius sets how far it reaches (4 metres by default).</li>
    </td>
  </tr>
  <tr>
    <td>1.0.6</td>
    <td>
    <li>Added a HD Mead Ketill that uses all the vanilla mead ketill recipes. Like the vanilla one it needs a fire underneath it to craft.</li>
    <li>Added config options for the HD Mead Ketill's sound volume (local setting), discover range and build range.</li>
    <li>Added a config option to turn off the HD Mead Ketill with Enable Mead Ketill. When off it is hidden from the build menu, and placed ones cannot be used to craft.</li>
    <li>Added a HD Frost Kiln that burns Ice to make Frozen Fuel, like the vanilla frost kiln. It has its own config section (max ice, ice per product, seconds per product, smoke) and can be turned off with Enable Frost Kiln.</li>
    </td>
  </tr>
  <tr>
    <td>1.0.5</td>
    <td>
    <li>Added a HD Cooking Station that uses all the vanilla cooking station recipes. An icon shows above it while it holds food ready to take off, with a config option to turn it off.</li>
    <li>Added a config option to speed up or slow down cooking on the HD Cooking Station (Cook Time Multiplier).</li>
    <li>Added a config option to stop food burning on the HD Cooking Station (Can Overcook Items).</li>
    <li>Added a config option for the HD Cooking Station to drop cooked food onto the ground as soon as it is ready, so it never burns (Auto Drop Cooked Food, off by default). While it is on, the output icon is not shown, as food doesn't stay on the station.</li>
    <li>Added a config option to turn off the HD Cooking Station with Enable Cooking Station. When off it is hidden from the build menu, and placed ones stop cooking and cannot be filled (cooked food can still be taken off).</li>
    <li>Added Sound Volume config options for the Beehive, Birdhouse, Wisp Fountain and HD Portal (0 to 1, local settings).</li>
    <li>Fixed the Large Fish Trap being hard to place and causing stutter while selected with the Hammer, and a freeze when placing it.</li>
    <li>Fish traps can no longer be built inside one another.</li>
    <li>Improved performance of the Compost Bin, Beehive, Birdhouse and fish traps: they no longer save their timer every second, which made their storage reload every second for everyone nearby.</li>
    <li>Fixed a "Can't unregister renderer" error for fish traps after a placement had been refused.</li>
    <li>Fixed teleporting through a HD Portal sometimes getting stuck on the loading screen when QuickTeleport is installed. Fast Teleport now turns itself off while QuickTeleport is installed, as that already speeds up every portal.</li>
    </td>
  </tr>
  <tr>
    <td>1.0.4</td>
    <td>
    <li>Added the Wisp Fountain. It lures wisps at night like the vanilla one and collects them into its own storage, so you no longer have to pick them by hand. An icon shows above it and it plays a sound while it holds wisps, with config options to turn each off.</li>
    <li>Added config options to turn off the Wisp Fountain's wisp collecting and to set the size of its storage (columns and rows).</li>
    <li>Added a HD Windmill that grinds Barley into Barley Flour using the wind, like the vanilla windmill. It has its own config section (max barley, seconds per product) and can be turned off with Enable Windmill.</li>
    <li>Added a HD Fermenter that uses all the vanilla fermenter recipes. An icon shows above it while it is ready to tap. It has its own config section (fermentation duration, show output icon) and can be turned off with Enable Fermenter, which pauses fermenting rather than losing progress.</li>
    <li>Added map pins for HD Portals, labelled with each portal's tag, with a config option to turn them off.</li>
    <li>HD Portals now teleport fast: a short fade, and you arrive as soon as the destination has loaded instead of after the vanilla 8 seconds. It has config options to turn it off and to set the fade time and safety delay.</li>
    <li>Delete and rebuild your config file if you have any issues.</li>
    </td>
  </tr>
  <tr>
    <td>1.0.3</td>
    <td>
    <li>Added a HD Smelter that uses all the vanilla smelter recipes.</li>
    <li>Added a HD Charcoal Kiln that uses the vanilla charcoal kiln recipes and needs no fuel.</li>
    <li>Added config sections for the HD Smelter (max ore, max fuel, fuel per product, seconds per product) and HD Charcoal Kiln (max wood, seconds per product).</li>
    <li>Added a config option to enable or disable the HD Smelter and HD Charcoal Kiln. When off they are hidden from the build menu, and placed ones stop working and cannot be filled.</li>
    <li>Added an Enable Smoke option for the HD Smelter and HD Charcoal Kiln.</li>
    <li>Added the HD Portal. It links by tag like the vanilla portal, including with vanilla portals.</li>
    <li>HD Portals show their name above them, with options for distance and line of sight, and a hotkey (Page Up) to show or hide the names.</li>
    <li>Hold Left Alt and press Use on a connected HD Portal to rename it and the portal it links to in one go.</li>
    <li>Added config options to enable or disable the HD Portal, to let it carry all items (including ore), and to turn off its sound.</li>
    <li>Delete and rebuild your config file if you have any issues.</li>
    </td>
  </tr>
  <tr>
    <td>1.0.2</td>
    <td>
    <li>Updated Beehive visuals.</li>
    <li>Added the Large Fish Trap, with its own config section.</li>
    <li>Added the Ocean Fish Trap. Build it from a boat on deep water in the Ocean biome; it floats and bobs with the waves. It has its own config section.</li>
    <li>Added Serpent Bait. The Compost Bin makes it from Rotten Meat, and the Ocean Fish Trap uses it to catch Serpent Meat. It can be turned off in the config.</li>
    <li>Added map pins for all fish traps, with a config option to turn them off.</li>
    <li>Added a rotating output icon to every producer that shows while it holds produced items, with a config option per piece to hide it.</li>
    <li>Fixed producer visuals and sounds flickering or restarting every second.</li>
    </td>
  </tr>
  <tr>
    <td>1.0.1</td>
    <td>
    <li>Adjusted the size of the Compost Bin and Beehive to make them more snappoint friendly.</li>
    <li>Added config options to disable sounds.
    </td>
  </tr>
  <tr>
    <td>1.0.0</td>
    <td>
    <li>Initial release.</li>
    </td>
  </tr>
</tbody>
</table>
