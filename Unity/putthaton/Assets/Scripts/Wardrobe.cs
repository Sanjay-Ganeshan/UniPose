using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JulAI.PutThatOn
{
    public class WardrobeSelection
    {
        public EClothingType? ClothingType;
        public int Index;

        public GameObject Instance;

        public bool Valid { get { return ClothingType.HasValue; } }        

        public WardrobeSelection()
        {
            this.ClothingType = null;
            this.Index = 0;
            this.Instance = null;
        }

        public WardrobeSelection(EClothingType t)
        {
            this.ClothingType = t;
            this.Index = 0;
            this.Instance = null;
        }

        public WardrobeSelection(EClothingType t, int i)
        {
            this.ClothingType = t;
            this.Index = i;
            this.Instance = null;
        }

        public WardrobeSelection(EClothingType t, int i, GameObject inst)
        {
            this.ClothingType = t;
            this.Index = i;
            this.Instance = inst;
        }

        public void Clear()
        {
            this.ClothingType = null;
            this.Index = 0;
            this.Instance = null;
        }
    }

    [System.Serializable]
    public class DrawerSprite
    {
        public EClothingType ClothingType;
        public Sprite sprite;
    }

    public class Wardrobe : MonoBehaviour
    {
        public Clothing[] KnownClothes;
        public SpriteRenderer PreviewPane;

        private Dictionary<EClothingType, List<Clothing>> KnownClothingByType;

        // Start is called before the first frame update

        private Dictionary<EClothingType, WardrobeSelection> Worn;
        private WardrobeSelection CurrentSelection;

        private Clothing selectedCloth { get { return KnownClothingByType[CurrentSelection.ClothingType.Value][CurrentSelection.Index]; } }

        public Sprite DefaultDrawerSprite;
        public DrawerSprite[] OpenDrawerSprites;

        public SpriteRenderer Drawers;

        public int NumWorn { get { return Worn.Count; } }

        private void Awake()
        {
            Worn = new Dictionary<EClothingType, WardrobeSelection>();
            CurrentSelection = new WardrobeSelection();
            KnownClothingByType = new Dictionary<EClothingType, List<Clothing>>();
            foreach (Clothing c in KnownClothes)
            {
                if(!KnownClothingByType.ContainsKey(c.CType))
                {
                    KnownClothingByType[c.CType] = new List<Clothing>();
                }
                KnownClothingByType[c.CType].Add(c);
            }
        }

        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }

        public bool SetView(EClothingType drawer)
        {
            if(this.Worn.ContainsKey(drawer))
            {
                // We're already wearing something from this drawer
                WardrobeSelection curr = Worn[drawer];
                this.CurrentSelection = new WardrobeSelection(curr.ClothingType.Value, curr.Index);

            }
            else
            {
                if(this.KnownClothingByType.ContainsKey(drawer))
                {
                    this.CurrentSelection = new WardrobeSelection(drawer);
                }
            }
            RefreshView();
            return CurrentSelection.Valid;
        }

        private void RefreshView()
        {
            bool foundDrawerSprite = false;
            if(this.CurrentSelection.Valid)
            {
                // Something's selected
                this.PreviewPane.sprite = selectedCloth.PreviewSprite;
                foreach(DrawerSprite dr in this.OpenDrawerSprites)
                {
                    if(dr.ClothingType == CurrentSelection.ClothingType.Value)
                    {
                        Drawers.sprite = dr.sprite;
                        foundDrawerSprite = true;
                        break;
                    }

                }
            }
            else
            {
                this.PreviewPane.sprite = null;
            }
            if(!foundDrawerSprite)
            {
                Drawers.sprite = DefaultDrawerSprite;
            }
        }

        public void ClearView()
        {
            this.CurrentSelection.Clear();
            RefreshView();
        }

        public void SelectNext()
        {
            if(CurrentSelection.Valid)
            {
                CurrentSelection.Index++;
                CurrentSelection.Index %= this.KnownClothingByType[CurrentSelection.ClothingType.Value].Count;
                RefreshView();
            }
        }

        public void SelectPrevious()
        {
            if (CurrentSelection.Valid)
            {
                CurrentSelection.Index--;
                // This will always wrap it around or just make it go extra
                CurrentSelection.Index += this.KnownClothingByType[CurrentSelection.ClothingType.Value].Count;
                CurrentSelection.Index %= this.KnownClothingByType[CurrentSelection.ClothingType.Value].Count;
                RefreshView();
            }
        }

        public void ChangeColor()
        {

        }

        public void WearCurrent()
        {
            if(CurrentSelection.Valid)
            {
                if(Worn.ContainsKey(CurrentSelection.ClothingType.Value))
                {
                    // We're already wearing something.
                    StripCurrent();
                }
                GameObject inst = GameObject.Instantiate(selectedCloth.Prefab);
                WardrobeSelection newlyWorn = new WardrobeSelection(CurrentSelection.ClothingType.Value, CurrentSelection.Index, inst);
                this.Worn.Add(newlyWorn.ClothingType.Value, newlyWorn);
            }
        }

        public void StripCurrent()
        {
            if(CurrentSelection.Valid)
            {
                StripFrom(CurrentSelection.ClothingType.Value);
            }
        }

        private void StripFrom(EClothingType ctype)
        {
            if(Worn.ContainsKey(ctype))
            {
                // We're wearing something
                GameObject.Destroy(Worn[ctype].Instance);
                Worn[ctype].Instance = null;
                Worn.Remove(ctype);
            }
        }

    }

}