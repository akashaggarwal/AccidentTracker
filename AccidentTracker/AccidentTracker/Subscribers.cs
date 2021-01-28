using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccidentTracker
{
    public class Subscribers 
    {
        public Subscribers(string f, string l, string e, string p,  bool em, bool s)
        {
            FirstName = f;
            LastName = l;
            Email = e;
            Phone = p;
            NotifyEmail = em;
            NotifySMS = s;
        }
        public Subscribers()
        {
                
        }

        public long ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public bool NotifyEmail { get; set; }
        public bool NotifySMS { get; set; }
    }
}
