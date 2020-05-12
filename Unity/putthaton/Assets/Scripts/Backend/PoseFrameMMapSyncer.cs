using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.IO.MemoryMappedFiles;

using UnityEngine;

namespace JulAI.UniPose
{
    /// <summary>
    /// The different tracked joints
    /// </summary>
    public enum EJointName
    {
        NOSE = 0,
        LEFT_EYE,
        RIGHT_EYE,
        LEFT_EAR,
        RIGHT_EAR,
        LEFT_SHOULDER,
        RIGHT_SHOULDER,
        LEFT_ELBOW,
        RIGHT_ELBOW,
        LEFT_WRIST,
        RIGHT_WRIST,
        LEFT_HIP,
        RIGHT_HIP,
        LEFT_KNEE,
        RIGHT_KNEE,
        LEFT_ANKLE,
        RIGHT_ANKLE
    }

    public interface IPoseFrame
    {
        float GetConfidence(EJointName joint);
        Vector2 GetPosition(EJointName joint);

        Texture2D GetFrame();
    }

    public class FastPoseFrame : IPoseFrame
    {

        public const float Pix2World = 1.0f / 100.0f;

        public readonly int ID;

        public readonly int Width;
        public readonly int Height;

        private float[] SkeletonData;
        private byte[] TextureData;

        public const int NUM_KEYPOINTS = 17;
        // in px
        public const int EXPECTED_WIDTH = 640;
        public const int EXPECTED_HEIGHT = 480;
        private const int Length_Skeleton = 3 * NUM_KEYPOINTS;
        private const int Length_Texture = EXPECTED_HEIGHT * EXPECTED_WIDTH * 3; // 3 channels

        private const int Offset_ID = 0;
        private const int Offset_Width = sizeof(int) + Offset_ID;
        private const int Offset_Height = sizeof(int) + Offset_Width;
        private const int Offset_Skeleton = sizeof(int) + Offset_Height;
        private const int Offset_Texture = sizeof(float) * Length_Skeleton + Offset_Skeleton;

        // don't access this one as an offset!
        public const int ByteLength = sizeof(byte) * Length_Texture + Offset_Texture;

        private Texture2D _texCache;

        public FastPoseFrame(byte[] buffer)
        {
            // The buffer is a valid pose-frame
            ID = BitConverter.ToInt32(buffer, Offset_ID);
            Width = BitConverter.ToInt32(buffer, Offset_Width);
            Height = BitConverter.ToInt32(buffer, Offset_Height);

            SkeletonData = new float[Length_Skeleton];
            TextureData = new byte[Length_Texture];

            int CurrOffset = Offset_Skeleton;
            for (int i = 0; i < Length_Skeleton; i++)
            {
                SkeletonData[i] = BitConverter.ToSingle(buffer, CurrOffset);
                CurrOffset += sizeof(float);
            }

            for (int i = 0; i < Length_Texture; i++)
            {
                TextureData[i] = buffer[CurrOffset];
                CurrOffset += 1;
            }

            _texCache = null;

        }

        public float GetConfidence(EJointName joint)
        {
            return this.GetConfidence((int)joint);
        }

        public float GetConfidence(int joint)
        {
            return this.SkeletonData[(joint * 3) + 2];
        }

        public Texture2D GetFrame()
        {
            if (_texCache != null) return _texCache;
            Texture2D tex = new Texture2D(EXPECTED_WIDTH, EXPECTED_HEIGHT, TextureFormat.RGB24, false);
            Color[] cols = new Color[EXPECTED_WIDTH * EXPECTED_HEIGHT];

            for (int i = 0; i < cols.Length; i++)
            {
                // We're actually reading BGR (OpenCV), but we want RGB for unity, so we do a little swip-swap

                // We're ALSO reading the pixels in reverse! This is because Unity textures read from
                // bottom-left instead of top-left. So, the image is upside down normally
                // Furthermore, we're thinking about "reflections in the mirror" instead of "camera in front of you"
                // so we ALSO would want to reverse the X's! Reversing while reading does both!
                // (TODO: Make X reversal optional)
                cols[cols.Length - 1 - i] = new Color(
                    (float)TextureData[i * 3 + 2] / 255.0f, 
                    (float)TextureData[i * 3 + 1] / 255.0f, 
                    (float)TextureData[i * 3 + 0] / 255.0f);
            }
            tex.SetPixels(cols);
            
            // Apply changes to GPU too!
            tex.Apply();
            // Cache this - that DOES MEAN THAT REFERENCES ARE IMPORTANT!
            _texCache = tex;
            return tex;
        }

