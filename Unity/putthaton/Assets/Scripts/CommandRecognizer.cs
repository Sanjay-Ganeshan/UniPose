using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows.Speech;

namespace JulAI.PutThatOn
{
    public class CommandRecognizer : MonoBehaviour
    {
        public string grammarpath = "grammar.xml";

        GrammarRecognizer recognizer;

        public static CommandRecognizer Instance;

        // just forward the speech recognition events
        public event PhraseRecognizer.PhraseRecognizedDelegate OnPhraseRecognized
        {
            add
            {
                recognizer.OnPhraseRecognized += value;
            }
            remove
            {
                recognizer.OnPhraseRecognized -= value;
            }
        }

        private void Awake()
        {
            string fullgrammarpath = System.IO.Path.Combine(Application.streamingAssetsPath, grammarpath);
            recognizer = new GrammarRecognizer(fullgrammarpath, ConfidenceLevel.Low);
            recognizer.OnPhraseRecognized += Recognizer_OnPhraseRecognized;
            Instance = this;
        }

        private void Recognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
        {
            Debug.Log("Heard " + args.text);
        }

        // Start is called before the first frame update
        void Start()
        {
            if(!recognizer.IsRunning)
            {
                recognizer.Start();
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        private void OnApplicationQuit()
        {
            if (recognizer.IsRunning)
            {
                recognizer.Stop();
            }
            recognizer.Dispose();
        }
        private void OnDisable()
        {
            if(recognizer.IsRunning)
            {
                recognizer.Stop();
            }
        }
        private void OnEnable()
        {
            if(!recognizer.IsRunning)
            {
                recognizer.Start();
            }
        }
        private void OnDestroy()
        {
            if(recognizer.IsRunning)
            {
                recognizer.Stop();
            }
            recognizer.Dispose();
        }

        public void PauseSpeech()
        {
            this.recognizer.Stop();
        }
        public void UnpauseSpeech()
        {
            this.recognizer.Start();
        }
    }
}
