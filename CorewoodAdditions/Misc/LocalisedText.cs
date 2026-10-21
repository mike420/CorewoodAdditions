using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace CorewoodAdditions;

internal class LocalisedText
{
	public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

	public static void AddLocalisations()
	{
		Localization.AddTranslation("English", new Dictionary<string, string>
		{
			{ "piece_mm_corewood_compost_bin", "Compost Bin" },
            { "piece_mm_corewood_compost_bin_desc", "Turns food into fishing bait over time. The bait type depends on the item supplied." },
            { "text_compostbin_working", "Producing bait" },
            { "text_compostbin_no_consumable", "No consumable item" },
            { "text_compostbin_full", "Inventory full" },
            { "piece_mm_corewood_beehive", "Beehive" },
            { "piece_mm_corewood_beehive_desc", "Produces honey over time when supplied with the configured item." },
            { "text_beehive_working", "Producing honey" },
            { "text_beehive_no_consumable", "No consumable item" },
            { "text_beehive_full", "Inventory full" },
            { "piece_mm_corewood_fishtrap", "Fish Trap" },
            { "piece_mm_corewood_fishtrap_desc", "Catches fish over time when placed in shallow water (0.5 m to 1.5 m deep) and supplied with bait." },
            { "piece_mm_corewood_fishtrap_large", "Large Fish Trap" },
            { "piece_mm_corewood_fishtrap_large_desc", "A bigger fish trap that catches fish over time when placed in shallow water (0.5 m to 1.5 m deep) and supplied with bait." },
            { "piece_mm_corewood_fishtrap_ocean", "Ocean Fish Trap" },
            { "piece_mm_corewood_fishtrap_ocean_desc", "Floats on the sea and catches fish over time when supplied with bait. Build it from a boat on deep water in the Ocean biome." },
            { "text_fishtrap_invalid_water", "Must be placed in shallow water." },
            { "text_fishtrap_invalid_ocean", "Must be placed on deep water in the Ocean." },
            { "text_fishtrap_too_close", "Too close to another fish trap." },
            { "text_fishtrap_working", "Fishing" },
            { "text_fishtrap_wrong_biome", "No fish compatible with this biome" },
            { "text_fishtrap_no_bait", "No valid bait found" },
            { "text_fishtrap_full", "Inventory full" },
            { "item_serpentbait", "Serpent Bait" },
            { "item_serpentbait_desc", "Bait for catching sea serpents. Use it in an Ocean Fish Trap to catch Serpent Meat." },
            { "piece_mm_corewood_birdhouse", "Birdhouse" },
            { "piece_mm_corewood_birdhouse_desc", "Produces feathers over time when supplied with the configured items." },
            { "text_birdhouse_working", "Producing feathers" },
            { "text_birdhouse_no_consumable", "No consumable item" },
            { "text_birdhouse_full", "Inventory full" },
            { "msg_corewood_station_disabled", "This station is disabled." },
            { "msg_corewood_portal_disabled", "This portal is disabled." },
            { "msg_corewood_portal_names_shown", "Portal names shown" },
            { "msg_corewood_portal_names_hidden", "Portal names hidden" },
            { "msg_corewood_portal_tag_both", "Tag both portals" },
            { "msg_corewood_portal_partner_missing", "The connected portal could not be found, only this portal was renamed." }
        });
	}
}
