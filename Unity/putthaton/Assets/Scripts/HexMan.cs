using System;
using System.Collections;
using System.Collections.Generic;
using JulAI.UniPose;
using UnityEngine;


namespace JulAI.UniPose
{
    public class HexMan : MonoBehaviour
    {

        public GameObject JointPrefab;

        private GameObject[] AllJoints;

        private const float Pix2World = 1.0f / 100.0f;

        public Color LeftColor = Color.red;
        public Color RightColor = Color.green;
        public Color NeutralColor = Color.black;

        [Range(0.0f, 1.0f)]
        public float ConfidenceThreshold = 0.4f;

        // Start is called before the first frame update
        void Start()
        {


            Transform me = this.transform;
            AllJoints = new GameObject[FastPoseFrame.NUM_KEYPOINTS];
            for(int i = 0; i < FastPoseFrame.NUM_KEYPOINTS; i++)
            {
                // Make a new hex for each
                AllJoints[i] = GameObject.Instantiate(JointPrefab, me);
                string name = ((EJointName)i).ToString();
                AllJoints[i].transform.name = name;
                
                // Color code! Helps with understanding the "mirror" effect
                if(name.ToLower().StartsWith("left"))
                {
                    AllJoints[i].GetComponent<SpriteRenderer>().color = LeftColor;
                }
                else if (name.ToLower().StartsWith("right"))
                {
                    AllJoints[i].GetComponent<SpriteRenderer>().color = RightColor;
                }
                else
                {
                    AllJoints[i].GetComponent<SpriteRenderer>().color = NeutralColor;
                }
            }

            PoseSyncer.Instance.OnSync += Pose_OnSync;
        }

        private void Pose_OnSync(object sender, PoseFrameMMapSyncer.OnSyncEventArgs e)
        {
            SyncFrom(e.SyncData);
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public void SyncFrom(FastPoseFrame pose)
        {
            for (int i = 0; i < AllJoints.Length; i++)
            {
                AllJoints[i].transform.position = this.Image2World(pose.GetPosition(i));
                if(pose.GetConfidence(i) < ConfidenceThreshold)
                {
                    AllJoints[i].SetActive(false);
                }
                else
                {
                    AllJoints[i].SetActive(true);
                }
            }
        }

        public Vector2 Image2World(Vector2 imageCoords)
        {
            // 1 world unit = 100 pixels

            // from - w/2 .. w/2 now
            
            float centeredx = imageCoords.x - (FastPoseFrame.EXPECTED_WIDTH / 2.0f);
            float mirroredx = centeredx * -1.0f;
            float scaledx = mirroredx * Pix2World;
            // We ALSO want to mirror it - this has NOTHING to do with camera space,
            // but because we want this to be like a reflection.

            // from -h/2 .. h/2. -h/2 is the TOP of the image
            float centeredy = imageCoords.y - (FastPoseFrame.EXPECTED_HEIGHT / 2.0f);
            float invertedy = -1.0f * centeredy;
            float scaledy = invertedy * Pix2World;

            return new Vector2(scaledx, scaledy);
        }
    }
}
