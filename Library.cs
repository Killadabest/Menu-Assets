using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static IVY_Paid.Settings;

namespace IVY_Paid.Notifications
{
    [BepInPlugin("org.gorillatag.lars.notifications2", "NotificationLibrary", "1.0.5")]
    public class NotifiLib : BaseUnityPlugin
    {
        private static NotifiLib instance;

        private const string SoundUrl = "https://github.com/Killadabest/Menu-Assets/raw/refs/heads/main/VYVNotification.mp3";
        private static AudioClip notificationSound;
        private static AudioSource audioSource;

        private GameObject HUDObj;
        private GameObject HUDObj2;
        private GameObject MainCamera;

        private Material AlertText = new Material(Shader.Find("GUI/Text Shader") ?? Shader.Find("UI/Default"));

        private static List<NotificationItem> activeNotifications = new List<NotificationItem>();
        private static Transform containerTransform;

        public static string PreviousNotifi;
        private bool HasInit;
        public static bool IsEnabled = true;

        private class NotificationItem
        {
            public GameObject obj;
            public Text textComponent;
            public RectTransform rect;
            public float currentX = -450f;
            public float targetX = 0f;
            public float currentY = 0f;
            public float targetY = 0f;
            public float timeAlive = 0f;
        }

        private void Awake()
        {
            instance = this;
            Logger.LogInfo("Plugin NotificationLibrary is loaded!");
            StartCoroutine(LoadAudio());
        }

        private IEnumerator LoadAudio()
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(SoundUrl, AudioType.MPEG))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    notificationSound = DownloadHandlerAudioClip.GetContent(www);
                }
            }
        }

        private void Init()
        {
            this.MainCamera = GameObject.Find("Main Camera");
            this.HUDObj = new GameObject("NOTIFICATIONLIB_HUD_OBJ");
            this.HUDObj2 = new GameObject("NOTIFICATIONLIB_HUD_OBJ2");

            Canvas canvas = this.HUDObj.AddComponent<Canvas>();
            this.HUDObj.AddComponent<CanvasScaler>();
            this.HUDObj.AddComponent<GraphicRaycaster>();

            canvas.enabled = true;
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = this.MainCamera.GetComponent<Camera>();

            RectTransform hudRect = this.HUDObj.GetComponent<RectTransform>();
            hudRect.sizeDelta = new Vector2(5f, 5f);
            hudRect.position = this.MainCamera.transform.position;

            this.HUDObj2.transform.position = new Vector3(this.MainCamera.transform.position.x, this.MainCamera.transform.position.y, this.MainCamera.transform.position.z - 4.6f);
            this.HUDObj.transform.parent = this.HUDObj2.transform;
            hudRect.localPosition = new Vector3(0f, 0f, 1.6f);

            Vector3 eulerAngles = hudRect.rotation.eulerAngles;
            eulerAngles.y = -270f;
            hudRect.rotation = Quaternion.Euler(eulerAngles);
            this.HUDObj.transform.localScale = Vector3.one;

            audioSource = this.HUDObj.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            containerTransform = this.HUDObj.transform;
        }

        private void FixedUpdate()
        {
            if (!this.HasInit && GameObject.Find("Main Camera") != null)
            {
                this.Init();
                this.HasInit = true;
            }

            if (!this.HasInit || this.HUDObj2 == null) return;

            this.HUDObj2.transform.position = this.MainCamera.transform.position;
            this.HUDObj2.transform.rotation = this.MainCamera.transform.rotation;

            for (int i = activeNotifications.Count - 1; i >= 0; i--)
            {
                NotificationItem item = activeNotifications[i];
                if (item.obj == null)
                {
                    activeNotifications.RemoveAt(i);
                    continue;
                }

                item.currentX = Mathf.Lerp(item.currentX, item.targetX, Time.deltaTime * 12f);
                item.currentY = Mathf.Lerp(item.currentY, item.targetY, Time.deltaTime * 10f);
                item.rect.localPosition = new Vector3(item.currentX, item.currentY, -0.5f);

                item.timeAlive += Time.deltaTime;
                if (item.timeAlive > 4f)
                {
                    Destroy(item.obj);
                    activeNotifications.RemoveAt(i);
                }
            }
        }

        public static void SendNotification(string NotificationText)
        {
            if (disableNotifications || !IsEnabled || containerTransform == null) return;
            if (PreviousNotifi == NotificationText) return;

            PreviousNotifi = NotificationText;

            if (audioSource != null && notificationSound != null)
            {
                audioSource.PlayOneShot(notificationSound);
            }

            foreach (var existingItem in activeNotifications)
            {
                existingItem.targetY += 35f;
            }

            GameObject textObj = new GameObject("Notification_Item");
            textObj.transform.SetParent(containerTransform, false);

            Text textComp = textObj.AddComponent<Text>();
            textComp.text = NotificationText;
            textComp.fontSize = 28;
            textComp.font = currentFont;
            textComp.alignment = TextAnchor.LowerLeft;
            textComp.supportRichText = true;
            textComp.material = instance.AlertText;

            RectTransform rect = textComp.rectTransform;
            rect.sizeDelta = new Vector2(450f, 40f);
            rect.localScale = new Vector3(0.00333333333f, 0.00333333333f, 0.33333333f);
            rect.localPosition = new Vector3(-450f, 0f, -0.5f);

            NotificationItem newItem = new NotificationItem
            {
                obj = textObj,
                textComponent = textComp,
                rect = rect,
                currentX = -450f,
                targetX = -1f,
                currentY = -1f,
                targetY = -1f,
                timeAlive = 0f
            };

            activeNotifications.Add(newItem);
        }

        public static void ClearAllNotifications()
        {
            foreach (var item in activeNotifications)
            {
                if (item.obj != null) Destroy(item.obj);
            }
            activeNotifications.Clear();
        }

        public static void ClearPastNotifications(int amount)
        {
            int removeCount = Mathf.Min(amount, activeNotifications.Count);
            for (int i = 0; i < removeCount; i++)
            {
                if (activeNotifications[0].obj != null) Destroy(activeNotifications[0].obj);
                activeNotifications.RemoveAt(0);
            }
        }
    }
}
