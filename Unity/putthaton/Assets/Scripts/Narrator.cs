using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JulAI.PutThatOn
{

    public class Narrator : MonoBehaviour
    {
        public AudioClip DrawerOpening;

        public AudioClip Msg_Welcome;
        public AudioClip Msg_ExplainShowMe;
        public AudioClip Msg_OpenedDrawer;
        public AudioClip Msg_PutSomethingOn;
        public AudioClip Msg_StillInDrawer;
        public AudioClip Msg_WearingSomething;
        public AudioClip Msg_ExplainGoodbye;
        public AudioClip Msg_Goodbye;
        public AudioClip Msg_Selfie;

        private AudioSource player;

        private bool said_welcome;
        private bool said_showme;
        private bool said_openeddrawer;
        private bool said_putsomethingon;
        private bool said_stillindrawer;
        private bool said_wearingsomething;
        private bool said_explainedgoodbye;
        private bool said_goodbye;

        public static Narrator Instance;
        public bool IsSpeaking { get; private set; }

        private void Awake()
        {
            Instance = this;
            player = GetComponent<AudioSource>();
            said_welcome = false;
            said_showme = false;
            said_openeddrawer = false;
            said_putsomethingon = false;
            said_stillindrawer = false;
            said_wearingsomething = false;
            said_explainedgoodbye = false;
            said_goodbye = false;
            IsSpeaking = false;
        }

        // Start is called before the first frame update
        void Start()
        {        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void ExplainShowMe()
        {
            if(!said_showme)
            {
                PlayClip(Msg_ExplainShowMe);
                said_showme = true;
            }
        }

        public void OpenDrawer()
        {
            if(!said_openeddrawer)
            {
                PlayDrawerSound();
                Invoke("SayDrawerMessage", DrawerOpening.length + 1.5f);
            }
            else
            {
                PlayDrawerSound();
            }
        }

        private void PlayDrawerSound()
        {
            // This isn't really speech so we don't need to bother with all the extra stuff
            //PlayClip(DrawerOpening, false);
            player.PlayOneShot(DrawerOpening);
        }

        private void SayDrawerMessage()
        {
            if(!said_openeddrawer)
            {
                PlayClip(Msg_OpenedDrawer);
                said_openeddrawer = true;
            }
        }

        private void PlayClip(AudioClip c, bool pauseRecognition = true)
        {
            if(!IsSpeaking)
            {
                IsSpeaking = true;
                player.PlayOneShot(c);
                if(pauseRecognition)
                {
                    PauseRecordingUntilClipEnds(c);
                }
                Invoke("ClearIsSpeaking", c.length);
            }
        }

        private void ClearIsSpeaking()
        {
            IsSpeaking = false;
        }

        private void PauseRecordingUntilClipEnds(AudioClip c)
        {
            CommandRecognizer.Instance.PauseSpeech();
            CommandRecognizer.Instance.Invoke("UnpauseSpeech", c.length);
        }

        public void UIUnchanged(EUIState whichState)
        {
            switch (whichState)
            {
                case EUIState.MIRROR:
                    // Standing at the mirror? ask about goodbye / show me
                    if(said_welcome && said_showme && said_wearingsomething && !said_explainedgoodbye)
                    {
                        PlayClip(Msg_ExplainGoodbye);
                        said_explainedgoodbye = true;
                    }
                    break;
                case EUIState.DRAWER:
                    if(!said_stillindrawer)
                    {
                        PlayClip(Msg_StillInDrawer);
                        said_stillindrawer = true;
                    }
                    break;
            }

        }

        public void PutSomethingOn()
        {
            if(!said_putsomethingon)
            {
                PlayClip(Msg_PutSomethingOn);
                said_putsomethingon = true;
            }
        }

        public void Welcome()
        {
            if(!said_welcome)
            {
                PlayClip(Msg_Welcome);
                Invoke("ExplainShowMe", Msg_Welcome.length + 8.0f);
                said_welcome = true;
            }
        }

        public void MirrorWithClothes()
        {
            if(!said_wearingsomething)
            {
                PlayClip(Msg_WearingSomething);
                said_wearingsomething = true;
            }
        }

        public void Goodbye()
        {
            if(!said_goodbye)
            {
                PlayClip(Msg_Goodbye);
                float end_in = Msg_Goodbye.length + 1.0f;
                Invoke("CloseApp", end_in);
            }
        }

        private void CloseApp()
        {
            Application.Quit();
        }

        public void Selfie()
        {
            PlayClip(Msg_Selfie);
        }
    }
}
