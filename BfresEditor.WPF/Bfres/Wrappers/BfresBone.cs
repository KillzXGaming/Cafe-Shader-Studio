using System;
using System.Collections.Generic;
using System.Text;
using BfresLibrary;
using BfresLibrary.GX2;
using BfresLibrary.Helpers;
using Toolbox.Core;
using CafeStudio.UI;

namespace BfresEditor
{
    public class BfresBone : STBone, IPropertyUI
    {
        public Bone BoneData { get; set; }

        public BfresBone(STSkeleton skeleton) : base(skeleton)
        {

        }

        public Type GetTypeUI() => typeof(BoneEditor);

        public void OnLoadUI(object uiInstance)
        {
   
        }

        public void OnRenderUI(object uiInstance)
        {
            var editor = (BoneEditor)uiInstance;
            editor.LoadEditor(this);
        }
    }
}
