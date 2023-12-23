using FistVR;
using H3MP.Scripts;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedFloater : TrackedObject
    {
        public Construct_Floater physicalFloater;
        public TrackedFloaterData floaterData;

        public static List<uint> unknownFloaterBeginExploding = new List<uint>();
        public static List<uint> unknownFloaterBeginDefusing = new List<uint>();
        public static Dictionary<uint, bool> unknownFloaterExplode = new Dictionary<uint, bool>();

        public override void Awake()
        {
            base.Awake();

            GameObject trackedItemRef = new GameObject();
            trackedItemRef.transform.parent = transform;
            TrackedObjectReference refScript = trackedItemRef.AddComponent<TrackedObjectReference>();
            trackedItemRef.SetActive(false);

            CheckReferenceSize();
            int refIndex = availableTrackedRefIndices[availableTrackedRefIndices.Count - 1];
            availableTrackedRefIndices.RemoveAt(availableTrackedRefIndices.Count - 1);
            trackedReferenceObjects[refIndex] = trackedItemRef;
            trackedReferences[refIndex] = this;
            trackedItemRef.name = refIndex.ToString();
            refScript.refIndex = refIndex;
            GetComponent<Construct_Floater>().SpawnOnSplode.Add(trackedItemRef);
        }

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the physical object anymore
            GameManager.trackedFloaterByFloater.Remove(physicalFloater);
            GameManager.trackedObjectByDamageable.Remove(physicalFloater);
            IFVRDamageable damageable = physicalFloater.GetComponentInChildren<Construct_Floater_Core>();
            if (damageable != null)
            {
                GameManager.trackedObjectByDamageable.Remove(damageable);
            }

            base.OnDestroy();
        }
    }
}
