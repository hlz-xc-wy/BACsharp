﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BACSharp.Types
{
    public class BacNetProperty
    {
        public BacNetUInt PropertyId { get; set; }
        public ArrayList Values { get; set; }
    }
}