using FistVR;
using H3MP.Networking;
using H3MP.Patches;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace H3MP.Tracking
{
    public class TrackedTemplate : TrackedObject
    {
        /// <summary>
        /// If you need to implement Awake() for this tracked type, you will need to call base 
        /// The base call should be made AFTER you do what you need to do since it will make an initial update of the object
        /// Refer to other tracked types' implementation for examples
        /// </summary>
        public override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// If you need to implement Awake() for this tracked type, you will need to call base 
        /// The base call should be made AFTER you do what you need to do since it will make an initial update of the object
        /// Refer to other tracked types' implementation for examples
        /// See the content of definition for more details
        /// </summary>
        protected override void OnDestroy()
        {
            /// This should be checked at the beginning
            /// This flag will be set by H3MP when it needs to skip the entire destruction process
            if (skipFullDestroy)
            {
                return;
            }

            /// This is where you'd remove references to the physical tracked object if you keep any
            GameManager.trackedTemplateByTemplate.Remove(physicalTemplate);

            /// If your tracked object can be under active control, you'd want to make sure it isn't under active control anymore here
            EnsureUncontrolled();

            /// Base MUST be called after handling thie type's destruction
            base.OnDestroy();
        }

        /// <summary>
        /// Will be called upon destruction of the object
        /// This should be used in case you need to destroy some detached part of your object that will not be destroyed alongside the destruction of
        /// the original physical script's gameobject
        /// Refer to TrackedSosig's implementation where the main Sosig script is not on the same gameobject as the SosigLinks
        /// so we have to destroy them separately
        /// Note: Implementation is optional
        /// </summary>
        public override void SecondaryDestroy() 
        { 
        }

        /// <summary>
        /// Should ensure the tracked object is not under active control
        /// Note: Implementation is only necessary if this type can be under active control
        /// See TrackedItem implementation for an example
        /// </summary>
        public override void EnsureUncontrolled()
        {
        }

        /// <summary>
        /// When interaction begins (picked up by player or sosig, put in QBS, graviton beamer, etc.), if interaction is possible with this type, this will be called
        /// Refer to TrackedItem implementation for example
        /// Note: Implementation is optional
        /// </summary>
        /// <param name="hand">The hand that was used to interact with this object, if applicable</param>
        public override void BeginInteraction(FVRViveHand hand)
        {
        }

        /// <summary>
        /// When interaction ends (dropped by hand, drop by sosig, etc.), if interaction is possible with this type, this will be called
        /// Refer to TrackedSosig's implementation for example, where in the case of TNH we need to give control of the Sosig to the TNH instance 
        /// controller when we drop it
        /// Note: Implementation is optional
        /// </summary>
        /// <param name="hand">The hand that was used to interact with this object, if applicable</param>
        public override void EndInteraction(FVRViveHand hand) 
        {
        }
    }
}