        public Vector2 GetPosition(EJointName joint)
        {
            return this.GetPosition((int)joint);
        }

        public Vector2 GetPosition(int joint)
        {
            return new Vector2(this.SkeletonData[(joint * 3) + 0], this.SkeletonData[(joint * 3) + 1]);
        }

        public Vector2 GetWorldPosition(EJointName joint)
        {
            return GetWorldPosition((int)joint);
        }

        public Vector2 GetWorldPosition(int joint)
        {
            return Image2World(GetPosition(joint));
        }
        public Vector2 Image2World(Vector2 imageCoords)
        {
            // 1 world unit = 100 pixels

            // from - w/2 .. w/2 now

            float centeredx = imageCoords.x - (EXPECTED_WIDTH / 2.0f);
            float mirroredx = centeredx * -1.0f;
            float scaledx = mirroredx * Pix2World;
            // We ALSO want to mirror it - this has NOTHING to do with camera space,
            // but because we want this to be like a reflection.

            // from -h/2 .. h/2. -h/2 is the TOP of the image
            float centeredy = imageCoords.y - (EXPECTED_HEIGHT / 2.0f);
            float invertedy = -1.0f * centeredy;
            float scaledy = invertedy * Pix2World;

            return new Vector2(scaledx, scaledy);
        }
    }

    /// <summary>
    /// Since we need to send / recieve a huge amount of 
    /// data every frame, we have a specific class
    /// </summary>
    public class PoseFrameMMapSyncer
    {

        private const int STOP_CODE_ID = -10;

        public readonly string TagName;

        public int UpdateID { get; private set; }

        private byte[] MessageBuffer;

        private MemoryMappedFile MappedFile;
        private MemoryMappedViewStream MappedStream;

        public class OnSyncEventArgs
        {
            public readonly FastPoseFrame SyncData;

            public OnSyncEventArgs(FastPoseFrame SyncData)
            {
                this.SyncData = SyncData;
            }
        }

        public delegate void OnSyncEventHandler(object sender, OnSyncEventArgs e);

        public event OnSyncEventHandler OnSync;

        public bool IsOpen { get; private set; }

        public PoseFrameMMapSyncer(string MMapTag)
        {
            this.TagName = MMapTag;
            this.UpdateID = STOP_CODE_ID + 1; // Just a random ID we likely won't start on

            // We'll just use this buffer when getting ID's too
            this.MessageBuffer = new byte[FastPoseFrame.ByteLength];
            IsOpen = false;
        }

        public bool Connect()
        {
            try
            {
                MappedFile = MemoryMappedFile.OpenExisting(this.TagName);
            }
            catch(FileNotFoundException)
            {
                // The mmapped file doesn't exist yet
                return false;
            }
            MappedStream = MappedFile.CreateViewStream();
            IsOpen = true;
            return true;
        }

        public void Disconnect()
        {
            if (IsOpen)
            {
                MappedStream.Dispose();
                MappedFile.Dispose();
                IsOpen = false;
            }
        }

        private void UpdateFromBuffer()
        {
            // The buffer is full
            FastPoseFrame receivedFrame = new FastPoseFrame(this.MessageBuffer);
            OnSync?.Invoke(this, new OnSyncEventArgs(receivedFrame));

            this.UpdateID = receivedFrame.ID;
            if(this.UpdateID == STOP_CODE_ID)
            {
                // We won't be getting more!
                Disconnect();
            }
        }

        public void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            MappedStream.Flush();
            MappedStream.Seek(0, SeekOrigin.Begin);
            // Read the first 4 bytes
            MappedStream.Read(MessageBuffer, 0, 4);
            int nextID = BitConverter.ToInt32(MessageBuffer, 0);
            if (nextID != this.UpdateID)
            {
                // There was a change. Fill the rest of the buffer
                MappedStream.Read(MessageBuffer, 4, MessageBuffer.Length - 4);
                UpdateFromBuffer();
            }
        }

    }
}
