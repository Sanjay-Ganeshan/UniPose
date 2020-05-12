using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JulAI.UniPose;
using System;
using System.IO;

namespace JulAI.PutThatOn
{
    public enum EUIState
    {
        // Looking for "show dresses", "selfie"
        MIRROR,
        DRAWER
    }

    public class FlowManager : MonoBehaviour
    {
        public EUIState CurrentState { get; private set; }

        public Wardrobe WardrobeManager;

        private Narrator Seamstress;

        public float InactiveTime = 20.0f;

        private float InactiveCurrTime = 0.0f;

        public Camera SelfieCam;

        // Start is called before the first frame update
        void Start()
        {
            PoseSyncer.Instance.OnSync += Pose_OnSync;
            CommandRecognizer.Instance.OnPhraseRecognized += Speech_OnRecognized;
            SwipeRecognizer.Instance.OnSwipe += Swipe_OnRecognize;
            Seamstress = Narrator.Instance;
            Seamstress.Welcome();
        }

        private void Swipe_OnRecognize(object sender, SwipeRecognizer.OnSwipeEventArgs e)
        {
            switch (CurrentState)
            {
                case EUIState.MIRROR:
                    break;
                case EUIState.DRAWER:
                    switch (e.Direction)
                    {
                        case ESwipeDirection.LEFT:
                            // Swipe left = move right
                            WardrobeManager.SelectNext();
                            break;
                        case ESwipeDirection.RIGHT:
                            WardrobeManager.SelectPrevious();
                            break;

                        case ESwipeDirection.UP:
                        case ESwipeDirection.DOWN:
                        default:
                            // Do nothing
                            break;
                    }
                    break;
            }
        }

        private void Speech_OnRecognized(UnityEngine.Windows.Speech.PhraseRecognizedEventArgs args)
        {
            string heard = args.text.ToLower();
            
            switch (CurrentState)
            {
                case EUIState.MIRROR:
                    // Just looking at the mirror
                    if(heard.Equals(GameConstants.KW_GOODBYE))
                    {
                        Goodbye();
                    }
                    else if(heard.Equals(GameConstants.KW_SELFIE))
                    {
                        TakeSelfie();
                    }
                    else if(heard.Equals(GameConstants.KW_SHOW_DRESSES))
                    {
                        OpenDrawer(EClothingType.DRESS);
                    }
                    else if(heard.Equals(GameConstants.KW_SHOW_PANTS))
                    {
                        OpenDrawer(EClothingType.PANTS);
                    }
                    else if(heard.Equals(GameConstants.KW_SHOW_SHIRTS))
                    {
                        OpenDrawer(EClothingType.SHIRT);
                    }
                    else if(heard.Equals(GameConstants.KW_SHOW_ACCESSORIES))
                    {
                        OpenDrawer(EClothingType.ACCESSORY);
                    }
                    break;
                case EUIState.DRAWER:
                    if(heard.Equals(GameConstants.KW_PUT_THAT_ON))
                    {
                        WearCurrent();
                    }
                    else if(heard.Equals(GameConstants.KW_TAKE_THAT_OFF))
                    {
                        StripCurrent();
                    }
                    else if(heard.Equals(GameConstants.KW_CHANGE_COLOR))
                    {
                        ChangeColor();
                    }
                    else if(heard.Equals(GameConstants.KW_NEXT))
                    {
                        WardrobeManager.SelectNext();
                    }
                    else if(heard.Equals(GameConstants.KW_PREVIOUS))
                    {
                        WardrobeManager.SelectPrevious();
                    }
                    else if(heard.Equals(GameConstants.KW_BROWSE))
                    {
                        CloseDrawer();
                    }
                    else if (heard.Equals(GameConstants.KW_SHOW_DRESSES))
                    {
                        OpenDrawer(EClothingType.DRESS);
                    }
                    else if (heard.Equals(GameConstants.KW_SHOW_PANTS))
                    {
                        OpenDrawer(EClothingType.PANTS);
                    }
                    else if (heard.Equals(GameConstants.KW_SHOW_SHIRTS))
                    {
                        OpenDrawer(EClothingType.SHIRT);
                    }
                    else if (heard.Equals(GameConstants.KW_SHOW_ACCESSORIES))
                    {
                        OpenDrawer(EClothingType.ACCESSORY);
                    }
                    break;
            }
        }

        private void MarkActivity()
        {
            InactiveCurrTime = 0.0f;
        }

        private void ChangeColor()
        {
            WardrobeManager.ChangeColor();
        }

        private void StripCurrent()
        {
            WardrobeManager.StripCurrent();
            MarkActivity();
        }

        private void WearCurrent()
        {
            WardrobeManager.WearCurrent();
            Seamstress.PutSomethingOn();
            MarkActivity();
        }

        private void TakeSelfie()
        {
            MarkActivity();
            Seamstress.Selfie();
            SelfieCam.enabled = true;
            // Since it hasn't been updating, we force a rerender
            SelfieCam.Render();

            string fn = GetNextSelfieFilename();

            ExportRT(SelfieCam.targetTexture, fn);
            Debug.Log(string.Format("Saved selfie to {0}", fn));

            SelfieCam.enabled = false;
        }

        private string GetNextSelfieFilename()
        {
            int ix = 0;
            string selfiepath;
            do
            {
                selfiepath = Path.Combine(Application.persistentDataPath, string.Format("selfie{0}.jpg", ix));
                ix++;
            }
            while (File.Exists(selfiepath));

            // File doesn't exist now!
            return selfiepath;
        }

        private void ExportRT(RenderTexture rt, string fn)
        {
            // capture the virtuCam and save it as a square PNG.

            Texture2D virtualPhoto =
                new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            // false, meaning no need for mipmaps
            RenderTexture.active = rt;
            virtualPhoto.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);

            byte[] bytes;
            bytes = virtualPhoto.EncodeToJPG();

            System.IO.File.WriteAllBytes(
                fn, bytes);
        }

        private void Goodbye()
        {
            Seamstress.Goodbye();
            MarkActivity();
        }

        void OpenDrawer(EClothingType drawer)
        {
            bool success = WardrobeManager.SetView(drawer);
            if(success)
            {
                // If this failed (i.e. we don't have any clothing of that type), don't change modes
                this.CurrentState = EUIState.DRAWER;
                Seamstress.OpenDrawer();
            }
            MarkActivity();
        }

        void CloseDrawer()
        {
            WardrobeManager.ClearView();
            this.CurrentState = EUIState.MIRROR;
            if(WardrobeManager.NumWorn > 0)
            {
                Seamstress.MirrorWithClothes();
            }
            MarkActivity();
        }
        
        private void Pose_OnSync(object sender, PoseFrameMMapSyncer.OnSyncEventArgs e)
        {
            
        }


        // Update is called once per frame
        void Update()
        {
            if(InactiveCurrTime > InactiveTime)
            {
                Seamstress.UIUnchanged(CurrentState);
                InactiveCurrTime = 0.0f;
            }
            InactiveCurrTime += Time.deltaTime;
        }
    }
}
