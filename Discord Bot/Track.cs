using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_Bot
{
    public class Track
    {
        public string Name { get; set; }
        public string Reference { get; set; }
        public Track(string name, string reference)
        {
            this.Name = name;
            this.Reference = reference;
        }
    }
}
