﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace f3
{
    public class MeshReferenceSO : GroupSO
    {
        public string MeshReferencePath { get; set; }

        public MeshReferenceSO() : base()
        {
            MeshReferencePath = "";
        }

        override public SOType Type { get { return SOTypes.MeshReference; } }

    }
}
