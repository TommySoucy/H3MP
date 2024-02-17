using FistVR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace H3MP.Scripts
{
    public class BodyWristMenuSection : FVRWristMenuSection
    {
        public static bool init;

        delegate void ButtonClick(Text text);
        Dictionary<int, List<KeyValuePair<FVRPointableButton, Vector3>>> pages;
        int currentPage = -1;

        public static Text playerBodyText;
        public static Text colorText;
        public static Text visibilityText;
        public static Text handText;
        public static Text selfHideText;

        public override void Enable()
        {
            // Init buttons if not already done
            InitButtons();

            SetPage(0);
        }

        private void InitButtons()
        {
            if (pages != null)
            {
                return;
            }

            pages = new Dictionary<int, List<KeyValuePair<FVRPointableButton, Vector3>>>();

            Image background = gameObject.AddComponent<Image>();
            background.rectTransform.sizeDelta = new Vector2(500, 350);
            background.color = new Color(0.1f, 0.1f, 0.1f, 1);

            Text textOut = null;
            //InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(-215, 140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnBackClicked, "Back", out textOut);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 150, 0) }, new Vector2(1200, 150), new Vector2(340, 45), OnPlayerBodyClicked, "Body: " + GameManager.playerPrefabID, out playerBodyText);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(155, 150, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextPlayerBodyClicked, ">", out textOut);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(-155, 150, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousPlayerBodyClicked, "<", out textOut);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 100, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnColorClicked, "Current color: " + GameManager.colorNames[GameManager.colorIndex], out colorText);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(155, 100, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnNextColorClicked, ">", out textOut);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(-155, 100, 0) }, new Vector2(150, 150), new Vector2(45, 45), OnPreviousColorClicked, "<", out textOut);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 50, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnVisibleClicked, "Visible: " + (GameManager.currentPlayerBody != null && GameManager.currentPlayerBody.bodyRenderers != null && GameManager.currentPlayerBody.bodyRenderers.Length > 0 && GameManager.currentPlayerBody.bodyRenderers[0].enabled ? "True" : "False"), out visibilityText);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, 0, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnHandsClicked, "Hands visible: " + (GameManager.currentPlayerBody != null && GameManager.currentPlayerBody.handRenderers != null && GameManager.currentPlayerBody.handRenderers.Length > 0 && GameManager.currentPlayerBody.handRenderers[0].enabled ? "True" : "False"), out handText);
            InitButton(new List<int>() { 0 }, new List<Vector3>() { new Vector3(0, -50, 0) }, new Vector2(1200, 150), new Vector2(270, 45), OnSelfHideClicked, "Auto-hide self: " + (PlayerBody.optionAutoHideSelf ? "True" : "False"), out selfHideText);
            //InitButton(new List<int>() { 3 }, new List<Vector3>() { new Vector3(215, -140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnNextOptionsClicked, "Next", out textOut);
            //InitButton(new List<int>() { 4 }, new List<Vector3>() { new Vector3(-215, -140, 0) }, new Vector2(240, 240), new Vector2(70, 70), OnPrevOptionsClicked, "Prev", out textOut);
        }

        private void InitButton(List<int> pageIndices, List<Vector3> positions, Vector2 sizeDelta, Vector2 boxSize, ButtonClick clickMethod, string defaultText, out Text textOut)
        {
            GameObject button = Instantiate(this.Menu.BaseButton, transform);
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = sizeDelta;
            buttonRect.GetChild(0).GetComponent<RectTransform>().sizeDelta = sizeDelta;
            BoxCollider boxCollider = button.GetComponent<BoxCollider>();
            boxCollider.size = new Vector3(boxSize.x, boxSize.y, boxCollider.size.z);
            button.transform.localPosition = positions[0];
            button.transform.localRotation = Quaternion.identity;
            Destroy(button.GetComponent<FVRWristMenuSectionButton>());
            button.GetComponent<Text>().text = defaultText;
            FVRPointableButton BTN_Ref = button.GetComponent<FVRPointableButton>();
            Text buttonText = button.GetComponent<Text>();
            textOut = buttonText;
            BTN_Ref.Button.onClick.AddListener(() => clickMethod(buttonText));

            for (int i = 0; i < pageIndices.Count; ++i)
            {
                if (pages.TryGetValue(pageIndices[i], out List<KeyValuePair<FVRPointableButton, Vector3>> buttons))
                {
                    buttons.Add(new KeyValuePair<FVRPointableButton, Vector3>(BTN_Ref, positions[i]));
                }
                else
                {
                    KeyValuePair<FVRPointableButton, Vector3> entry = new KeyValuePair<FVRPointableButton, Vector3>(BTN_Ref, positions[i]);
                    pages.Add(pageIndices[i], new List<KeyValuePair<FVRPointableButton, Vector3>>() { entry });
                }
            }
        }

        private void OnColorClicked(Text textRef)
        {
            // Place holder
        }

        private void OnNextColorClicked(Text textRef)
        {
            if (GameManager.colorByIFF)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                ++GameManager.colorIndex;
                if (GameManager.colorIndex >= GameManager.colors.Length)
                {
                    GameManager.colorIndex = 0;
                }

                GameManager.SetPlayerColor(GameManager.ID, GameManager.colorIndex, false, 0);
            }
        }

        private void OnPreviousColorClicked(Text textRef)
        {
            if (GameManager.colorByIFF)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                --GameManager.colorIndex;
                if (GameManager.colorIndex < 0)
                {
                    GameManager.colorIndex = GameManager.colors.Length - 1;
                }

                GameManager.SetPlayerColor(GameManager.ID, GameManager.colorIndex, false, 0);
            }
        }

        private void OnPlayerBodyClicked(Text textRef)
        {
            // Place holder
        }

        private void OnNextPlayerBodyClicked(Text textRef)
        {
            if (GameManager.playerModelAwaitingInstantiation)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
                return;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            ++GameManager.playerPrefabIndex;

            if (GameManager.playerPrefabIndex >= GameManager.playerPrefabIDs.Count)
            {
                if (Mod.managerObject == null)
                {
                    GameManager.playerPrefabIndex = -1;
                }
                else
                {
                    GameManager.playerPrefabIndex = 0;
                }
            }

            ProcessPlayerPrefabIndex(GameManager.playerPrefabIndex, playerBodyText);
        }

        private void OnPreviousPlayerBodyClicked(Text textRef)
        {
            if (GameManager.playerModelAwaitingInstantiation)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
                return;
            }

            SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

            --GameManager.playerPrefabIndex;

            if ((Mod.managerObject != null && GameManager.playerPrefabIndex < 0)
                || (Mod.managerObject == null && GameManager.playerPrefabIndex < -1))
            {
                GameManager.playerPrefabIndex = GameManager.playerPrefabIDs.Count - 1;
            }

            ProcessPlayerPrefabIndex(GameManager.playerPrefabIndex, playerBodyText);
        }

        private void ProcessPlayerPrefabIndex(int index, Text text)
        {
            if (GameManager.playerPrefabIndex == -1)
            {
                GameManager.playerPrefabID = "None";

                if (GameManager.currentPlayerBody != null)
                {
                    Destroy(GameManager.currentPlayerBody.gameObject);
                }

                text.text = "Body: None";

                return;
            }

            string newID = GameManager.playerPrefabIDs[GameManager.playerPrefabIndex];
            if (!GameManager.playerPrefabID.Equals(newID))
            {
                GameManager.SetPlayerPrefab(newID);
            }
        }

        private void OnVisibleClicked(Text textRef)
        {
            if (GameManager.currentPlayerBody == null 
                || GameManager.currentPlayerBody.bodyRenderers == null
                || GameManager.currentPlayerBody.bodyRenderers.Length == 0)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                GameManager.bodyVisible = !GameManager.bodyVisible;
                GameManager.currentPlayerBody.SetBodyVisible(GameManager.bodyVisible);
                visibilityText.text = "Visible: " + GameManager.bodyVisible;
            }
        }

        private void OnHandsClicked(Text textRef)
        {
            if (GameManager.currentPlayerBody == null 
                || GameManager.currentPlayerBody.handRenderers == null
                || GameManager.currentPlayerBody.handRenderers.Length == 0)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);

                GameManager.handsVisible = !GameManager.handsVisible;
                GameManager.currentPlayerBody.SetHandsVisible(GameManager.handsVisible);
                handText.text = "Hands visible: " + GameManager.handsVisible;
            }
        }

        private void OnSelfHideClicked(Text textRef)
        {
            if (GameManager.currentPlayerBody == null)
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Error, transform.position);
            }
            else
            {
                SM.PlayGlobalUISound(SM.GlobalUISound.Beep, transform.position);
                GameManager.currentPlayerBody.ToggleSelfHide();
                selfHideText.text = "Auto-hide self: " + (PlayerBody.optionAutoHideSelf ? "True" : "False");
            }
        }

        private void SetPage(int index)
        {
            // Disable buttons from previous page if applicable
            if (currentPage != -1 && pages.TryGetValue(currentPage, out List<KeyValuePair<FVRPointableButton, Vector3>> previousButtons))
            {
                for (int i = 0; i < previousButtons.Count; ++i)
                {
                    previousButtons[i].Key.gameObject.SetActive(false);
                }
            }

            // Enable buttons of new page and set their positions
            if (pages.TryGetValue(index, out List<KeyValuePair<FVRPointableButton, Vector3>> newButtons))
            {
                for (int i = 0; i < newButtons.Count; ++i)
                {
                    newButtons[i].Key.gameObject.SetActive(true);
                    newButtons[i].Key.GetComponent<RectTransform>().localPosition = newButtons[i].Value;
                }
            }

            currentPage = index;
        }
    }
}
