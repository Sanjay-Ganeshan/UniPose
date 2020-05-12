using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Windows.Speech;

namespace JulAI.PutThatOn
{

    public class StartButton : MonoBehaviour
    {
        private Collider2D col;
        private KeywordRecognizer kwr;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
        }

        // Start is called before the first frame update
        void Start()
        {
            kwr = new KeywordRecognizer(new string[] { "Start" });
            kwr.OnPhraseRecognized += startRecognized;
            kwr.Start();
        }

        private void startRecognized(PhraseRecognizedEventArgs args)
        {
            LaunchGame();
        }

        // Update is called once per frame
        void Update()
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;
            if (Input.GetMouseButtonDown(0) && 
                col.bounds.Contains(mousePos))
            {
                LaunchGame();
            }
        }

        void LaunchGame()
        {
            kwr.Stop();
            SceneManager.LoadScene("PutThatOn");
        }
    }

}