using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DrudgeToReddit
{
    public class Link : IEquatable<Link>
    {
        public string Url;
        public string Text;
        public DateTime ObservedTime = DateTime.Now;

        public bool Equals(Link other)
        {
            return this.Url == other.Url;
        }
    }
}
