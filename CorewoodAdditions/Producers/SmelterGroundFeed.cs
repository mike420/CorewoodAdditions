using System.Collections.Generic;
using UnityEngine;

namespace CorewoodAdditions
{
    // Lets the HD Smelter pull coal and ore lying on the ground near its intakes, one of each per smelter update.
    internal static class SmelterGroundFeed
    {
        private static readonly Collider[] Hits = new Collider[128];
        private static readonly List<ItemDrop> Drops = new List<ItemDrop>();
        private static int s_itemMask;

        internal static void OnUpdateSmelter(Smelter smelter)
        {
            if (!MainConfig.smelterPullFromGround.Value || !MainConfig.smelterKilnEnabled.Value)
            {
                return;
            }
            if (smelter == null || smelter.m_nview == null || !smelter.m_nview.IsValid() || !smelter.m_nview.IsOwner())
            {
                return;
            }
            if (Utils.GetPrefabName(smelter.gameObject) != AddPrefabs.SmelterNewPrefabName)
            {
                return;
            }
            // Same full checks as vanilla OnAddFuel and OnAddOre.
            bool wantFuel = smelter.m_fuelItem != null && smelter.GetFuel() <= smelter.m_maxFuel - 1;
            bool wantOre = smelter.GetQueueSize() < smelter.m_maxOre;
            if (!wantFuel && !wantOre)
            {
                return;
            }
            FindDrops(smelter);
            string fuelName = smelter.m_fuelItem != null ? smelter.m_fuelItem.m_itemData.m_shared.m_name : null;
            foreach (ItemDrop drop in Drops)
            {
                if (!wantFuel && !wantOre)
                {
                    break;
                }
                bool isFuel = wantFuel && drop.m_itemData.m_shared.m_name == fuelName;
                string prefabName = Utils.GetPrefabName(drop.gameObject);
                bool isOre = !isFuel && wantOre && smelter.IsItemAllowed(prefabName);
                if (!isFuel && !isOre)
                {
                    continue;
                }
                // Only the drop's owner can change its stack, so take ownership and pick it up on a later update.
                if (!drop.m_nview.IsOwner())
                {
                    drop.RequestOwn();
                    continue;
                }
                bool cheated = drop.m_itemData.m_cheated;
                ConsumeOne(drop);
                if (isFuel)
                {
                    smelter.m_nview.InvokeRPC("RPC_AddFuel");
                    wantFuel = false;
                }
                else
                {
                    smelter.m_nview.InvokeRPC("RPC_AddOre", prefabName, cheated);
                    wantOre = false;
                }
            }
            Drops.Clear();
        }

        private static void FindDrops(Smelter smelter)
        {
            Drops.Clear();
            if (s_itemMask == 0)
            {
                s_itemMask = LayerMask.GetMask("item");
            }
            float radius = Mathf.Max(0f, MainConfig.smelterPullRadius.Value);
            AddDropsNear(smelter.m_addOreSwitch != null ? smelter.m_addOreSwitch.transform.position : smelter.transform.position, radius);
            if (smelter.m_addWoodSwitch != null)
            {
                AddDropsNear(smelter.m_addWoodSwitch.transform.position, radius);
            }
        }

        private static void AddDropsNear(Vector3 center, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(center, radius, Hits, s_itemMask);
            for (int i = 0; i < count; i++)
            {
                Collider hit = Hits[i];
                ItemDrop drop = hit.attachedRigidbody != null ? hit.attachedRigidbody.GetComponent<ItemDrop>() : hit.GetComponentInParent<ItemDrop>();
                if (drop != null && drop.m_nview != null && drop.m_nview.IsValid() && drop.m_itemData?.m_shared != null && !Drops.Contains(drop))
                {
                    Drops.Add(drop);
                }
            }
        }

        private static void ConsumeOne(ItemDrop drop)
        {
            if (drop.m_itemData.m_stack <= 1)
            {
                drop.m_nview.Destroy();
            }
            else
            {
                drop.SetStack(drop.m_itemData.m_stack - 1);
            }
        }
    }
}
