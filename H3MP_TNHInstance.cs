using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP
{
    public class H3MP_TNHInstance
    {
        public int instance = -1;
        public List<int> playerIDs; // Players in this instance
        private int _currentlyPlaying; // Number of players actually in-game
        public int currentlyPlaying
        {
            get { return _currentlyPlaying; }
            set
            {
                int preVal = _currentlyPlaying;
                _currentlyPlaying = value; 
                if(preVal != 0 && _currentlyPlaying == 0 && Mod.TNHInstanceList != null)
                {
                    GameObject newInstance = GameObject.Instantiate<GameObject>(Mod.TNHInstancePrefab, Mod.TNHInstanceList.transform);
                    newInstance.transform.GetChild(0).GetComponent<Text>().text = "Instance " + instance;
                    newInstance.SetActive(true);

                    FVRPointableButton instanceButton = newInstance.AddComponent<FVRPointableButton>();
                    instanceButton.SetButton();
                    instanceButton.MaxPointingRange = 5;
                    instanceButton.Button.onClick.AddListener(() => { Mod.modInstance.OnTNHInstanceClicked(instance); });

                    Mod.joinTNHInstances.Add(instance, newInstance);
                }
                else if(preVal == 0 && _currentlyPlaying != 0 && Mod.TNHInstanceList != null && Mod.joinTNHInstances.ContainsKey(instance))
                {
                    GameObject.Destroy(Mod.joinTNHInstances[instance]);
                    Mod.joinTNHInstances.Remove(instance);
                }
            }
        }

        // Settings
        public bool letPeopleJoin;

        public H3MP_TNHInstance(int instance, int hostID, bool letPeopleJoin)
        {
            this.instance = instance;
            playerIDs = new List<int>();
            playerIDs.Add(hostID);

            this.letPeopleJoin = letPeopleJoin;
        }

        public void AddCurrentlyPlaying()
        {
            ++currentlyPlaying;

            // Send to other clients
            if (H3MP_ThreadManager.host)
            {
                H3MP_ServerSend.AddTNHCurrentlyPlaying(instance);
            }
            else
            {
                H3MP_ClientSend.AddTNHCurrentlyPlaying(instance);
            }
        }

        public void RemoveCurrentlyPlaying()
        {
            --currentlyPlaying;

            // Send to other clients
            if (H3MP_ThreadManager.host)
            {
                H3MP_ServerSend.RemoveTNHCurrentlyPlaying(instance);
            }
            else
            {
                H3MP_ClientSend.RemoveTNHCurrentlyPlaying(instance);
            }
        }
    }
}
