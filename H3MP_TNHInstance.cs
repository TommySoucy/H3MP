using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace H3MP
{
    public class H3MP_TNHInstance
    {
        public int instance = -1;
        public List<int> playerIDs;

        // Settings
        public bool letPeopleJoin;

        public H3MP_TNHInstance(int instance, int hostID, bool letPeopleJoin)
        {
            this.instance = instance;
            playerIDs = new List<int>();
            playerIDs.Add(hostID);

            this.letPeopleJoin = letPeopleJoin;
        }
    }
}
