using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JulAI.UniPose
{
    public class CaptureMirror : MonoBehaviour
    {
        private SpriteRenderer BGRenderer;

        private void Awake()
        {
            BGRenderer = GetComponent<SpriteRenderer>();
        }

        // Start is called before the first frame update
        void Start()
        {
            PoseSyncer.Instance.OnSync += Pose_OnSync;
        }

        private void Pose_OnSync(object sender, PoseFrameMMapSyncer.OnSyncEventArgs e)
        {
            Texture2D tex = e.SyncData.GetFrame();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), 0.5f * Vector2.one, 100.0f);
            if(BGRenderer.sprite != null)
            {
                Destroy(BGRenderer.sprite);
                BGRenderer.sprite = null;
            }
            BGRenderer.sprite = sprite;
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
