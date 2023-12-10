using FistVR;
using H3MP.Tracking;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace H3MP.Networking
{
    /// <summary>Sent from server to client.</summary>
    public enum ServerPackets
    {
        welcome = 0,
        spawnPlayer = 1,
        playerState = 2,
        playerScene = 3,
        addNonSyncScene = 4, //UNUSED
        shatterableCrateSetHoldingHealth = 5,
        giveObjectControl = 6,
        objectParent = 7,
        connectSync = 8,
        weaponFire = 9,
        playerDamage = 10,
        sosigPickUpItem = 11,
        sosigPlaceItemIn = 12,
        sosigDropSlot = 13,
        sosigHandDrop = 14,
        sosigConfigure = 15,
        sosigLinkRegisterWearable = 16,
        sosigLinkDeRegisterWearable = 17,
        sosigSetIFF = 18,
        sosigSetOriginalIFF = 19,
        sosigLinkDamage = 20,
        sosigDamageData = 21,
        sosigWearableDamage = 22,
        sosigLinkExplodes = 23,
        sosigDies = 24,
        sosigClear = 25,
        sosigSetBodyState = 26,
        playSosigFootStepSound = 27,
        sosigSpeakState = 28,
        sosigSetCurrentOrder = 29,
        sosigVaporize = 30,
        sosigRequestHitDecal = 31,
        sosigLinkBreak = 32,
        sosigLinkSever = 33,
        updateRequest = 34,
        playerInstance = 35,
        addTNHInstance = 36,
        addTNHCurrentlyPlaying = 37,
        removeTNHCurrentlyPlaying = 38,
        setTNHProgression = 39,
        setTNHEquipment = 40,
        setTNHHealthMode = 41,
        setTNHTargetMode = 42,
        setTNHAIDifficulty = 43,
        setTNHRadarMode = 44,
        setTNHItemSpawnerMode = 45,
        setTNHBackpackMode = 46,
        setTNHHealthMult = 47,
        setTNHSosigGunReload = 48,
        setTNHSeed = 49,
        setTNHLevelID = 50,
        addInstance = 51,
        setTNHController = 52,
        spectatorHost = 53,
        TNHPlayerDied = 54,
        TNHAddTokens = 55,
        TNHSetLevel = 56,
        autoMeaterSetState = 57,
        autoMeaterSetBladesActive = 58,
        autoMeaterDamage = 59,
        autoMeaterFireShot = 60,
        autoMeaterFirearmFireAtWill = 61,
        autoMeaterHitZoneDamage = 62,
        autoMeaterHitZoneDamageData = 63,
        TNHSosigKill = 64,
        TNHHoldPointSystemNode = 65,
        TNHHoldBeginChallenge = 66,
        TNHSetPhaseTake = 67,
        TNHHoldCompletePhase = 68,
        TNHHoldPointFailOut = 69,
        TNHSetPhaseComplete = 70,
        TNHSetPhase = 71,
        encryptionDamage = 72,
        encryptionDamageData = 73,
        encryptionRespawnSubTarg = 74,
        encryptionSpawnGrowth = 75,
        encryptionInit = 76,
        encryptionResetGrowth = 77,
        encryptionDisableSubtarg = 78,
        encryptionSubDamage = 79,
        shatterableCrateDamage = 80,
        shatterableCrateDestroy = 81,
        initTNHInstances = 82,
        sosigWeaponFire = 83,
        sosigWeaponShatter = 84,
        sosigWeaponDamage = 85,
        LAPD2019Fire = 86,
        LAPD2019LoadBattery = 87,
        LAPD2019ExtractBattery = 88,
        minigunFire = 89,
        attachableFirearmFire = 90,
        breakActionWeaponFire = 91,
        playerIFF = 92,
        uberShatterableShatter = 93,
        TNHHoldPointBeginAnalyzing = 94,
        TNHHoldPointRaiseBarriers = 95,
        TNHHoldIdentifyEncryption = 96,
        TNHHoldPointBeginPhase = 97,
        TNHHoldPointCompleteHold = 98,
        sosigPriorityIFFChart = 99,
        leverActionFirearmFire = 100,
        revolvingShotgunFire = 101,
        derringerFire = 102,
        flintlockWeaponBurnOffOuter = 103,
        flintlockWeaponFire = 104,
        grappleGunFire = 105,
        HCBReleaseSled = 106,
        remoteMissileDetonate = 107,
        remoteMissileDamage = 108,
        revolverFire = 109,
        singleActionRevolverFire = 110,
        stingerLauncherFire = 111,
        stingerMissileDamage = 112,
        stingerMissileExplode = 113,
        pinnedGrenadeExplode = 114,
        FVRGrenadeExplode = 115,
        clientDisconnect = 116,
        serverClosed = 117,
        initConnectionData = 118, //UNUSED
        bangSnapSplode = 119,
        C4Detonate = 120,
        claymoreMineDetonate = 121,
        SLAMDetonate = 122,
        ping = 123,
        TNHSetPhaseHold = 124,
        shatterableCrateSetHoldingToken = 125,
        resetTNH = 126,
        reviveTNHPlayer = 127,
        playerColor = 128,
        colorByIFF = 129,
        nameplateMode = 130,
        radarMode = 131,
        radarColor = 132,
        TNHInitializer = 133,
        maxHealth = 134,
        fuseIgnite = 135,
        fuseBoom = 136,
        molotovShatter = 137,
        molotovDamage = 138,
        pinnedGrenadePullPin = 139,
        magazineAddRound = 140,
        clipAddRound = 141,
        speedloaderChamberLoad = 142,
        remoteGunChamber = 143,
        chamberRound = 144,
        magazineLoad = 145,
        magazineLoadAttachable = 146,
        clipLoad = 147,
        revolverCylinderLoad = 148,
        revolvingShotgunLoad = 149,
        grappleGunLoad = 150,
        carlGustafLatchSate = 151,
        carlGustafShellSlideSate = 152,
        TNHHostStartHold = 153,
        integratedFirearmFire = 154,
        grappleAttached = 155,
        trackedObject = 156,
        trackedObjects = 157,
        objectUpdate = 158,
        destroyObject = 159,
        registerCustomPacketType = 160,
        breakableGlassDamage = 161,
        windowShatterSound = 162,
        spectatorHostAssignment = 163,
        giveUpSpectatorHost = 164,
        spectatorHostOrderTNHHost = 165,
        TNHSpectatorHostReady = 166,
        spectatorHostStartTNH = 167,
        unassignSpectatorHost = 168,
        reactiveSteelTargetDamage = 169,
        MTUTest = 170,
        IDConfirm = 171,
        enforcePlayerModels = 172,
        objectScene = 173,
        objectInstance = 174,
        updateEncryptionDisplay = 175,
        encryptionRespawnSubTargGeo = 176,
        roundDamage = 177,
        roundSplode = 178,
        connectionComplete = 179,
        sightFlipperState = 180,
        sightRaiserState = 181,
        gatlingGunFire = 182,
        punchThrough = 183,
        gasCuboidGout = 184,
        gasCuboidDamage = 185,
        gasCuboidHandleDamage = 186,
        gasCuboidDamageHandle = 187,
        gasCuboidExplode = 188,
        gasCuboidShatter = 189,
        floaterDamage = 190,
        floaterCoreDamage = 191,
        floaterBeginExploding = 192,
        floaterExplode = 193,
        irisShatter = 194,
        irisSetState = 195,
        brutBlockSystemStart = 196,
        floaterBeginDefusing = 197,
        batchedPacket = 198,
        nodeInit = 199,
        nodeFire = 200,
        hazeDamage = 201,
        encryptionFireGun = 202
    }

    /// <summary>Sent from client to server.</summary>
    public enum ClientPackets
    {
        grappleAttached = 0,
        welcomeReceived = 1,
        playerState = 2,
        playerScene = 3,
        addNonSyncScene = 4, //UNUSED
        destroyObject = 5,
        objectUpdate = 6,
        shatterableCrateSetHoldingHealth = 7,
        giveObjectControl = 8,
        trackedObjects = 9,
        objectParent = 10,
        weaponFire = 11,
        playerDamage = 12,
        remoteGunChamber = 13,
        chamberRound = 14,
        integratedFirearmFire = 15,
        trackedObject = 16,
        sosigPickupItem = 17,
        sosigPlaceItemIn = 18,
        sosigDropSlot = 19,
        sosigHandDrop = 20,
        sosigConfigure = 21,
        sosigLinkRegisterWearable = 22,
        sosigLinkDeRegisterWearable = 23,
        sosigSetIFF = 24,
        sosigSetOriginalIFF = 25,
        sosigLinkDamage = 26,
        sosigDamageData = 27,
        sosigWearableDamage = 28,
        sosigLinkExplodes = 29,
        sosigDies = 30,
        sosigClear = 31,
        sosigSetBodyState = 32,
        playSosigFootStepSound = 33,
        sosigSpeakState = 34,
        sosigSetCurrentOrder = 35,
        sosigVaporize = 36,
        sosigRequestHitDecal = 37,
        sosigLinkBreak = 38,
        sosigLinkSever = 39,
        updateObjectRequest = 40,
        speedloaderChamberLoad = 41,
        playerInstance = 42,
        addTNHInstance = 43,
        addTNHCurrentlyPlaying = 44,
        removeTNHCurrentlyPlaying = 45,
        setTNHProgression = 46,
        setTNHEquipment = 47,
        setTNHHealthMode = 48,
        setTNHTargetMode = 49,
        setTNHAIDifficulty = 50,
        setTNHRadarMode = 51,
        setTNHItemSpawnerMode = 52,
        setTNHBackpackMode = 53,
        setTNHHealthMult = 54,
        setTNHSosigGunReload = 55,
        setTNHSeed = 56,
        setTNHLevelID = 57,
        addInstance = 58,
        setTNHController = 59,
        spectatorHost = 60,
        TNHPlayerDied = 61,
        TNHAddTokens = 62,
        TNHSetLevel = 63,
        revolvingShotgunLoad = 64,
        grappleGunLoad = 65,
        carlGustafLatchSate = 66,
        carlGustafShellSlideSate = 67,
        TNHHostStartHold = 68,
        autoMeaterSetState = 69,
        autoMeaterSetBladesActive = 70,
        autoMeaterDamage = 71,
        autoMeaterDamageData = 72,
        autoMeaterFireShot = 73,
        autoMeaterFirearmFireAtWill = 74,
        autoMeaterHitZoneDamage = 75,
        autoMeaterHitZoneDamageData = 76,
        TNHSosigKill = 77,
        TNHHoldPointSystemNode = 78,
        TNHHoldBeginChallenge = 79,
        shatterableCrateDamage = 80,
        TNHSetPhaseTake = 81,
        TNHHoldCompletePhase = 82,
        TNHHoldPointFailOut = 83,
        TNHSetPhaseComplete = 84,
        TNHSetPhase = 85,
        magazineLoad = 86,
        magazineLoadAttachable = 87,
        clipLoad = 88,
        revolverCylinderLoad = 89,
        encryptionDamage = 90,
        encryptionDamageData = 91,
        encryptionRespawnSubTarg = 92,
        encryptionSpawnGrowth = 93,
        encryptionInit = 94,
        encryptionResetGrowth = 95,
        encryptionDisableSubtarg = 96,
        encryptionSubDamage = 97,
        shatterableCrateDestroy = 98,
        registerCustomPacketType = 99,
        DoneLoadingScene = 100,
        DoneSendingUpdaToDateObjects = 101,
        sosigWeaponFire = 102,
        sosigWeaponShatter = 103,
        sosigWeaponDamage = 104,
        LAPD2019Fire = 105,
        LAPD2019LoadBattery = 106,
        LAPD2019ExtractBattery = 107,
        minigunFire = 108,
        attachableFirearmFire = 109,
        breakActionWeaponFire = 110,
        playerIFF = 111,
        uberShatterableShatter = 112,
        TNHHoldPointBeginAnalyzing = 113,
        TNHHoldPointRaiseBarriers = 114,
        TNHHoldIdentifyEncryption = 115,
        TNHHoldPointBeginPhase = 116,
        TNHHoldPointCompleteHold = 117,
        sosigPriorityIFFChart = 118,
        leverActionFirearmFire = 119,
        revolvingShotgunFire = 120,
        derringerFire = 121,
        flintlockWeaponBurnOffOuter = 122,
        flintlockWeaponFire = 123,
        grappleGunFire = 124,
        HCBReleaseSled = 125,
        remoteMissileDetonate = 126,
        remoteMissileDamage = 127,
        revolverFire = 128,
        singleActionRevolverFire = 129,
        stingerLauncherFire = 130,
        stingerMissileDamage = 131,
        stingerMissileExplode = 132,
        pinnedGrenadeExplode = 133,
        FVRGrenadeExplode = 134,
        clientDisconnect = 135,
        bangSnapSplode = 136,
        C4Detonate = 137,
        claymoreMineDetonate = 138,
        SLAMDetonate = 139,
        ping = 140,
        TNHSetPhaseHold = 141,
        shatterableCrateSetHoldingToken = 142,
        resetTNH = 143,
        reviveTNHPlayer = 144,
        playerColor = 145,
        requestTNHInit = 146,
        TNHInit = 147,
        fuseIgnite = 148,
        fuseBoom = 149,
        molotovShatter = 150,
        molotovDamage = 151,
        pinnedGrenadePullPin = 152,
        magazineAddRound = 153,
        clipAddRound = 154,
        breakableGlassDamage = 155,
        windowShatterSound = 156,
        requestSpectatorHost = 157,
        unassignSpectatorHost = 158,
        spectatorHostOrderTNHHost = 159,
        TNHSpectatorHostReady = 160,
        spectatorHostStartTNH = 161,
        reassignSpectatorHost = 162,
        reactiveSteelTargetDamage = 163,
        MTUTest = 164,
        IDConfirm = 165,
        objectScene = 166,
        objectInstance = 167,
        updateEncryptionDisplay = 168,
        encryptionRespawnSubTargGeo = 169,
        roundDamage = 170,
        roundSplode = 171,
        sightFlipperState = 172,
        sightRaiserState = 173,
        gatlingGunFire = 174,
        gasCuboidGout = 175,
        gasCuboidDamage = 176,
        gasCuboidHandleDamage = 177,
        gasCuboidDamageHandle = 178,
        gasCuboidExplode = 179,
        gasCuboidShatter = 180,
        floaterDamage = 181,
        floaterCoreDamage = 182,
        floaterBeginExploding = 183,
        floaterExplode = 184,
        irisShatter = 185,
        irisSetState = 186,
        brutBlockSystemStart = 187,
        floaterBeginDefusing = 188,
        batchedPacket = 189,
        nodeInit = 190,
        nodeFire = 191,
        hazeDamage = 192,
        encryptionFireGun = 193
    }

    public class Packet : IDisposable
    {
        public List<byte> buffer;
        public byte[] readableBuffer;
        public int readPos;

        /// <summary>Creates a new empty packet (without an ID).</summary>
        public Packet()
        {
            buffer = new List<byte>(); // Intitialize buffer
            readPos = 0; // Set readPos to 0
        }

        /// <summary>Creates a new packet with a given ID. Used for sending.</summary>
        /// <param name="_id">The packet ID.</param>
        public Packet(int _id)
        {
            buffer = new List<byte>(); // Intitialize buffer
            readPos = 0; // Set readPos to 0

            Write(_id); // Write packet id to the buffer
        }

        /// <summary>Creates a packet from which data can be read. Used for receiving.</summary>
        /// <param name="_data">The bytes to add to the packet.</param>
        public Packet(byte[] _data)
        {
            buffer = new List<byte>(); // Intitialize buffer
            readPos = 0; // Set readPos to 0

            SetBytes(_data);
        }

        #region Functions
        /// <summary>Sets the packet's content and prepares it to be read.</summary>
        /// <param name="_data">The bytes to add to the packet.</param>
        public void SetBytes(byte[] _data)
        {
            Write(_data);
            readableBuffer = buffer.ToArray();
        }

        /// <summary>Inserts the length of the packet's content at the start of the buffer.</summary>
        public void WriteLength()
        {
            // TODO: Optimization: When we instantiate a packet, maybe add 4 placeholder bytes to the buffer right away to reserve space for this so we sdon't have to insert
            //                     every time we send a packet
            buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count)); // Insert the byte length of the packet at the very beginning
        }

        /// <summary>Inserts the given int at the start of the buffer.</summary>
        /// <param name="_value">The int to insert.</param>
        public void InsertInt(int _value)
        {
            // TODO: Optimization: When we instantiate a packet, maybe add 4 placeholder bytes to the buffer right away to reserve space for this so we sdon't have to insert
            //                     every time we send a packet. See WriteLength() above
            buffer.InsertRange(0, BitConverter.GetBytes(_value)); // Insert the int at the start of the buffer
        }

        /// <summary>Gets the packet's content in array form.</summary>
        public byte[] ToArray()
        {
            readableBuffer = buffer.ToArray();
            return readableBuffer;
        }

        /// <summary>Gets the length of the packet's content.</summary>
        public int Length()
        {
            return buffer.Count; // Return the length of buffer
        }

        /// <summary>Gets the length of the unread data contained in the packet.</summary>
        public int UnreadLength()
        {
            return Length() - readPos; // Return the remaining length (unread)
        }

        /// <summary>Resets the packet instance to allow it to be reused.</summary>
        /// <param name="_shouldReset">Whether or not to reset the packet.</param>
        public void Reset(bool _shouldReset = true)
        {
            if (_shouldReset)
            {
                buffer.Clear(); // Clear buffer
                readableBuffer = null;
                readPos = 0; // Reset readPos
            }
            else
            {
                readPos -= 4; // "Unread" the last read int
            }
        }
        #endregion

        #region Write Data
        /// <summary>Adds a byte to the packet.</summary>
        /// <param name="_value">The byte to add.</param>
        public void Write(byte _value)
        {
            buffer.Add(_value);
        }
        /// <summary>Adds an array of bytes to the packet.</summary>
        /// <param name="_value">The byte array to add.</param>
        public void Write(byte[] _value)
        {
            buffer.AddRange(_value);
        }
        /// <summary>Adds a short to the packet.</summary>
        /// <param name="_value">The short to add.</param>
        public void Write(short _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds a ushort to the packet.</summary>
        /// <param name="_value">The ushort to add.</param>
        public void Write(ushort _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds an int to the packet.</summary>
        /// <param name="_value">The int to add.</param>
        public void Write(int _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds an uint to the packet.</summary>
        /// <param name="_value">The uint to add.</param>
        public void Write(uint _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds a long to the packet.</summary>
        /// <param name="_value">The long to add.</param>
        public void Write(long _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds a float to the packet.</summary>
        /// <param name="_value">The float to add.</param>
        public void Write(float _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds a double to the packet.</summary>
        /// <param name="_value">The double to add.</param>
        public void Write(double _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds a bool to the packet.</summary>
        /// <param name="_value">The bool to add.</param>
        public void Write(bool _value)
        {
            buffer.AddRange(BitConverter.GetBytes(_value));
        }
        /// <summary>Adds a string to the packet.</summary>
        /// <param name="_value">The string to add.</param>
        public void Write(string _value)
        {
            Write(_value.Length); // Add the length of the string to the packet
            buffer.AddRange(Encoding.ASCII.GetBytes(_value)); // Add the string itself
        }
        /// <summary>Adds a Vector3 to the packet.</summary>
        /// <param name="_value">The Vector3 to add.</param>
        public void Write(Vector3 _value)
        {
            Write(_value.x);
            Write(_value.y);
            Write(_value.z);
        }
        /// <summary>Adds a Vector2 to the packet.</summary>
        /// <param name="_value">The Vector2 to add.</param>
        public void Write(Vector2 _value)
        {
            Write(_value.x);
            Write(_value.y);
        }
        /// <summary>Adds a Quaternion to the packet.</summary>
        /// <param name="_value">The Quaternion to add.</param>
        public void Write(Quaternion _value)
        {
            Write(_value.x);
            Write(_value.y);
            Write(_value.z);
            Write(_value.w);
        }
        /// <summary>Adds a Damage to the packet.</summary>
        /// <param name="_value">The Damage to add.</param>
        public void Write(Damage damage)
        {
            Write(damage.point);
            Write(damage.Source_IFF);
            Write(damage.Source_Point);
            Write(damage.Dam_Blunt);
            Write(damage.Dam_Piercing);
            Write(damage.Dam_Cutting);
            Write(damage.Dam_TotalKinetic);
            Write(damage.Dam_Thermal);
            Write(damage.Dam_Chilling);
            Write(damage.Dam_EMP);
            Write(damage.Dam_TotalEnergetic);
            Write(damage.Dam_Stunning);
            Write(damage.Dam_Blinding);
            Write(damage.hitNormal);
            Write(damage.strikeDir);
            Write(damage.edgeNormal);
            Write(damage.damageSize);
            Write((byte)damage.Class);
        }
        /// <summary>Adds a SosigConfigTemplate to the packet.</summary>
        /// <param name="config">The SosigConfigTemplate to add.</param>
        public void Write(SosigConfigTemplate config)
        {
            Write(config.AppliesDamageResistToIntegrityLoss);
            Write(config.DoesDropWeaponsOnBallistic);
            Write(config.TotalMustard);
            Write(config.BleedDamageMult);
            Write(config.BleedRateMultiplier);
            Write(config.BleedVFXIntensity);
            Write(config.SearchExtentsModifier);
            Write(config.ShudderThreshold);
            Write(config.ConfusionThreshold);
            Write(config.ConfusionMultiplier);
            Write(config.ConfusionTimeMax);
            Write(config.StunThreshold);
            Write(config.StunMultiplier);
            Write(config.StunTimeMax);
            Write(config.HasABrain);
            Write(config.RegistersPassiveThreats);
            Write(config.CanBeKnockedOut);
            Write(config.MaxUnconsciousTime);
            Write(config.AssaultPointOverridesSkirmishPointWhenFurtherThan);
            Write(config.ViewDistance);
            Write(config.HearingDistance);
            Write(config.MaxFOV);
            Write(config.StateSightRangeMults);
            Write(config.StateHearingRangeMults);
            Write(config.StateFOVMults);
            Write(config.CanPickup_Ranged);
            Write(config.CanPickup_Melee);
            Write(config.CanPickup_Other);
            Write(config.DoesJointBreakKill_Head);
            Write(config.DoesJointBreakKill_Upper);
            Write(config.DoesJointBreakKill_Lower);
            Write(config.DoesSeverKill_Head);
            Write(config.DoesSeverKill_Upper);
            Write(config.DoesSeverKill_Lower);
            Write(config.DoesExplodeKill_Head);
            Write(config.DoesExplodeKill_Upper);
            Write(config.DoesExplodeKill_Lower);
            Write(config.CrawlSpeed);
            Write(config.SneakSpeed);
            Write(config.WalkSpeed);
            Write(config.RunSpeed);
            Write(config.TurnSpeed);
            Write(config.MovementRotMagnitude);
            Write(config.DamMult_Projectile);
            Write(config.DamMult_Explosive);
            Write(config.DamMult_Melee);
            Write(config.DamMult_Piercing);
            Write(config.DamMult_Blunt);
            Write(config.DamMult_Cutting);
            Write(config.DamMult_Thermal);
            Write(config.DamMult_Chilling);
            Write(config.DamMult_EMP);
            Write(config.CanBeSurpressed);
            Write(config.SuppressionMult);
            Write(config.CanBeGrabbed);
            Write(config.CanBeSevered);
            Write(config.CanBeStabbed);
            Write(config.MaxJointLimit);
            if(config.LinkDamageMultipliers == null || config.LinkDamageMultipliers.Count == 0)
            {
                Write((byte)0);
            }
            else
            {
                Write((byte)config.LinkDamageMultipliers.Count);
                foreach(float f in config.LinkDamageMultipliers)
                {
                    Write(f);
                }
            }
            if(config.LinkStaggerMultipliers == null || config.LinkStaggerMultipliers.Count == 0)
            {
                Write((byte)0);
            }
            else
            {
                Write((byte)config.LinkStaggerMultipliers.Count);
                foreach(float f in config.LinkStaggerMultipliers)
                {
                    Write(f);
                }
            }
            if(config.StartingLinkIntegrity == null || config.StartingLinkIntegrity.Count == 0)
            {
                Write((byte)0);
            }
            else
            {
                Write((byte)config.StartingLinkIntegrity.Count);
                foreach(Vector2 v in config.StartingLinkIntegrity)
                {
                    Write(v);
                }
            }
            if (config.StartingChanceBrokenJoint == null || config.StartingChanceBrokenJoint.Count == 0)
            {
                Write((byte)0);
            }
            else
            {
                Write((byte)config.StartingChanceBrokenJoint.Count);
                foreach (float f in config.StartingChanceBrokenJoint)
                {
                    Write(f);
                }
            }
            if (config.LinkSpawnChance == null || config.LinkSpawnChance.Count == 0)
            {
                Write((byte)0);
            }
            else
            {
                Write((byte)config.LinkSpawnChance.Count);
                foreach (float f in config.LinkSpawnChance)
                {
                    Write(f);
                }
            }
            Write(config.TargetCapacity);
            Write(config.TargetTrackingTime);
            Write(config.NoFreshTargetTime);
            Write(config.DoesAggroOnFriendlyFire);
            Write(config.UsesLinkSpawns);
            Write(config.OverrideSpeech);
            Write(config.TimeInSkirmishToAlert);
        }
        /// <summary>Adds a TNHInstance to the packet.</summary>
        /// <param name="instance">The TNHInstance to add.</param>
        public void Write(TNHInstance instance, bool full = false)
        {
            Write(instance.instance);
            Write(instance.letPeopleJoin);
            Write(instance.progressionTypeSetting);
            Write(instance.healthModeSetting);
            Write(instance.equipmentModeSetting);
            Write(instance.targetModeSetting);
            Write(instance.AIDifficultyModifier);
            Write(instance.radarModeModifier);
            Write(instance.itemSpawnerMode);
            Write(instance.backpackMode);
            Write(instance.healthMult);
            Write(instance.sosiggunShakeReloading);
            Write(instance.TNHSeed);
            Write(instance.levelID);
            if (instance.playerIDs == null || instance.playerIDs.Count == 0)
            {
                Write(0);
            }
            else
            {
                Write(instance.playerIDs.Count);
                for (int i = 0; i < instance.playerIDs.Count; ++i)
                {
                    Write(instance.playerIDs[i]);
                }
            }
            Write(instance.initializer);
            Write(instance.controller);

            if (full)
            {
                if (instance.currentlyPlaying == null || instance.currentlyPlaying.Count == 0)
                {
                    Write(0);
                }
                else
                {
                    Write(instance.currentlyPlaying.Count);
                    foreach (int playerID in instance.currentlyPlaying)
                    {
                        Write(playerID);
                    }
                }
                if (instance.played == null || instance.played.Count == 0)
                {
                    Write(0);
                }
                else
                {
                    Write(instance.played.Count);
                    foreach (int playerID in instance.played)
                    {
                        Write(playerID);
                    }
                }
                if (instance.dead == null || instance.dead.Count == 0)
                {
                    Write(0);
                }
                else
                {
                    Write(instance.dead.Count);
                    foreach (int playerID in instance.dead)
                    {
                        Write(playerID);
                    }
                }
                Write(instance.tokenCount);
                Write(instance.holdOngoing);
                Write(instance.curHoldIndex);
                Write(instance.level);
                Write((short)instance.phase);
                if (instance.activeSupplyPointIndices == null || instance.activeSupplyPointIndices.Count == 0)
                {
                    Write(0);
                }
                else
                {
                    Write(instance.activeSupplyPointIndices.Count);
                    foreach (int index in instance.activeSupplyPointIndices)
                    {
                        Write(index);
                    }
                }
                if (instance.raisedBarriers == null || instance.raisedBarriers.Count == 0)
                {
                    Write(0);
                }
                else
                {
                    Write(instance.raisedBarriers.Count);
                    foreach (int index in instance.raisedBarriers)
                    {
                        Write(index);
                    }
                }
                if (instance.raisedBarrierPrefabIndices == null || instance.raisedBarrierPrefabIndices.Count == 0)
                {
                    Write(0);
                }
                else
                {
                    Write(instance.raisedBarrierPrefabIndices.Count);
                    foreach (int prefabIndex in instance.raisedBarrierPrefabIndices)
                    {
                        Write(prefabIndex);
                    }
                }
            }
        }
        /// <summary>Adds a TNH_Manager.SosigPatrolSquad to the packet.</summary>
        /// <param name="instance">The TNH_Manager.SosigPatrolSquad to add.</param>
        public void Write(TNH_Manager.SosigPatrolSquad patrol)
        {
            Write(patrol.Squad.Count);
            for(int i=0; i < patrol.Squad.Count; ++i)
            {
                Write(patrol.Squad[i].GetComponent<TrackedSosig>().data.trackedID);
            }
            Write(patrol.PatrolPoints.Count);
            for(int i=0; i < patrol.PatrolPoints.Count; ++i)
            {
                Write(patrol.PatrolPoints[i]);
            }
            Write(patrol.CurPatrolPointIndex);
            Write(patrol.IsPatrollingUp);
        }
        #endregion

        #region Read Data
        /// <summary>Reads a byte from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte ReadByte(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                byte _value = readableBuffer[readPos]; // Get the byte at readPos' position
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 1; // Increase readPos by 1
                }
                return _value; // Return the byte
            }
            else
            {
                throw new Exception("Could not read value of type 'byte'!");
            }
        }

        /// <summary>Reads an array of bytes from the packet.</summary>
        /// <param name="_length">The length of the byte array.</param>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public byte[] ReadBytes(int _length, bool _moveReadPos = true)
        {
            if(_length == 0)
            {
                return null;
            }

            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                byte[] _value = buffer.GetRange(readPos, _length).ToArray(); // Get the bytes at readPos' position with a range of _length
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += _length; // Increase readPos by _length
                }
                return _value; // Return the bytes
            }
            else
            {
                throw new Exception("Could not read value of type 'byte[]'!");
            }
        }

        /// <summary>Reads a short from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public short ReadShort(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                short _value = BitConverter.ToInt16(readableBuffer, readPos); // Convert the bytes to a short
                if (_moveReadPos)
                {
                    // If _moveReadPos is true and there are unread bytes
                    readPos += 2; // Increase readPos by 2
                }
                return _value; // Return the short
            }
            else
            {
                throw new Exception("Could not read value of type 'short'!");
            }
        }

        /// <summary>Reads an ushort from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public ushort ReadUShort(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                ushort _value = BitConverter.ToUInt16(readableBuffer, readPos); // Convert the bytes to an ushort
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 2; // Increase readPos by 2
                }
                return _value; // Return the ushort
            }
            else
            {
                throw new Exception("Could not read value of type 'ushort'!");
            }
        }

        /// <summary>Reads an int from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public int ReadInt(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                int _value = BitConverter.ToInt32(readableBuffer, readPos); // Convert the bytes to an int
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 4; // Increase readPos by 4
                }
                return _value; // Return the int
            }
            else
            {
                throw new Exception("Could not read value of type 'int'!");
            }
        }

        /// <summary>Reads an uint from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public uint ReadUInt(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                uint _value = BitConverter.ToUInt32(readableBuffer, readPos); // Convert the bytes to an uint
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 4; // Increase readPos by 4
                }
                return _value; // Return the uint
            }
            else
            {
                throw new Exception("Could not read value of type 'uint'!");
            }
        }

        /// <summary>Reads a long from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public long ReadLong(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                long _value = BitConverter.ToInt64(readableBuffer, readPos); // Convert the bytes to a long
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 8; // Increase readPos by 8
                }
                return _value; // Return the long
            }
            else
            {
                throw new Exception("Could not read value of type 'long'!");
            }
        }

        /// <summary>Reads a float from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public float ReadFloat(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                float _value = BitConverter.ToSingle(readableBuffer, readPos); // Convert the bytes to a float
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 4; // Increase readPos by 4
                }
                return _value; // Return the float
            }
            else
            {
                throw new Exception("Could not read value of type 'float'!");
            }
        }

        /// <summary>Reads a double from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public double ReadDouble(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                double _value = BitConverter.ToDouble(readableBuffer, readPos); // Convert the bytes to a double
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 8; // Increase readPos by 8
                }
                return _value; // Return the double
            }
            else
            {
                throw new Exception("Could not read value of type 'double'!");
            }
        }

        /// <summary>Reads a bool from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public bool ReadBool(bool _moveReadPos = true)
        {
            if (buffer.Count > readPos)
            {
                // If there are unread bytes
                bool _value = BitConverter.ToBoolean(readableBuffer, readPos); // Convert the bytes to a bool
                if (_moveReadPos)
                {
                    // If _moveReadPos is true
                    readPos += 1; // Increase readPos by 1
                }
                return _value; // Return the bool
            }
            else
            {
                throw new Exception("Could not read value of type 'bool'!");
            }
        }

        /// <summary>Reads a string from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public string ReadString(bool _moveReadPos = true)
        {
            try
            {
                int _length = ReadInt(); // Get the length of the string
                string _value = Encoding.ASCII.GetString(readableBuffer, readPos, _length); // Convert the bytes to a string
                if (_moveReadPos && _value.Length > 0)
                {
                    // If _moveReadPos is true string is not empty
                    readPos += _length; // Increase readPos by the length of the string
                }
                return _value; // Return the string
            }
            catch
            {
                throw new Exception("Could not read value of type 'string'!");
            }
        }

        /// <summary>Reads a Vector3 from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector3 ReadVector3(bool _moveReadPos = true)
        {
            return new Vector3(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
        }

        /// <summary>Reads a Vector2 from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public Vector2 ReadVector2(bool _moveReadPos = true)
        {
            return new Vector2(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
        }

        /// <summary>Reads a Quaternion from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public Quaternion ReadQuaternion(bool _moveReadPos = true)
        {
            return new Quaternion(ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos), ReadFloat(_moveReadPos));
        }

        /// <summary>Reads a SosigConfigTemplate from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public SosigConfigTemplate ReadSosigConfig(bool full = false, bool _moveReadPos = true)
        {
            SosigConfigTemplate config = ScriptableObject.CreateInstance<SosigConfigTemplate>();

            config.AppliesDamageResistToIntegrityLoss = ReadBool();
            config.DoesDropWeaponsOnBallistic = ReadBool();
            config.TotalMustard = ReadFloat();
            config.BleedDamageMult = ReadFloat();
            config.BleedRateMultiplier = ReadFloat();
            config.BleedVFXIntensity = ReadFloat();
            config.SearchExtentsModifier = ReadFloat();
            config.ShudderThreshold = ReadFloat();
            config.ConfusionThreshold = ReadFloat();
            config.ConfusionMultiplier = ReadFloat();
            config.ConfusionTimeMax = ReadFloat();
            config.StunThreshold = ReadFloat();
            config.StunMultiplier = ReadFloat();
            config.StunTimeMax = ReadFloat();
            config.HasABrain = ReadBool();
            config.RegistersPassiveThreats = ReadBool();
            config.CanBeKnockedOut = ReadBool();
            config.MaxUnconsciousTime = ReadFloat();
            config.AssaultPointOverridesSkirmishPointWhenFurtherThan = ReadFloat();
            config.ViewDistance = ReadFloat();
            config.HearingDistance = ReadFloat();
            config.MaxFOV = ReadFloat();
            config.StateSightRangeMults = ReadVector3();
            config.StateHearingRangeMults = ReadVector3();
            config.StateFOVMults = ReadVector3();
            config.CanPickup_Ranged = ReadBool();
            config.CanPickup_Melee = ReadBool();
            config.CanPickup_Other = ReadBool();
            config.DoesJointBreakKill_Head = ReadBool();
            config.DoesJointBreakKill_Upper = ReadBool();
            config.DoesJointBreakKill_Lower = ReadBool();
            config.DoesSeverKill_Head = ReadBool();
            config.DoesSeverKill_Upper = ReadBool();
            config.DoesSeverKill_Lower = ReadBool();
            config.DoesExplodeKill_Head = ReadBool();
            config.DoesExplodeKill_Upper = ReadBool();
            config.DoesExplodeKill_Lower = ReadBool();
            config.CrawlSpeed = ReadFloat();
            config.SneakSpeed = ReadFloat();
            config.WalkSpeed = ReadFloat();
            config.RunSpeed = ReadFloat();
            config.TurnSpeed = ReadFloat();
            config.MovementRotMagnitude = ReadFloat();
            config.DamMult_Projectile = ReadFloat();
            config.DamMult_Explosive = ReadFloat();
            config.DamMult_Melee = ReadFloat();
            config.DamMult_Piercing = ReadFloat();
            config.DamMult_Blunt = ReadFloat();
            config.DamMult_Cutting = ReadFloat();
            config.DamMult_Thermal = ReadFloat();
            config.DamMult_Chilling = ReadFloat();
            config.DamMult_EMP = ReadFloat();
            config.CanBeSurpressed = ReadBool();
            config.SuppressionMult = ReadFloat();
            config.CanBeGrabbed = ReadBool();
            config.CanBeSevered = ReadBool();
            config.CanBeStabbed = ReadBool();
            config.MaxJointLimit = ReadFloat();
            byte linkDamMultCount = ReadByte();
            if (linkDamMultCount > 0)
            {
                config.LinkDamageMultipliers = new List<float>();
                for (int i=0; i < linkDamMultCount; ++i)
                {
                    config.LinkDamageMultipliers.Add(ReadFloat());
                }
            }
            byte linkStaggerMultCount = ReadByte();
            if (linkStaggerMultCount > 0)
            {
                config.LinkStaggerMultipliers = new List<float>();
                for (int i = 0; i < linkStaggerMultCount; ++i)
                {
                    config.LinkStaggerMultipliers.Add(ReadFloat());
                }
            }
            byte startLinkIntegCount = ReadByte();
            if (startLinkIntegCount > 0)
            {
                config.StartingLinkIntegrity = new List<Vector2>();
                for (int i = 0; i < startLinkIntegCount; ++i)
                {
                    config.StartingLinkIntegrity.Add(ReadVector2());
                }
            }
            byte startBreakChanceCount = ReadByte();
            if (startBreakChanceCount > 0)
            {
                config.StartingChanceBrokenJoint = new List<float>();
                for (int i = 0; i < startBreakChanceCount; ++i)
                {
                    config.StartingChanceBrokenJoint.Add(ReadFloat());
                }
            }
            byte linkSpawnChanceCount = ReadByte();
            if (linkSpawnChanceCount > 0)
            {
                config.LinkSpawnChance = new List<float>();
                for (int i = 0; i < linkSpawnChanceCount; ++i)
                {
                    config.LinkSpawnChance.Add(ReadFloat());
                }
            }
            config.TargetCapacity = ReadInt();
            config.TargetTrackingTime = ReadFloat();
            config.NoFreshTargetTime = ReadFloat();
            config.DoesAggroOnFriendlyFire = ReadBool();
            config.UsesLinkSpawns = ReadBool();
            config.OverrideSpeech = ReadBool();
            config.TimeInSkirmishToAlert = ReadFloat();

            return config;
        }

        /// <summary>Reads a Damage from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public Damage ReadDamage(bool _moveReadPos = true)
        {
            Damage damage = new Damage();

            damage.point = ReadVector3();
            damage.Source_IFF = ReadInt();
            damage.Source_Point = ReadVector3();
            damage.Dam_Blunt = ReadFloat();
            damage.Dam_Piercing = ReadFloat();
            damage.Dam_Cutting = ReadFloat();
            damage.Dam_TotalKinetic = ReadFloat();
            damage.Dam_Thermal = ReadFloat();
            damage.Dam_Chilling = ReadFloat();
            damage.Dam_EMP = ReadFloat();
            damage.Dam_TotalEnergetic = ReadFloat();
            damage.Dam_Stunning = ReadFloat();
            damage.Dam_Blinding = ReadFloat();
            damage.hitNormal = ReadVector3();
            damage.strikeDir = ReadVector3();
            damage.edgeNormal = ReadVector3();
            damage.damageSize = ReadFloat();
            damage.Class = (Damage.DamageClass)ReadByte();

            return damage;
        }

        /// <summary>Reads a TNHInstance from the packet.</summary>
        /// <param name="_moveReadPos">Whether or not to move the buffer's read position.</param>
        public TNHInstance ReadTNHInstance(bool full = false, bool _moveReadPos = true)
        {
            int instanceID = ReadInt();
            bool letPeopleJoin = ReadBool();
            int progressionTypeSetting = ReadInt();
            int healthModeSetting = ReadInt();
            int equipmentModeSetting = ReadInt();
            int targetModeSetting = ReadInt();
            int AIDifficultyModifier = ReadInt();
            int radarModeModifier = ReadInt();
            int itemSpawnerMode = ReadInt();
            int backpackMode = ReadInt();
            int healthMult = ReadInt();
            int sosiggunShakeReloading = ReadInt();
            int TNHSeed = ReadInt();
            string levelID = ReadString();
            int playerCount = ReadInt();
            int hostID = ReadInt();
            TNHInstance instance = new TNHInstance(instanceID, hostID, letPeopleJoin,
                                                             progressionTypeSetting, healthModeSetting, equipmentModeSetting,
                                                             targetModeSetting, AIDifficultyModifier, radarModeModifier,
                                                             itemSpawnerMode, backpackMode, healthMult, sosiggunShakeReloading, TNHSeed, levelID);
            for (int i = 1; i < playerCount; ++i) 
            {
                int newPlayerID = ReadInt();

                if (instance.playerIDs.Contains(newPlayerID))
                {
                    Mod.LogWarning("ReadTNHInstance player ID: " + newPlayerID + " already in TNH instance " + instanceID + ":\n" + Environment.StackTrace);
                    continue;
                }

                instance.playerIDs.Add(newPlayerID);
            }

            instance.initializer = ReadInt();
            instance.controller = ReadInt();

            if (full)
            {
                int currentlyPlayingCount = ReadInt();
                for(int i=0; i < currentlyPlayingCount; ++i)
                {
                    instance.currentlyPlaying.Add(ReadInt());
                }
                int playedCount = ReadInt();
                for(int i=0; i < playedCount; ++i)
                {
                    instance.played.Add(ReadInt());
                }
                int deadCount = ReadInt();
                for(int i=0; i < deadCount; ++i)
                {
                    instance.dead.Add(ReadInt());
                }
                instance.tokenCount = ReadInt();
                instance.holdOngoing = ReadBool();
                instance.curHoldIndex = ReadInt();
                instance.level = ReadInt();
                instance.phase = (TNH_Phase)ReadShort();
                int activeSupplyPointIndicesCount = ReadInt();
                if(activeSupplyPointIndicesCount > 0)
                {
                    instance.activeSupplyPointIndices = new List<int>();
                }
                for (int i = 0; i < activeSupplyPointIndicesCount; ++i)
                {
                    instance.activeSupplyPointIndices.Add(ReadInt());
                }
                int raisedBarriersCount = ReadInt();
                if (raisedBarriersCount > 0)
                {
                    instance.raisedBarriers = new List<int>();
                }
                for (int i = 0; i < raisedBarriersCount; ++i)
                {
                    instance.raisedBarriers.Add(ReadInt());
                }
                int raisedBarrierPrefabIndicesCount = ReadInt();
                if (raisedBarrierPrefabIndicesCount > 0)
                {
                    instance.raisedBarrierPrefabIndices = new List<int>();
                }
                for (int i = 0; i < raisedBarrierPrefabIndicesCount; ++i)
                {
                    instance.raisedBarrierPrefabIndices.Add(ReadInt());
                }
            }

            return instance;
        }
        #endregion

        private bool disposed = false;

        protected virtual void Dispose(bool _disposing)
        {
            if (!disposed)
            {
                if (_disposing)
                {
                    buffer = null;
                    readableBuffer = null;
                    readPos = 0;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}