using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace JulAI.PutThatOn
{
    public enum EClothingType
    {
        ACCESSORY,
        // Top & bottoms
        DRESS,
        SHIRT,
        PANTS
    }

    [System.Serializable]
    public class Clothing
    {
        public GameObject Prefab;
        public Sprite PreviewSprite;
        public EClothingType CType;

        public Clothing()
        {

        }

    }
}
