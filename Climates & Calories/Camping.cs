// Project:         Climates & Calories mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2020 Ralzar
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Ralzar

using UnityEngine;
using DaggerfallConnect;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.MagicAndEffects;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using UnityEngine;
using System;
using DaggerfallWorkshop;
using DaggerfallConnect.Arena2;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Serialization;
using System.Collections.Generic;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallConnect.Utility;
using System;
using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility.AssetInjection;
using DaggerfallWorkshop.Game.UserInterface;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Utility;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallConnect.Utility;

namespace ClimatesCalories
{
    public class Camping
    {
        public static DFPosition CampMapPixel = null;
        public static bool CampDeployed = false;
        public static Vector3 TentPosition;
        public static Quaternion TentRotation;
        public static GameObject Tent = null;
        public static Matrix4x4 TentMatrix;
        public static GameObject Fire = null;
        public static Vector3 FirePosition;
        public static bool FireLit = false;

        public const int tentModelID = 41606;
        public const int templateIndex_Tent = 515;

        public static bool UseCampEquip(DaggerfallUnityItem item, ItemCollection collection)
        {
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInside && !GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                    DaggerfallUI.MessageBox("You can not set up your tent indoors.");
                    return false;
            }
            else if (!CampDeployed)
            {
                item.LowerCondition(1, GameManager.Instance.PlayerEntity, collection);
                DeployTent();
                return true;
            }
            else
            {
                DaggerfallUI.MessageBox("You have already set up your tent.");
                return false;
            }
        }

        public static void DeployTent(bool fromSave = false)
        {
            if (fromSave == false)
            {
                CampMapPixel = GameManager.Instance.PlayerGPS.CurrentMapPixel;
                SetTentPositionAndRotation();
                DaggerfallUI.MessageBox("You set up camp");
            }
            //Attempt to load a model replacement
            Tent = MeshReplacement.ImportCustomGameobject(tentModelID, null, TentMatrix);
            Fire = GameObjectHelper.CreateDaggerfallBillboardGameObject(210, 1, null);
            if (Tent == null)
            {
                Tent = GameObjectHelper.CreateDaggerfallMeshGameObject(tentModelID, null);
            }
            //Set the model's position in the world
            Tent.transform.SetPositionAndRotation(TentPosition, TentRotation);
            if (GameManager.Instance.PlayerEnterExit.IsPlayerInsideDungeon)
            {
                FirePosition = Tent.transform.position + (Tent.transform.up * 0.7f);
                Tent.SetActive(false);
            }
            else
            {
                FirePosition = Tent.transform.position + (Tent.transform.forward * 3) + (Tent.transform.up * 0.6f);
                Tent.SetActive(true);
            }
            Fire.transform.SetPositionAndRotation(FirePosition, TentRotation);
            Fire.SetActive(true);
            AddTorchAudioSource(Fire);
            GameObject lightsNode = new GameObject("Lights");
            lightsNode.transform.parent = Fire.transform;
            AddLight(DaggerfallUnity.Instance, Fire, lightsNode.transform);
            CampDeployed = true;
            FireLit = true;
        }        

        public static void RestOrPackTent(RaycastHit hit)
        {
            DaggerfallMessageBox campPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            if (hit.transform.gameObject.GetInstanceID() == Tent.GetInstanceID())
            {
                string[] message = { "Do you wish to rest?" };
                campPopUp.SetText(message);
                campPopUp.OnButtonClick += CampPopUp_OnButtonClick;
                campPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                campPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                campPopUp.Show();
            }
            else
            {
                DaggerfallUI.MessageBox("This is not your camp.");
            }
        }

        public static void RestOrPackFire(RaycastHit hit)
        {
            DaggerfallMessageBox campPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            if (hit.transform.gameObject.GetInstanceID() == Fire.GetInstanceID())
            {
                string[] message = { "Do you wish to rest?" };
                campPopUp.SetText(message);
                campPopUp.OnButtonClick += CampPopUp_OnButtonClick;
                campPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
                campPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
                campPopUp.Show();
            }
            else
            {
                ClimateCalories.camping = true;
                DaggerfallUI.PostMessage(DaggerfallUIMessages.dfuiOpenRestWindow);
            }
        }

        private static void CampPopUp_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.Yes)
            {
                sender.CloseWindow();
                IUserInterfaceManager uiManager = DaggerfallUI.UIManager;
                ClimateCalories.camping = true;
                uiManager.PushWindow(new DaggerfallRestWindow(uiManager, true));
            }
            else
            {
                sender.CloseWindow();
                PackOrLeaveCamp();
            }
        }

        public static void PackOrLeaveCamp()
        {
            DaggerfallMessageBox packPopUp = new DaggerfallMessageBox(DaggerfallUI.UIManager, DaggerfallUI.UIManager.TopWindow);
            string[] message = { "Do you wish to pack up your camp?" };
            packPopUp.SetText(message);
            packPopUp.OnButtonClick += PackPopUp_OnButtonClick;
            packPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.Yes);
            packPopUp.AddButton(DaggerfallMessageBox.MessageBoxButtons.No, true);
            packPopUp.Show();
        }

        private static void PackPopUp_OnButtonClick(DaggerfallMessageBox sender, DaggerfallMessageBox.MessageBoxButtons messageBoxButton)
        {
            if (messageBoxButton == DaggerfallMessageBox.MessageBoxButtons.No)
            {
                sender.CloseWindow();
            }
            else
            {
                DestroyCamp();
                CampDeployed = false;
                FireLit = false;
                TentMatrix = new Matrix4x4();
                sender.CloseWindow();
                DaggerfallUI.MessageBox("You pack up your camp.");
            }
        }


        public static void DestroyCamp()
        {
            if (Tent != null)
            {
                UnityEngine.Object.Destroy(Tent);
                UnityEngine.Object.Destroy(Fire);
                Tent = null;
                Fire = null;
            }
        }





        private static void SetTentPositionAndRotation()
        {
            GameObject player = GameManager.Instance.PlayerObject;
            TentPosition = player.transform.position + (player.transform.forward * 3);
            TentMatrix = player.transform.localToWorldMatrix;

            RaycastHit hit;
            Ray ray = new Ray(TentPosition, Vector3.down);
            if (Physics.Raycast(ray, out hit, 10))
            {
                Debug.Log("Setting tent position and rotation");
                TentPosition = hit.point;
                TentRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            else
            {
                Debug.Log("Setting tent position and rotation failed");
            }
        }

        private static GameObject AddLight(DaggerfallUnity dfUnity, GameObject obj, Transform parent)
        {
            Vector3 position = FirePosition;
            GameObject go = GameObjectHelper.InstantiatePrefab(dfUnity.Option_DungeonLightPrefab.gameObject, string.Empty, parent, position);
            Light light = go.GetComponent<Light>();
            if (light != null)
            {
                light.range = 15;
            }

            return go;
        }

        private static void AddTorchAudioSource(GameObject go)
        {
            DaggerfallAudioSource c = go.AddComponent<DaggerfallAudioSource>();
            c.AudioSource.dopplerLevel = 0;
            c.AudioSource.rolloffMode = AudioRolloffMode.Linear;
            c.AudioSource.maxDistance = 5f;
            c.AudioSource.volume = 0.7f;
            c.SetSound(SoundClips.Burning, AudioPresets.LoopIfPlayerNear);
        }



    }
}