using FistVR;
using H3MP.Networking;
using H3MP.Tracking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace H3MP.Patches
{
    public class InteractionPatches
    {
        public static void DoPatching(Harmony harmony, ref int patchIndex)
        {
            // HandCurrentInteractableSetPatch
            MethodInfo handCurrentInteractableSetPatchOriginal = typeof(FVRViveHand).GetMethod("set_CurrentInteractable", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handCurrentInteractableSetPatchPrefix = typeof(HandCurrentInteractableSetPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo handCurrentInteractableSetPatchPostfix = typeof(HandCurrentInteractableSetPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(handCurrentInteractableSetPatchOriginal, harmony, true);
            harmony.Patch(handCurrentInteractableSetPatchOriginal, new HarmonyMethod(handCurrentInteractableSetPatchPrefix), new HarmonyMethod(handCurrentInteractableSetPatchPostfix));

            ++patchIndex; // 1

            // SetQuickBeltSlotPatch
            MethodInfo setQuickBeltSlotPatchOriginal = typeof(FVRPhysicalObject).GetMethod("SetQuickBeltSlot", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setQuickBeltSlotPatchPostfix = typeof(SetQuickBeltSlotPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(setQuickBeltSlotPatchOriginal, harmony, true);
            harmony.Patch(setQuickBeltSlotPatchOriginal, null, new HarmonyMethod(setQuickBeltSlotPatchPostfix));

            ++patchIndex; // 2

            // SosigPickUpPatch
            MethodInfo sosigPickUpPatchOriginal = typeof(SosigHand).GetMethod("PickUp", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigPickUpPatchPostfix = typeof(SosigPickUpPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigPickUpPatchOriginal, harmony, true);
            harmony.Patch(sosigPickUpPatchOriginal, null, new HarmonyMethod(sosigPickUpPatchPostfix));

            ++patchIndex; // 3

            // SosigHandDropPatch
            MethodInfo sosigHandDropPatchOriginal = typeof(SosigHand).GetMethod("DropHeldObject", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigHandThrowPatchOriginal = typeof(SosigHand).GetMethod("ThrowObject", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigHandDropPatchPrefix = typeof(SosigHandDropPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigHandDropPatchOriginal, harmony, true);
            PatchController.Verify(sosigHandThrowPatchOriginal, harmony, true);
            harmony.Patch(sosigHandDropPatchOriginal, new HarmonyMethod(sosigHandDropPatchPrefix));
            harmony.Patch(sosigHandThrowPatchOriginal, new HarmonyMethod(sosigHandDropPatchPrefix));

            ++patchIndex; // 4

            // SosigPlaceObjectInPatch
            MethodInfo sosigPutObjectInPatchOriginal = typeof(SosigInventory.Slot).GetMethod("PlaceObjectIn", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigPutObjectInPatchPostfix = typeof(SosigPlaceObjectInPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigPutObjectInPatchOriginal, harmony, true);
            harmony.Patch(sosigPutObjectInPatchOriginal, null, new HarmonyMethod(sosigPutObjectInPatchPostfix));

            ++patchIndex; // 5

            // SosigSlotDetachPatch
            MethodInfo sosigSlotDetachPatchOriginal = typeof(SosigInventory.Slot).GetMethod("DetachHeldObject", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigSlotDetachPatchPrefix = typeof(SosigSlotDetachPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(sosigSlotDetachPatchOriginal, harmony, true);
            harmony.Patch(sosigSlotDetachPatchOriginal, new HarmonyMethod(sosigSlotDetachPatchPrefix));

            ++patchIndex; // 6

            // GrabbityPatch
            MethodInfo grabbityPatchFlickOriginal = typeof(FVRViveHand).GetMethod("BeginFlick", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo grabbityPatchFlickPrefix = typeof(GrabbityPatch).GetMethod("FlickPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo grabbityPatchFindHoverOriginal = typeof(FVRViveHand).GetMethod("CastToFindHover", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo grabbityPatchFindHoverPrefix = typeof(GrabbityPatch).GetMethod("CastToFindHoverPrefix", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(grabbityPatchFlickOriginal, harmony, false);
            PatchController.Verify(grabbityPatchFindHoverOriginal, harmony, false);
            harmony.Patch(grabbityPatchFlickOriginal, new HarmonyMethod(grabbityPatchFlickPrefix));
            harmony.Patch(grabbityPatchFindHoverOriginal, new HarmonyMethod(grabbityPatchFindHoverPrefix));

            ++patchIndex; // 7

            // GBeamerPatch
            MethodInfo GBeamerPatchObjectSearchOriginal = typeof(GBeamer).GetMethod("ObjectSearch", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo GBeamerPatchObjectSearchPrefix = typeof(GBeamerPatch).GetMethod("ObjectSearchPrefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo GBeamerPatchObjectSearchPostfix = typeof(GBeamerPatch).GetMethod("ObjectSearchPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo GBeamerPatchWideShuntOriginal = typeof(GBeamer).GetMethod("WideShunt", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo GBeamerPatchWideShuntTranspiler = typeof(GBeamerPatch).GetMethod("WideShuntTranspiler", BindingFlags.NonPublic | BindingFlags.Static);

            PatchController.Verify(GBeamerPatchObjectSearchOriginal, harmony, false);
            PatchController.Verify(GBeamerPatchWideShuntOriginal, harmony, false);
            harmony.Patch(GBeamerPatchObjectSearchOriginal, new HarmonyMethod(GBeamerPatchObjectSearchPrefix), new HarmonyMethod(GBeamerPatchObjectSearchPostfix));
            try 
            { 
                harmony.Patch(GBeamerPatchWideShuntOriginal, null, null, new HarmonyMethod(GBeamerPatchWideShuntTranspiler));
            }
            catch (Exception ex)
            {
                Mod.LogError("Exception caught applying InteractionPatches.GBeamerPatch: " + ex.Message + ":\n" + ex.StackTrace);
            }

            ++patchIndex; // 8
        }
    }

    // Patches FVRViveHand.CurrentInteractable.set to keep track of item held
    class HandCurrentInteractableSetPatch
    {
        static FVRInteractiveObject preObject;

        static void Prefix(ref FVRViveHand __instance, ref FVRInteractiveObject ___m_currentInteractable)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            preObject = ___m_currentInteractable;
        }

        static void Postfix(ref FVRViveHand __instance, ref FVRInteractiveObject ___m_currentInteractable)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (___m_currentInteractable != null)
            {
                if (preObject != ___m_currentInteractable)
                {
                    if(GameManager.trackedObjectByInteractive.TryGetValue(___m_currentInteractable, out TrackedObject trackedObject))
                    {
                        trackedObject.BeginInteraction(__instance);
                    }
                }
            }
            else // ___m_currentInteractable == null
            {
                if (preObject != null) // Dropped preObject
                {
                    if (GameManager.trackedObjectByInteractive.TryGetValue(preObject, out TrackedObject trackedObject))
                    {
                        trackedObject.EndInteraction(__instance);
                    }
                }
            }
        }
    }

    // Patches FVRPhysicalObject.SetQuickBeltSlot so we can keep track of item control
    class SetQuickBeltSlotPatch
    {
        static void Postfix(ref FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (slot != null)
            {
                // Just put this item in a slot
                TrackedItem trackedItem = GameManager.trackedItemByItem.ContainsKey(__instance) ? GameManager.trackedItemByItem[__instance] : __instance.GetComponent<TrackedItem>();
                if (trackedItem != null && trackedItem.data.controller != GameManager.ID)
                {
                    // Take control

                    // Send to all clients
                    trackedItem.data.TakeControlRecursive();

                    // Update locally
                    Mod.SetKinematicRecursive(trackedItem.physical.transform, false);
                }
            }
        }
    }

    // Patches SosigHand.PickUp so we can keep track of item control and inventory
    class SosigPickUpPatch
    {
        public static int skip;

        static void Postfix(ref SosigHand __instance, SosigWeapon o)
        {
            if (Mod.managerObject == null || skip > 0)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.TryGetValue(o, out trackedItem) ? trackedItem : o.GetComponent<TrackedItem>();
            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.TryGetValue(__instance.S, out trackedSosig) ? trackedSosig : __instance.S.GetComponent<TrackedSosig>();
            if (trackedItem != null && trackedSosig != null)
            {
                bool primaryHand = __instance == __instance.S.Hand_Primary;

                trackedItem.BeginInteraction(null);

                if (ThreadManager.host)
                {
                    trackedSosig.sosigData.inventory[primaryHand ? 0 : 1] = trackedItem.data.trackedID;

                    ServerSend.SosigPickUpItem(trackedSosig.data.trackedID, trackedItem.data.trackedID, primaryHand);
                }
                else
                {
                    // If we don't have item tracked ID
                    //  Add to unknownSosigInventoryItems indicating that once this item ha tracked ID we should add it to given sosig's logical inventory 
                    if (trackedItem.data.trackedID == -1)
                    {
                        if (TrackedItem.unknownSosigInventoryItems.TryGetValue(trackedItem.data.localWaitingIndex, out KeyValuePair<TrackedSosigData, int> entry))
                        {
                            TrackedItem.unknownSosigInventoryItems[trackedItem.data.localWaitingIndex] = new KeyValuePair<TrackedSosigData, int>(trackedSosig.sosigData, primaryHand ? 0 : 1);
                        }
                        else
                        {
                            TrackedItem.unknownSosigInventoryItems.Add(trackedItem.data.localWaitingIndex, new KeyValuePair<TrackedSosigData, int>(trackedSosig.sosigData, primaryHand ? 0 : 1));
                        }
                    }

                    // If we don't have Sosig tracked ID
                    //  Add to unknownItemInteract indicating that once we receive Sosig tracked ID we should add the item to its logical inventory
                    if (trackedSosig.data.trackedID == -1)
                    {
                        if (TrackedSosig.unknownItemInteract.ContainsKey(trackedSosig.data.localWaitingIndex))
                        {
                            TrackedSosig.unknownItemInteract[trackedSosig.data.localWaitingIndex].Add(new KeyValuePair<int, KeyValuePair<TrackedItemData, int>>(0, new KeyValuePair<TrackedItemData, int>(trackedItem.itemData, primaryHand ? 0 : 1)));
                        }
                        else
                        {
                            TrackedSosig.unknownItemInteract.Add(trackedSosig.data.localWaitingIndex, new List<KeyValuePair<int, KeyValuePair<TrackedItemData, int>>>() { new KeyValuePair<int, KeyValuePair<TrackedItemData, int>>(0, new KeyValuePair<TrackedItemData, int>(trackedItem.itemData, primaryHand ? 0 : 1)) });
                        }
                    }

                    // If we have Item and Sosig tracked ID, send order
                    if (trackedItem.data.trackedID != -1 && trackedSosig.data.trackedID != -1)
                    {
                        trackedSosig.sosigData.inventory[primaryHand ? 0 : 1] = trackedItem.data.trackedID;
                        ClientSend.SosigPickUpItem(trackedSosig, trackedItem.data.trackedID, primaryHand);
                    }
                }
            }
        }
    }

    // Patches SosigHand.DropHeldObject AND SosigHand.ThrowObject so we can keep track of item control
    class SosigHandDropPatch
    {
        public static int skip;

        static void Prefix(ref SosigHand __instance)
        {
            if (skip > 0)
            {
                return;
            }

            if (Mod.managerObject == null || !__instance.IsHoldingObject)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance.S) ? GameManager.trackedSosigBySosig[__instance.S] : __instance.S.GetComponent<TrackedSosig>();
            if (trackedSosig != null)
            {
                int handIndex = __instance == __instance.S.Hand_Primary ? 0 : 1;
                trackedSosig.sosigData.inventory[handIndex] = -1;
                Mod.LogInfo("Sosig " + trackedSosig.data.trackedID + " dropped item in hand: " + handIndex);

                if (ThreadManager.host)
                {
                    ServerSend.SosigHandDrop(trackedSosig.data.trackedID, __instance.S.Hand_Primary == __instance);
                }
                else
                {
                    if (trackedSosig.data.trackedID == -1)
                    {
                        // Check if we had previously added unknown to grab an item in the same hand
                        bool found = false;
                        if (TrackedSosig.unknownItemInteract.TryGetValue(trackedSosig.data.localWaitingIndex, out List<KeyValuePair<int, KeyValuePair<TrackedItemData, int>>> interactions))
                        {
                            for (int i = interactions.Count; i >= 0; --i)
                            {
                                // If the interaction is pickup of the same hand
                                if (interactions[i].Key == 0 && interactions[i].Value.Value == handIndex)
                                {
                                    found = true;

                                    // Remove from the unknown
                                    interactions.RemoveAt(i);

                                    if (interactions.Count == 0)
                                    {
                                        TrackedSosig.unknownItemInteract.Remove(trackedSosig.data.localWaitingIndex);
                                        break;
                                    }
                                }
                            }
                        }

                        if (!found)
                        {
                            // Haven't found an interaction to remove so will need to send this to others
                            if (TrackedSosig.unknownItemInteract.ContainsKey(trackedSosig.data.localWaitingIndex))
                            {
                                TrackedSosig.unknownItemInteract[trackedSosig.data.localWaitingIndex].Add(new KeyValuePair<int, KeyValuePair<TrackedItemData, int>>(3, new KeyValuePair<TrackedItemData, int>(null, handIndex)));
                            }
                            else
                            {
                                TrackedSosig.unknownItemInteract.Add(trackedSosig.data.localWaitingIndex, new List<KeyValuePair<int, KeyValuePair<TrackedItemData, int>>>() { new KeyValuePair<int, KeyValuePair<TrackedItemData, int>>(3, new KeyValuePair<TrackedItemData, int>(null, handIndex)) });
                            }
                        }
                    }
                    else
                    {
                        ClientSend.SosigHandDrop(trackedSosig.data.trackedID, __instance.S.Hand_Primary == __instance);
                    }
                }
            }
        }
    }

    // Patches SosigInventory.Slot.PlaceObjectIn so we can keep track of item control
    class SosigPlaceObjectInPatch
    {
        public static int skip;

        static void Postfix(ref SosigInventory.Slot __instance, SosigWeapon o)
        {
            if (Mod.managerObject == null || skip > 0)
            {
                return;
            }

            TrackedItem trackedItem = GameManager.trackedItemBySosigWeapon.TryGetValue(o, out trackedItem) ? trackedItem : o.GetComponent<TrackedItem>();
            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.TryGetValue(__instance.I.S, out trackedSosig) ? trackedSosig : __instance.I.S.GetComponent<TrackedSosig>();
            if (trackedItem != null && trackedSosig != null)
            {

                int slotIndex = 0;
                for (int i = 0; i < __instance.I.Slots.Count; ++i)
                {
                    if (__instance.I.Slots[i] == __instance)
                    {
                        slotIndex = i;
                        break;
                    }
                }

                trackedItem.BeginInteraction(null);

                if (ThreadManager.host)
                {
                    trackedSosig.sosigData.inventory[slotIndex + 2] = trackedItem.data.trackedID;

                    ServerSend.SosigPlaceItemIn(trackedSosig.data.trackedID, slotIndex, trackedItem.data.trackedID);
                }
                else
                {
                    // If we don't have item tracked ID
                    //  Add to unknownSosigInventoryItems indicating that once this item has tracked ID we should add it to given sosig's logical inventory 
                    if (trackedItem.data.trackedID == -1)
                    {
                        if (TrackedItem.unknownSosigInventoryItems.TryGetValue(trackedItem.data.localWaitingIndex, out KeyValuePair<TrackedSosigData, int> entry))
                        {
                            TrackedItem.unknownSosigInventoryItems[trackedItem.data.localWaitingIndex] = new KeyValuePair<TrackedSosigData, int>(trackedSosig.sosigData, slotIndex);
                        }
                        else
                        {
                            TrackedItem.unknownSosigInventoryItems.Add(trackedItem.data.localWaitingIndex, new KeyValuePair<TrackedSosigData, int>(trackedSosig.sosigData, slotIndex));
                        }
                    }

                    // If we don't have Sosig tracked ID
                    //  Add to unknownItemInteract indicating that once we receive Sosig tracked ID we should add the item to its logical inventory
                    if (trackedSosig.data.trackedID == -1)
                    {
                        if (TrackedSosig.unknownItemInteract.ContainsKey(trackedSosig.data.localWaitingIndex))
                        {
                            TrackedSosig.unknownItemInteract[trackedSosig.data.localWaitingIndex].Add(new KeyValuePair<int, KeyValuePair<TrackedItemData, int>>(1, new KeyValuePair<TrackedItemData, int>(trackedItem.itemData, slotIndex)));
                        }
                        else
                        {
                            TrackedSosig.unknownItemInteract.Add(trackedSosig.data.localWaitingIndex, new List<KeyValuePair<int, KeyValuePair<TrackedItemData, int>>>() { new KeyValuePair<int, KeyValuePair<TrackedItemData, int>>(1, new KeyValuePair<TrackedItemData, int>(trackedItem.itemData, slotIndex)) });
                        }
                    }

                    // If we have Item and Sosig tracked ID, send order
                    if (trackedItem.data.trackedID != -1 && trackedSosig.data.trackedID != -1)
                    {
                        trackedSosig.sosigData.inventory[slotIndex + 2] = trackedItem.data.trackedID;
                        ClientSend.SosigPlaceItemIn(trackedSosig.data.trackedID, slotIndex, trackedItem.data.trackedID);
                    }
                }
            }
        }
    }

    // Patches SosigInventory.Slot.DetachHeldObject so we can keep track of item control
    class SosigSlotDetachPatch
    {
        public static int skip;

        static void Prefix(ref SosigInventory.Slot __instance)
        {
            if (skip > 0)
            {
                return;
            }

            if (Mod.managerObject == null || !__instance.IsHoldingObject)
            {
                return;
            }

            TrackedSosig trackedSosig = GameManager.trackedSosigBySosig.ContainsKey(__instance.I.S) ? GameManager.trackedSosigBySosig[__instance.I.S] : __instance.I.S.GetComponent<TrackedSosig>();
            if (trackedSosig != null && trackedSosig.data.trackedID != -1)
            {
                int slotIndex = 0;
                for (int i = 0; i < __instance.I.Slots.Count; ++i)
                {
                    if (__instance.I.Slots[i] == __instance)
                    {
                        slotIndex = i;
                        break;
                    }
                }
                Mod.LogInfo("Sosig " + trackedSosig.data.trackedID + " drop item in slot: " + slotIndex);
                trackedSosig.sosigData.inventory[slotIndex + 2] = -1;

                if (ThreadManager.host)
                {
                    ServerSend.SosigDropSlot(trackedSosig.data.trackedID, slotIndex);
                }
                else
                {
                    if (trackedSosig.data.trackedID == -1)
                    {
                        bool found = false;
                        if (TrackedSosig.unknownItemInteract.TryGetValue(trackedSosig.data.localWaitingIndex, out List<KeyValuePair<int, KeyValuePair<TrackedItemData, int>>> interactions))
                        {
                            for (int i = interactions.Count; i >= 0; --i)
                            {
                                // If the interaction is pickup of the same slot
                                if (interactions[i].Key == 1 && interactions[i].Value.Value == slotIndex)
                                {
                                    found = true;

                                    // Remove from the unknown
                                    interactions.RemoveAt(i);

                                    if (interactions.Count == 0)
                                    {
                                        TrackedSosig.unknownItemInteract.Remove(trackedSosig.data.localWaitingIndex);
                                        break;
                                    }
                                }
                            }
                        }

                        if (!found)
                        {
                            // Haven't found an interaction to remove so will need to send this to others
                            if (TrackedSosig.unknownItemInteract.ContainsKey(trackedSosig.data.localWaitingIndex))
                            {
                                TrackedSosig.unknownItemInteract[trackedSosig.data.localWaitingIndex].Add(new KeyValuePair<int, KeyValuePair<TrackedItemData, int>>(2, new KeyValuePair<TrackedItemData, int>(null, slotIndex)));
                            }
                            else
                            {
                                TrackedSosig.unknownItemInteract.Add(trackedSosig.data.localWaitingIndex, new List<KeyValuePair<int, KeyValuePair<TrackedItemData, int>>>() { new KeyValuePair<int, KeyValuePair<TrackedItemData, int>>(2, new KeyValuePair<TrackedItemData, int>(null, slotIndex)) });
                            }
                        }
                    }
                    else
                    {
                        ClientSend.SosigDropSlot(trackedSosig.data.trackedID, slotIndex);
                    }
                }
            }
        }
    }

    // Patches FVRViveHand BeginFlick to take control of the object and CastToFindHover to consider uncontrolled items
    class GrabbityPatch
    {
        static bool FlickPrefix(FVRViveHand __instance, FVRPhysicalObject o)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(o, out TrackedItem currentItem) ? currentItem : o.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                trackedItem.BeginInteraction(__instance);
            }

            return true;
        }

        static bool CastToFindHoverPrefix(FVRViveHand __instance, RaycastHit ___m_grabbity_hit)
        {
            if (Mod.managerObject == null)
            {
                return true;
            }

            bool flag = false;
            if (Physics.Raycast(__instance.PointingTransform.position, __instance.Input.FilteredForward, out ___m_grabbity_hit, 10f, __instance.LM_Grabbity_Beam, QueryTriggerInteraction.Collide) && ___m_grabbity_hit.collider.attachedRigidbody != null)
            {
                FVRPhysicalObject component = ___m_grabbity_hit.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>();
                if (component != null)
                {
                    TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(component, out trackedItem) ? trackedItem : component.GetComponent<TrackedItem>();
                    // If tracked and not under our control, check our own conditions, otherwise treat as normal
                    if (trackedItem != null && trackedItem.data.controller != GameManager.ID &&
                        !trackedItem.itemData.underActiveControl && component.IsDistantGrabbable() && !Physics.Linecast(__instance.PointingTransform.position, __instance.PointingTransform.position + __instance.PointingTransform.forward * ___m_grabbity_hit.distance, __instance.LM_Grabbity_Block, QueryTriggerInteraction.Ignore))
                    {
                        __instance.SetGrabbityHovered(component);
                        flag = true;
                    }
                    else if (!component.IsHeld && component.IsDistantGrabbable() && component.QuickbeltSlot == null && !component.RootRigidbody.isKinematic && !Physics.Linecast(__instance.PointingTransform.position, __instance.PointingTransform.position + __instance.PointingTransform.forward * ___m_grabbity_hit.distance, __instance.LM_Grabbity_Block, QueryTriggerInteraction.Ignore))
                    {
                        __instance.SetGrabbityHovered(component);
                        flag = true;
                    }
                }
            }
            if (!flag && Physics.SphereCast(__instance.PointingTransform.position, 0.2f, __instance.PointingTransform.forward, out ___m_grabbity_hit, 10f, __instance.LM_Grabbity_BeamTrigger, QueryTriggerInteraction.Collide) && ___m_grabbity_hit.collider.attachedRigidbody != null)
            {
                FVRPhysicalObject component2 = ___m_grabbity_hit.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>();
                if (component2 != null)
                {
                    TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(component2, out trackedItem) ? trackedItem : component2.GetComponent<TrackedItem>();
                    // If tracked and not under our control, check our own conditions, otherwise treat as normal
                    if (trackedItem != null && trackedItem.data.controller != GameManager.ID &&
                        !trackedItem.itemData.underActiveControl && component2.IsDistantGrabbable() && !Physics.Linecast(__instance.PointingTransform.position, __instance.PointingTransform.position + __instance.PointingTransform.forward * ___m_grabbity_hit.distance, __instance.LM_Grabbity_Block, QueryTriggerInteraction.Ignore))
                    {
                        __instance.SetGrabbityHovered(component2);
                        flag = true;
                    }
                    else if (!component2.IsHeld && component2.IsDistantGrabbable() && component2.QuickbeltSlot == null && !component2.RootRigidbody.isKinematic && !Physics.Linecast(__instance.PointingTransform.position, __instance.PointingTransform.position + __instance.PointingTransform.forward * ___m_grabbity_hit.distance, __instance.LM_Grabbity_Block, QueryTriggerInteraction.Ignore))
                    {
                        __instance.SetGrabbityHovered(component2);
                        flag = true;
                    }
                }
            }
            if (!flag && Physics.SphereCast(__instance.PointingTransform.position, 0.2f, __instance.PointingTransform.forward, out ___m_grabbity_hit, 10f, __instance.LM_Grabbity_Beam, QueryTriggerInteraction.Collide) && ___m_grabbity_hit.collider.attachedRigidbody != null)
            {
                FVRPhysicalObject component3 = ___m_grabbity_hit.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>();
                if (component3 != null && !component3.IsHeld && component3.IsDistantGrabbable() && component3.QuickbeltSlot == null && !component3.RootRigidbody.isKinematic && !Physics.Linecast(__instance.PointingTransform.position, __instance.PointingTransform.position + __instance.PointingTransform.forward * ___m_grabbity_hit.distance, __instance.LM_Grabbity_Block, QueryTriggerInteraction.Ignore))
                {
                    TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(component3, out trackedItem) ? trackedItem : component3.GetComponent<TrackedItem>();
                    // If tracked and not under our control, check our own conditions, otherwise treat as normal
                    if (trackedItem != null && trackedItem.data.controller != GameManager.ID &&
                        !trackedItem.itemData.underActiveControl && component3.IsDistantGrabbable() && !Physics.Linecast(__instance.PointingTransform.position, __instance.PointingTransform.position + __instance.PointingTransform.forward * ___m_grabbity_hit.distance, __instance.LM_Grabbity_Block, QueryTriggerInteraction.Ignore))
                    {
                        __instance.SetGrabbityHovered(component3);
                        flag = true;
                    }
                    else if (!component3.IsHeld && component3.IsDistantGrabbable() && component3.QuickbeltSlot == null && !component3.RootRigidbody.isKinematic && !Physics.Linecast(__instance.PointingTransform.position, __instance.PointingTransform.position + __instance.PointingTransform.forward * ___m_grabbity_hit.distance, __instance.LM_Grabbity_Block, QueryTriggerInteraction.Ignore))
                    {
                        __instance.SetGrabbityHovered(component3);
                        flag = true;
                    }
                }
            }
            if (!flag)
            {
                __instance.SetGrabbityHovered(null);
            }

            return false;
        }
    }

    // Patches GBeamer to take control of manipulated objects
    class GBeamerPatch
    {
        static bool hadObject;

        static void ObjectSearchPrefix(bool ___m_hasObject)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            hadObject = ___m_hasObject;
        }

        static void ObjectSearchPostfix(FVRPhysicalObject ___m_obj)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            if (!hadObject && ___m_obj != null)
            {
                // Just started manipulating this item, take control
                TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(___m_obj, out TrackedItem currentItem) ? currentItem : ___m_obj.GetComponent<TrackedItem>();
                if (trackedItem != null)
                {
                    trackedItem.BeginInteraction(null);
                }
            }
        }

        static IEnumerable<CodeInstruction> WideShuntTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);

            // To take control of every object we are about to shunt
            List<CodeInstruction> toInsert0 = new List<CodeInstruction>();
            toInsert0.Add(new CodeInstruction(OpCodes.Ldloc_3)); // Load the current physical object
            toInsert0.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GBeamerPatch), "TakeControl"))); // Call our TakeControl method

            bool applied = false;
            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];

                if (instruction.opcode == OpCodes.Ldloc_3)
                {
                    instructionList.InsertRange(i + 1, toInsert0);
                    applied = true;
                    break;
                }
            }

            if (!applied)
            {
                Mod.LogError("GBeamerPatch WideShuntTranspiler not applied!");
            }

            return instructionList;
        }

        public static void TakeControl(FVRPhysicalObject physObj)
        {
            if (Mod.managerObject == null)
            {
                return;
            }

            // Just started manipulating this item, take control
            TrackedItem trackedItem = GameManager.trackedItemByItem.TryGetValue(physObj, out TrackedItem currentItem) ? currentItem : physObj.GetComponent<TrackedItem>();
            if (trackedItem != null)
            {
                trackedItem.BeginInteraction(null);
            }
        }
    }
}
