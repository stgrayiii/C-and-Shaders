//Stewart Gray III
//Multiple Position Binder | This Tool enables the Transforms of VFX instances to be written to and read from a Texture
//December 2021
using System.Collections.Generic;
using UnityEngine.VFX;

namespace UnityEngine.VFX.Utility
{
    [AddComponentMenu("VFX/Property Binders/Multiple Position Binder")]
    [VFXBinder("Point Cache/Multiple Position Binder")]

    class VFXMultiplePositionBinder : VFXBinderBase
    {

        [VFXPropertyBinding("UnityEngine.Texture2D"), UnityEngine.Serialization.FormerlySerializedAs("PositionMapParameter")]
        public ExposedProperty PositionMapProperty = "PositionMap";

        [VFXPropertyBinding("System.Int32"), UnityEngine.Serialization.FormerlySerializedAs("PositionCountParameter")]
        public ExposedProperty PositionCountProperty = "PositionCount";

        //An array of empty Transform Targets
        public GameObject[] Targets = null;
        public bool EveryFrame = false;

        //Declaring a Texture to contain our baked positions from the Transforms above
        private Texture2D positionMap;
        private int count = 0;

        protected override void OnEnable()
        {
            base.OnEnable(); 
            UpdateTexture();
        }

        //This component must have both: a Position Map Texture and a Count
        public override bool IsValid(VisualEffect component)
        {
            return Targets != null &&
                component.HasTexture(PositionMapProperty) &&
                component.HasInt(PositionCountProperty);
        }

        //Updating our Position Map Texture and its associated Count
        public override void UpdateBinding(VisualEffect component)
        {
            if (EveryFrame || Application.isEditor)
                UpdateTexture();
            component.SetTexture(PositionMapProperty, positionMap);
            component.SetInt(PositionCountProperty, count);
        }

        void UpdateTexture()
        {   
            //Do not do anything if there are no Transform targets
            if (Targets == null || Targets.Length == 0)
                return;

            //Initializing a List to contain our (valid) Transforms
            var candidates = new List<Vector3>();
            foreach (var obj in Targets)
            {
                if (obj != null)
                    candidates.Add(obj.transform.position);
            }
            count = candidates.Count;

            //We only want to create the Position Map Texture once all valid Transforms have been added to the List
            if (positionMap == null || positionMap.width != count)
            {
                positionMap = new Texture2D(count, 1, TextureFormat.RGBAFloat, false); 
            }
            //Intializing a List of Colors - these will store the Position data of the Transforms
            List<Color> colors = new List<Color>();
            foreach (var pos in candidates)
            {
                //Add all the positions of the Transforms in our previous List as Colors
                colors.Add(new Color(pos.x, pos.y, pos.z));
            }
            positionMap.name = gameObject.name + "_PositionMap";
            positionMap.filterMode = FilterMode.Point;
            positionMap.wrapMode = TextureWrapMode.Repeat;

            //Populating our Position Map with the Colors containing our Positions
            positionMap.SetPixels(colors.ToArray(), 0);
            positionMap.Apply();
        }

        public override string ToString()
        {
            return string.Format("Multiple Position Binder ({0} positions)", count);
        }
    }
}