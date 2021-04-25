using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.ViewModels;
using Toolbox.Core;

namespace BfresEditor 
{
    public class BfresNodeBase : NodeBase
    {
        public override bool HasCheckBox
        {
            get { return Tag is ICheckableNode; }
        }

        public override bool IsChecked 
        { get => base.IsChecked; 
            set 
            {
                base.IsChecked = value;
                if (Tag is ICheckableNode)
                    ((ICheckableNode)Tag).OnChecked(value);
            }
        }

        public BfresNodeBase(string text) : base(text)
        {

        }

        public BfresNodeBase() : base()
        {

        }
    }
}
