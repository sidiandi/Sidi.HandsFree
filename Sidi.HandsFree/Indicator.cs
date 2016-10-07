// Copyright (c) 2016, Andreas Grimme

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sidi.HandsFree
{
    public class Range
    {
        public string Text;
    }

    /// <summary>
    /// HFP indicator value
    /// </summary>
    public class Indicator
    {
        public int Index;
        public string Name;
        public Range Range;
        public int CurrentValue;

        public override string ToString()
        {
            return String.Format("{0}: {1} [{2}]", Name, CurrentValue, Range);
        }
    }
}
